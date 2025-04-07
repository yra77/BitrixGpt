

using BitrixGpt.Bitrix24.Models;
using BitrixGpt.ConnectOpenAI;
using BitrixGpt.Helpers;
using BitrixGpt.Logs;


namespace BitrixGpt.Bitrix24
{
    /// <summary>
    /// делегат відповідь від ChatGpt 
    /// </summary>
    /// <param name="userID"></param>
    /// <param name="questions"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    public delegate Task responseChatGpt(int userID, string questions, string response);

    class BitrixMain
    {


        private readonly ILog _log;
        private readonly Settings_Prop _SETTINGS;
        private readonly responseChatGpt _responseChatGpt_delegate;
        /// <summary>
        /// int - dealID, MainData
        /// </summary>
        private readonly Dictionary<int, MainData> _listMainData;

        /// <summary>
        /// int - clientID, List<Deal> - список дубликатов deal
        /// </summary>
        private readonly Dictionary<string, List<Deal>> _dublikats;


        public BitrixMain(ILog log, Settings_Prop settings)
        {
            try
            {
                _log = log;
                _SETTINGS = settings;
                _listMainData = [];
                _dublikats = [];
                _responseChatGpt_delegate = SendMsgResponseAsync;
            }
            catch (Exception ex)
            {
                _ = _log.LogDelegate(this, $"Error TelegramInputOutputMsg constructor {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string[] voronki_arr = [];
            string[] source_name_arr = [];
            string[] projects_name_arr = [];

            try
            {
                voronki_arr = _SETTINGS.BITRIX_VORONKA.Split(",");
                source_name_arr = _SETTINGS.BITRIX_SOURCE_NAME.Split(",");
                projects_name_arr = _SETTINGS.BITRIX_MY_PROJECTS_NAME.Split(",");
            }
            catch (Exception ex)
            { await _log.LogDelegate(this, $"ERROR settings split: {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error); }

            do
            {
                try
                {
                    //якщо воронок декілька
                    var listDeal = new List<Deal>();
                    for (int i = 0; i < voronki_arr.Length; i++)
                    {
                        var res = await Deal_Bitrix.GetDealAsync(voronkaID: voronki_arr[i]);
                        if (res != null) listDeal.AddRange(res);
                    }

                    if (listDeal?.Count() == 0)
                    {
                        _listMainData.Clear();
                        // delay 3 min
                        await Task.Delay(180000, cancellationToken);
                        continue;
                    }

                    //проверяем, может уже нет сделки в первой колонке битрикса
                    //если нет, удаляем из массива
                    if (_listMainData.Count() > 0
                        && listDeal.Any(x => _listMainData.Any(v => v.Key != x.ID)))
                    {
                        foreach (var item in _listMainData)
                        {
                            if (!listDeal.Any(j => j.ID == item.Key))
                            {
                                //историю переписки переносим в архив
                                await MoveFile_To_Archive.File_MoveArchivAsync(item.Value.Deal.ID.ToString(), _log);
                                //перемещаем в отвеченные
                                string stageName = (item.Value.Deal.CATEGORY_ID == "4") ? "UC_8ZPNTY" : "PREPARATION";
                                //дубликати переносимо
                                await MoveDublicatAsync(item.Value.Deal.CONTACT_ID, $"C{item.Value.Deal.CATEGORY_ID}:{stageName}", item.Value.Deal.CATEGORY_ID);
                                // переадресація діалогу іншому менеджеру
                                await SendToBitrix.TransferOpenLineDialogAsync(item.Value.OL_OpentChatID, item.Value.Deal.ASSIGNED_BY_ID);
                                _listMainData.Remove(item.Key);
                            }
                        }
                    }

                    foreach (var item in listDeal)
                    {
                        //если в ручную переместили со стадии "отвеченно" назад в новую. перемещаем в "отвеченно"
                        if (item.TITLE.Contains(" === Отвечено", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //перемещаем в отвеченные
                            string stageName = (item.CATEGORY_ID == "4") ? "UC_8ZPNTY" : "PREPARATION";
                            await Deal_Bitrix.MoveDealToNextStageAsync(item.ID, $"C{item.CATEGORY_ID}:{stageName}", int.Parse(item.CATEGORY_ID), item.TITLE);
                            continue;
                        }
                        if (item.SOURCE_ID != null
                           && !source_name_arr.Any(x => item.SOURCE_ID.Contains(x, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            await _log.LogDelegate(this, $"Воронка {item.CATEGORY_ID}, угода - {item.ID}, {item.SOURCE_ID} Це джерело ми не перевіряємо", Enums.LogLevels.Warning);
                            continue;
                        }

                        string nameClient = item.TITLE.Split("-")[0].Trim();

                        if (_listMainData.Count() == 0
                            || IsDealDublicat(item, nameClient)
                            || _listMainData.ContainsKey(item.ID))
                        {
                            //ищем все чаты и открытые и закрытые
                            var opChatsIDList = await Deal_Bitrix.GetOpenLineChatIDAsync(int.Parse(item.CONTACT_ID), false);
                            if (opChatsIDList == null)
                            {
                                await _log.LogDelegate(this, $"Воронка {item.CATEGORY_ID}, угода - {item.ID}, {item.CONTACT_ID} - Цей контакт не має чатів {item.TITLE}", Enums.LogLevels.Warning);
                                continue;
                            }
                            //открытый чат должен стоять последним
                            var opDialogInfo = await Deal_Bitrix.GetOpenLineDialogInfoAsync(opChatsIDList[opChatsIDList.Count - 1].CHAT_ID);
                            if (opDialogInfo < 1)
                            {
                                await _log.LogDelegate(this, $"Воронка {item.CATEGORY_ID}, угода - {item.ID}, {item.CONTACT_ID} - цей контакт не має відкритих діалогів {item.TITLE}", Enums.LogLevels.Warning);
                                continue;
                            }
                            var dialogHistory = await Deal_Bitrix.GetOpenLineChatHistoryAsync(opDialogInfo);
                            if (dialogHistory == null)
                            {
                                await _log.LogDelegate(this, $"Воронка {item.CATEGORY_ID}, угода - {item.ID}, {item.CONTACT_ID} - цей контакт не має історії повідомлень {item.TITLE}", Enums.LogLevels.Warning);
                                continue;
                            }

                            int lastMsgClientID = _listMainData.ContainsKey(item.ID) ? _listMainData[item.ID].InpMgID_Last : 0;
                            string clientQustions = "";
                            //якщо є повідомлення без відповіді
                            if (!IS_ManagerHasReplied(dialogHistory.MESSAGE.Values.ToList(), item.ASSIGNED_BY_ID))
                            {
                                //проверяем и возвращаем вопрос на который нет ответа и ID последнего вопроса
                                clientQustions = GetClientMsgWithoutManagerRiple(dialogHistory.MESSAGE.Values.ToList(),
                                                                                item.ASSIGNED_BY_ID, ref lastMsgClientID);
                            }

                            //query to gpt
                            if (clientQustions?.Length > 0)
                            {
                                //await Deal_Bitrix.AnswerOpenLineDialogAsync(opChatsIDList[opChatsIDList.Count - 1].CHAT_ID, opDialogInfo, int.Parse(item.ASSIGNED_BY_ID));
                                await _log.LogDelegate(this, $"Надсилаємо питання до gpt угода - {item.TITLE}", Enums.LogLevels.Info);
                                string projName = GetProjectName(projects_name_arr, item.TITLE);
                                await QueryToGptChatAsync(item.ID, clientQustions, projName);
                            }

                            if (!_listMainData.ContainsKey(item.ID))
                            {
                                _listMainData.Add(key: item.ID, value: new MainData()
                                {
                                    Deal = item,
                                    ContactID = int.Parse(item.CONTACT_ID),
                                    ClientName = nameClient,
                                    OL_OpentChatID = opChatsIDList[opChatsIDList.Count - 1].CHAT_ID,
                                    OL_DialogID = opDialogInfo,
                                    InpMgID_Last = lastMsgClientID
                                });
                            }
                            else
                            {
                                _listMainData[item.ID].InpMgID_Last = lastMsgClientID;
                            }
                        }
                        else await SetDublicatToListAsync(item);
                    }
                }
                catch (Exception ex)
                { await _log.LogDelegate(this, $"ERROR: {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error); }

                // delay 3 min
                await Task.Delay(180000, cancellationToken);
            } while (!cancellationToken.IsCancellationRequested);
        }

        /// <summary>
        /// если есть не отвеченные вопросы, мы их возвращаем а так же id последнего вопроса
        /// </summary>
        /// <param name="dealogHistory"></param>
        /// <param name="managerId"></param>
        /// <param name="lastMsgClientID"></param>
        /// <returns></returns>
        public string GetClientMsgWithoutManagerRiple(List<Dialog> dealogHistory,
                                                                string managerId,
                                                         ref int lastMsgClientID)
        {
            string clientQustions = null;

            List<Dialog> msgClient_List = dealogHistory.Where(x => int.Parse(x.SENDERID) != 0
                                && int.Parse(x.SENDERID) != int.Parse(_SETTINGS.BITRIX_ADMIN_ID)
                                && int.Parse(x.SENDERID) != int.Parse(managerId))
                                .OrderBy(m => m.ID)
                                .ToList();

            if (dealogHistory.Any(x => int.Parse(x.SENDERID) == int.Parse(managerId)))
            {
                string lastManagerMsgId = dealogHistory.Where(x => int.Parse(x.SENDERID) == int.Parse(managerId))?.OrderBy(x => x.ID)?.Last()?.ID;
                if (lastManagerMsgId != null)
                {
                    msgClient_List = msgClient_List.Where(x => int.Parse(x.ID) > int.Parse(lastManagerMsgId))
                                                   .OrderBy(m => m.ID).ToList();
                }
            }

            if (msgClient_List?.Count() > 0)
            {
                foreach (var item in msgClient_List)
                {
                    if (int.Parse(item.ID) > lastMsgClientID)
                    {
                        clientQustions += " " + item.TEXT;
                        lastMsgClientID = int.Parse(item.ID);
                    }
                }
            }
            return clientQustions;

            // если вопросов несколько за три минуты, то собираем в один
            // foreach (var msg in dialogHistory.MESSAGE)
            // {
            //     if (int.Parse(msg.Value.SENDERID) != 0
            //         && int.Parse(msg.Value.SENDERID) != int.Parse(item.ASSIGNED_BY_ID)
            //         && int.Parse(msg.Value.SENDERID) != int.Parse(_SETTINGS.BITRIX_ADMIN_ID))
            //     {
            //          long unixTime_msg = DateTimeOffset.Parse(msg.Value.DATE).ToUnixTimeSeconds();
            //         if (DateTimeOffset.Now.ToUnixTimeSeconds() - 180 < unixTime_msg
            //             && int.Parse(msg.Value.ID) > lastMsgClient)
            //         {
            //             Console.WriteLine($"PPPPPPPPPPPPPPP\n{msg.Value.TEXT}");
            //             clientQustions += " " + msg.Value.TEXT;
            //             lastMsgClient = int.Parse(msg.Value.ID);
            //         }
            //     }
            // }
        }

        /// <summary>
        /// дублікати додаємо до масиву щоб потім разом перенести до наступної стадії
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private async Task SetDublicatToListAsync(Deal item)
        {
            if (_dublikats.ContainsKey(item.CONTACT_ID))
            {
                if (_dublikats[item.CONTACT_ID].Any(x => x.ID != item.ID))
                {
                    _dublikats[item.CONTACT_ID].Add(item);
                    //додаємо ===дублікат у назву
                    await Deal_Bitrix.MoveDealToNextStageAsync(item.ID, $"C{item.CATEGORY_ID}:NEW", int.Parse(item.CATEGORY_ID), item.TITLE + " === Дубликат");
                }
            }
            else
            {
                _dublikats.Add(item.CONTACT_ID, new List<Deal> { item });
                //додаємо ===дублікат у назву
                await Deal_Bitrix.MoveDealToNextStageAsync(item.ID, $"C{item.CATEGORY_ID}:NEW", int.Parse(item.CATEGORY_ID), item.TITLE + " === Дубликат");
            }
            await _log.LogDelegate(this, $"Воронка {item.CATEGORY_ID}, {item.ID} - Угода дубльована, цей контакт {item.CONTACT_ID} вже є {item.TITLE}", Enums.LogLevels.Warning);
        }

        /// <summary>
        /// перевіряємо співпадіння контактів в угодах(клієнт може написати з кількох джерел)
        /// </summary>
        /// <param name="item"></param>
        /// <param name="nameClient"></param>
        /// <returns></returns>
        private bool IsDealDublicat(Deal item, string nameClient)
        {
            var isNoContact = _listMainData.Any(x => x.Value.Deal.CONTACT_ID != item.CONTACT_ID);
            if (isNoContact)
            {
                isNoContact = !_listMainData.Any(x => !nameClient.Contains("Гость")
                                 && (x.Value.ClientName.Contains(nameClient,
                                            StringComparison.InvariantCultureIgnoreCase)
                                 && !x.Value.Deal.SOURCE_ID.Split("|")[1].Contains(item.SOURCE_ID.Split("|")[1],
                                                      StringComparison.InvariantCultureIgnoreCase)
                                 && DateTimeOffset.Parse(x.Value.Deal.DATE_CREATE).ToUnixTimeSeconds()
                                    - DateTimeOffset.Parse(item.DATE_CREATE).ToUnixTimeSeconds()
                                    < 800));
            }
            return isNoContact;
        }

        /// <summary>
        /// True - якщо менеджер відповів(тобто останнє повідомлення від менеджера)
        /// </summary>
        /// <param name="dealogHistory"></param>
        /// <param name="managerId"></param>
        /// <returns></returns>
        public bool IS_ManagerHasReplied(List<Dialog> dealogHistory, string managerId)
        {
            Dialog lastManagerMessage = null;
            List<Dialog> messages = dealogHistory.OrderBy(m => DateTime.Parse(m.DATE)).ToList();

            //Шукаємо останнє повідомлення клієнта
            Dialog lastClientMessage = messages.LastOrDefault(x => int.Parse(x.SENDERID) != 0
                                    && int.Parse(x.SENDERID) != int.Parse(managerId)
                                    && int.Parse(x.SENDERID) != int.Parse(_SETTINGS.BITRIX_ADMIN_ID));

            if (lastClientMessage != null)
            {
                //Шукаємо перше повідомлення менеджера після lastClientMessage
                lastManagerMessage = messages.FirstOrDefault(m => int.Parse(m.SENDERID) == int.Parse(managerId)
                                     && DateTime.Parse(m.DATE) > DateTime.Parse(lastClientMessage.DATE));
            }
            return lastManagerMessage != null;
        }

        private async Task QueryToGptChatAsync(int userID, string userQuestion, string projectName)
        {
            await Task.Run(async () =>
            {
                try
                {
                    string clientChatHistory = await GetChatHistory.GetListHistoryAsync(userID.ToString(), _log);
                    //ставимо питання у чергу
                    await QueueToGpt.SetToQueueAsync(userID, userQuestion, clientChatHistory, projectName, _responseChatGpt_delegate);
                }
                catch (Exception ex) { await _log.LogDelegate(this, $"Error send to gpt {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error); }
            });
        }

        /// <summary>
        /// метод обратного вызова. возвращает ответ из gptchat class и оправляет в телеграм и в файл
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="questions"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private async Task SendMsgResponseAsync(int userID, string questions, string response)
        {
            //get deal/client data
            var deal = new MainData();
            //якщо вже немає у масиві то не надсилаємо відповідь
            if (_listMainData.TryGetValue(userID, out deal))
            {
                //прибираємо символи нової строки для збереження у файлі історії чата
                string quest = questions.Replace(System.Environment.NewLine, string.Empty);
                string resp = response.Replace(System.Environment.NewLine, string.Empty);

                if (response.Contains(_SETTINGS.SEND_MANAGER, StringComparison.InvariantCultureIgnoreCase)
                    || Verification.CheckStr(response, _SETTINGS.SEND_MANAGER))
                {
                    await _log.LogMsgDelegate(userID.ToString(), $"Питання: {quest}\nВідповідь: {resp}. Відправлено сповіщення: 'Треба участь менеджера'");
                    await MoveFile_To_Archive.File_MoveArchivAsync(userID.ToString(), _log);
                    //отправка уведомления в битрикс
                    await SendTo_Menager_BitrixAsync(deal.Deal, deal.OL_OpentChatID, deal.Deal.CATEGORY_ID, $"🔔❌ Треба участь менеджера! від клієнта - ", deal.Deal.TITLE + " === Отвечено");
                    // видаляємо не потрібну угоду
                    _listMainData.Remove(deal.Deal.ID);
                }
                else if (response.Contains(_SETTINGS.DATA_COLLECTED, StringComparison.InvariantCultureIgnoreCase)
                         || Verification.CheckStr(response, _SETTINGS.DATA_COLLECTED)
                         || Verification.CheckStr(response, "зібрані дані"))
                {
                    await _log.LogMsgDelegate(userID.ToString(), $"Питання: {quest}\nВідповідь: {resp} Відправлено сповіщення: 'Дані зібрано.'");
                    await MoveFile_To_Archive.File_MoveArchivAsync(userID.ToString(), _log);
                    //отправка уведомления в битрикс
                    await SendTo_Menager_BitrixAsync(deal.Deal, deal.OL_OpentChatID, deal.Deal.CATEGORY_ID, "🔔✅ Дані для друку зібрано! від клієнта - ", deal.Deal.TITLE + " === Отвечено");
                    // видаляємо не потрібну угоду
                    _listMainData.Remove(deal.Deal.ID);
                }
                else
                {
                    //відповідь від чату надсилаємо у бітрікс
                    // await Deal_Bitrix.JoinOpenLineSessionAsync(deal.DIALOG_SESSIONID, int.Parse(deal.Deal.ASSIGNED_BY_ID), deal.OL_OpentChatID);
                    await Deal_Bitrix.AnswerOpenLineDialogAsync(deal.OL_OpentChatID, int.Parse(deal.Deal.ASSIGNED_BY_ID));
                    if (await Deal_Bitrix.SendMsgToOpenLineAsync("DEAL", deal.Deal.ID, int.Parse(_SETTINGS.BITRIX_ADMIN_ID), deal.OL_OpentChatID, response))
                        await _log.LogMsgDelegate(userID.ToString(), $"Питання: {quest}\nВідповідь: {resp}");
                    else
                    {
                        //після помилки відправки:
                        // видаляємо угоду
                        _listMainData.Remove(deal.Deal.ID);
                        //Видаляємо список дублікатів
                        _dublikats.Remove(deal.ContactID.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Відправляємо сповіщення менеджеру та переміщуємо угоду
        /// до наступної стадії
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private async Task SendTo_Menager_BitrixAsync(Deal deal, int chatID, string voronkaID, string text, string dealTitle)
        {
            string msgBitrix = $"{text}{deal.CONTACT_ID}";
            //перемещаем в отвеченные
            string stageName = (voronkaID == "4") ? "UC_8ZPNTY" : "PREPARATION";

            await Deal_Bitrix.MoveDealToNextStageAsync(deal.ID, $"C{voronkaID}:{stageName}", int.Parse(voronkaID), dealTitle);
            // переадресація діалогу іншому менеджеру
            await SendToBitrix.TransferOpenLineDialogAsync(chatID, deal.ASSIGNED_BY_ID);
            // Відправляємо сповіщення менеджеру 
            await SendToBitrix.SendToMenegerAsync(deal.ASSIGNED_BY_ID, msgBitrix);
            await MoveDublicatAsync(deal.CONTACT_ID, $"C{voronkaID}:{stageName}", voronkaID);
        }

        /// <summary>
        /// Переміщуємо дублікати якщо є
        /// </summary>
        /// <param name="contactID">клієнт</param>
        /// <param name="stageName">стадія у бітрікс на яку треба перемістити угоду</param>
        /// <param name="voronkaID">воронка у битриксі</param>
        /// <returns></returns>
        private async Task MoveDublicatAsync(string contactID, string stageName, string voronkaID)
        {
            try
            {
                //Перемещение на следующую стадию, дубликатов
                List<Deal> tempList = [];
                if (_dublikats.TryGetValue(contactID, out tempList))
                {
                    foreach (var item in tempList)
                    {
                        await Deal_Bitrix.MoveDealToNextStageAsync(item.ID, stageName, int.Parse(voronkaID), item.TITLE);// + " === Дубликат");
                    }
                    //Видаляємо список дублікатів, після переміщення угод
                    _dublikats.Remove(contactID);
                }
            }
            catch (Exception ex) { await _log.LogDelegate(this, $"ERROR move Dublicat {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error); }
        }

        private string GetProjectName(string[] projects_name_arr, string dealTitle)
        {
            switch (projects_name_arr.FirstOrDefault(x => dealTitle.Contains(x,
                                 StringComparison.InvariantCultureIgnoreCase)))
            {
                case "Page":
                    return "Page";
                case "Веселка":
                    return "Веселка";
                case "Kyivphoto":
                case "Київфото":
                    return "Kyivphoto";
                default:
                    return "data_chat";
            }
        }
    }
}