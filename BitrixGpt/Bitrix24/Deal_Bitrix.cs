

using BitrixGpt.Bitrix24.Models;
using BitrixGpt.Logs;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;


namespace BitrixGpt.Bitrix24
{
    class Deal_Bitrix
    {


        public static ILog Log { set; private get; }
        public static string WebhookUrl { private get; set; }
        private static HttpClient _client = new HttpClient();


        public static async Task GetCrmStatusesAsync()
        {
            HttpResponseMessage response = await _client.GetAsync(WebhookUrl + "crm.status.list.json");
            string jsonResponse = await response.Content.ReadAsStringAsync();
            // JObject result = JObject.Parse(jsonResponse);
            // return result["result"] as JArray;
            Console.WriteLine(jsonResponse);
        }

        /// <summary>
        /// Редагування угоди тобто можна змінювати стадії, воронки, відповідальних і т.д.
        /// </summary>
        /// <param name="dealId"></param>
        /// <param name="nextStageId"></param>
        /// <param name="categoryID"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static async Task MoveDealToNextStageAsync(int dealId, string nextStageId, int categoryID, string title)
        {
            var requestData = new
            {
                ID = dealId,
                FIELDS = new { STAGE_ID = nextStageId, CATEGORY_ID = categoryID, TITLE = title }
            };
            try
            {
                string json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(WebhookUrl + "crm.deal.update.json", content);
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (title.Contains(" === Дубликат", StringComparison.InvariantCultureIgnoreCase))
                        await Log.LogDelegate(typeof(Deal_Bitrix), $"Success: Угода {dealId} - додано ===Дубликат", Enums.LogLevels.Success);
                    else
                        await Log.LogDelegate(typeof(Deal_Bitrix), $"Success: Угоду пересунуто до наступної стадії\n{requestData}", Enums.LogLevels.Success);
                }
            }
            catch (Exception ex) { await Log.LogDelegate(typeof(Deal_Bitrix), $"ERROR: \n{ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error); }
        }

        /// <summary>
        /// отправляем сообщение в диалог(открытой линии) с клиентом
        /// </summary>
        /// <param name="crm_entity_type">DEAL, CONTACT, LEAD</param>
        /// <param name="crm_entity_id">id сделки, контакта, лида</param>
        /// <param name="userID"></param>
        /// <param name="chatID"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task<bool> SendMsgToOpenLineAsync(string crm_entity_type, int crm_entity_id, int userID, int chatID, string message)
        {
            var requestData = new
            {
                CRM_ENTITY_TYPE = crm_entity_type,
                CRM_ENTITY = crm_entity_id,
                USER_ID = userID,
                CHAT_ID = chatID,
                MESSAGE = message
            };

            try
            {
                using HttpClient client = new HttpClient();
                string json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(WebhookUrl
                                               + "imopenlines.crm.message.add.json", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return true;
                }
                else
                {
                    string errorJoin = await response.Content.ReadAsStringAsync();
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Error bitrix SendMsgToOpenLineAsync не вдалося відправити повідомлення. {errorJoin}", Enums.LogLevels.Error);
                }
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Error bitrix SendMsgToOpenLineAsync {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }

            return false;
        }

        /// <summary>
        /// Get dialog session id
        /// </summary>
        /// <param name="openLineChatID"></param>
        /// <returns></returns>
        public static async Task<int> GetOpenLineDialogInfoAsync(int openLineChatID)
        {
            var requestData = new { CHAT_ID = openLineChatID };

            try
            {
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(WebhookUrl + "imopenlines.dialog.get.json", content);

                if (response.IsSuccessStatusCode)
                {
                    var openLinesDialogResult = JsonConvert.DeserializeObject<BitrixResponse<OpenLineDialogInfo>>(await response.Content.ReadAsStringAsync());

                    if (openLinesDialogResult.Result.ID > 0)
                        return int.Parse((openLinesDialogResult.Result.ENTITY_DATA_1).Split("|")[5]);
                    else
                        await Log.LogDelegate(typeof(Deal_Bitrix), $"Warning: Відкритих діалогів не знайдено.", Enums.LogLevels.Warning);
                }
                else
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Error: bitrix: не вдалося получити відкриті діалогів", Enums.LogLevels.Error);
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Error bitrix GetOpenLineDialogInfoAsync {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
            return 0;
        }

        public static async Task<DialogHistory> GetOpenLineChatHistoryAsync(int sessionID)
        {
            var requestData = new { SESSION_ID = sessionID };

            try
            {
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response =
                    await _client.PostAsync(WebhookUrl + "imopenlines.session.history.get.json",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var oLChatHistoryResult = JsonConvert.DeserializeObject<BitrixResponse<DialogHistory>>(await response.Content.ReadAsStringAsync());
                    if (oLChatHistoryResult.Result != null) return oLChatHistoryResult.Result;
                    else
                        await Log.LogDelegate(typeof(Deal_Bitrix), $"Warning: Історії діалогу не знайдено.", Enums.LogLevels.Warning);
                }
                else
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Error: bitrix: не вдалося получити історії діалогу", Enums.LogLevels.Error);
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Error bitrix GetOpenLineChatHistoryAsync {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
            return null;
        }

        /// <summary>
        /// Поверне чати які має клієнт
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="isOpenChat">Y - open chats only, N - all chats</param>
        /// <returns></returns>
        public static async Task<List<OpenLineChat>> GetOpenLineChatIDAsync(int clientID, bool isOpenChat)
        {
            try
            {
                var requestData = new
                {
                    CRM_ENTITY_TYPE = "CONTACT",  // або LEAD, DEAL
                    CRM_ENTITY = clientID,         // ID контакту або ліда
                    ACTIVE_ONLY = isOpenChat ? "Y" : "N"
                };

                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(WebhookUrl + "imopenlines.crm.chat.get.json", content);

                if (response.IsSuccessStatusCode)
                {
                    var openLinesChatResult = JsonConvert.DeserializeObject<BitrixResponse<List<OpenLineChat>>>(await response.Content.ReadAsStringAsync());
                    if (openLinesChatResult.Result.Count > 0) return openLinesChatResult.Result;
                    else await Log.LogDelegate(typeof(Deal_Bitrix), $"Warning: Відкритих чатів не знайдено.", Enums.LogLevels.Warning);
                }
                else
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Error: bitrix: не вдалося получити відкриті чати", Enums.LogLevels.Error);
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Error bitrix GetOpenLineChatIDAsync {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
            return null;
        }

        /// <summary>
        /// Пошук контакту за ім'ям, номером телефону
        /// </summary>
        /// <param name="clientName"></param>
        /// <param name="phone_num"></param>
        /// <returns></returns>
        public static async Task<List<Contact>> GetClientInfoAsync(string clientName, string phone_num = null)
        {
            try
            {
                var contactPayload = new { filter = new { NAME = clientName }, select = new[] { "ID", "NAME", "LAST_NAME", "PHONE", "SOURCE_ID", "IM" } };
                var contactPayload_num = new { filter = new { NAME = clientName, PHONE = phone_num }, select = new[] { "ID", "NAME", "LAST_NAME", "PHONE", "SOURCE_ID", "IM" } };

                var contactResponse = await _client.PostAsync(WebhookUrl + "crm.contact.list.json",
                    new StringContent(JsonConvert.SerializeObject((phone_num == null) ? contactPayload : contactPayload_num), Encoding.UTF8, "application/json"));

                if (contactResponse.IsSuccessStatusCode)
                {
                    var contactResult = JsonConvert.DeserializeObject<BitrixResponse<List<Contact>>>(await contactResponse.Content.ReadAsStringAsync());
                    if (contactResult.Result.Count > 0) return contactResult.Result;
                    else
                        await Log.LogDelegate(typeof(Deal_Bitrix), $"Warning: bitrix не знайдено клієнта ID", Enums.LogLevels.Warning);
                }
                else
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Error: bitrix не вдалося получити клієнта ID", Enums.LogLevels.Error);
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Error bitrix GetClientInfoAsync {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
            return null;
        }

        /// <summary>
        /// Пошук відкритої угоди
        /// </summary>
        /// <param name="menegerID">id менеджера</param>
        /// <param name="voronkaID">id воронки</param>
        /// <param name="contactId">id клиента</param>
        /// <param name="stageID">название (id) стадии</param>
        /// <returns></returns>
        public static async Task<List<Deal>> GetDealAsync(string voronkaID, string menegerID = null, string contactId = null)
        {
            //"C4:NEW" "C5:PREPAYMENT_INVOICE"-оплата, "C5:PREPAYMENT"-отвеченно
            try
            {
                string stageID = $"C{voronkaID}:NEW";
                //  var dealPayload = new { filter = new { CONTACT_ID = contactId, STAGE_ID = stageID, ASSIGNED_BY_ID = menegerID, CATEGORY_ID = voronkaID }, select = new[] { "ID", "TITLE", "CATEGORY_ID", "STAGE_ID", "ASSIGNED_BY_ID" } };
                var dealPayload = new { filter = new { STAGE_ID = stageID, CATEGORY_ID = voronkaID, CLOSED = "N", STAGE_SEMANTIC_ID = "P" }, select = new[] { "*" } };
                var dealResponse = await _client.PostAsync(WebhookUrl + "crm.deal.list.json",
                    new StringContent(JsonConvert.SerializeObject(dealPayload), Encoding.UTF8, "application/json"));

                if (dealResponse.IsSuccessStatusCode)
                {
                    var dealResult = JsonConvert.DeserializeObject<BitrixResponse<List<Deal>>>(await dealResponse.Content.ReadAsStringAsync());
                    if (dealResult.Result.Count > 0) return dealResult.Result;
                    else
                        await Log.LogDelegate(typeof(Deal_Bitrix), $"Warning: Воронка - {voronkaID}, Відкритих угод не знайдено.", Enums.LogLevels.Warning);
                }
                else
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Error: bitrix: не вдалося получити відкриті угоди", Enums.LogLevels.Error);
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Error bitrix GetDealAsync {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
            return null;
        }

        /// <summary>
        /// Метод для прийняття чату оператором
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="sessionId"></param>
        /// <param name="menegerId"></param>
        /// <returns></returns>
        public static async Task AnswerOpenLineDialogAsync(int chatId, int menegerId)
        {
            var requestData = new
            {
                CHAT_ID = chatId,
                // SESSION_ID = sessionId,
                USER_ID = menegerId  // Вказуємо менеджера, який відповідає
            };

            try
            {
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(WebhookUrl + "imopenlines.operator.answer.json", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    if (result["result"] == true)
                    {
                        await Log.LogDelegate(typeof(Deal_Bitrix), $"✅ Оператор {menegerId} успішно відповів (Chat ID: {chatId})", Enums.LogLevels.Info);
                    }
                    else
                    {
                        await Log.LogDelegate(typeof(Deal_Bitrix), $"⚠️ Не вдалося відповісти (Chat ID: {chatId}): {responseContent}", Enums.LogLevels.Warning);
                    }
                }
                else
                {
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"⚠️ Помилка підключення до Bitrix24 API: {response.StatusCode}", Enums.LogLevels.Warning);
                }
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Error AnswerOpenLineDialogAsync {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
        }

        /// <summary>
        /// Приєднання менеджера до діалогу
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="userId">ID менеджера</param>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public static async Task JoinOpenLineSessionAsync(int sessionId, int userId, int chatId)
        {
            var requestData = new
            {
                SESSION_ID = sessionId, // ID сесії відкритої лінії
                USER_ID = userId,        // ID менеджера, який приєднується
                CHAT_ID = chatId
            };

            try
            {
                string json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(WebhookUrl + "imopenlines.session.join.json", content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Успішно приєднано до сесії: {responseContent}", Enums.LogLevels.Info);
                }
                else
                {
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Помилка приєднання до сесії: {responseContent}", Enums.LogLevels.Error);
                }
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Помилка у JoinOpenLineSessionAsync: {ex.Message}", Enums.LogLevels.Error);
            }
        }

        /// <summary>
        /// Создание новой сделки
        /// </summary>
        /// <param name="managerId"></param>
        /// <param name="openLineTitle"></param>
        /// <param name="clientName"></param>
        /// <param name="phone_num"></param>
        /// <returns></returns>
        public static async Task CreateNewDealAsync(string openLineTitle, string clientName, string phone_num, string managerID, string voronkaID)
        {
            try
            {
                string stageID = $"C{voronkaID}:NEW";
                var contactId = await GetClientInfoAsync(clientName, phone_num);

                if (contactId == null)
                {
                    await Log.LogDelegate(typeof(Deal_Bitrix), "Клієнт не знайдений. Створення угоди неможливе.", Enums.LogLevels.Warning);
                    return;
                }

                var dealPayload = new
                {
                    fields = new
                    {
                        TITLE = $"Запит з {openLineTitle} - {clientName}",
                        ASSIGNED_BY_ID = managerID,
                        CONTACT_ID = contactId[0].ID,
                        CATEGORY_ID = voronkaID,// ID воронки (1 - стандартна)
                        STAGE_ID = stageID
                    }
                };

                var dealResponse = await _client.PostAsync(WebhookUrl + "crm.deal.add.json",
                    new StringContent(JsonConvert.SerializeObject(dealPayload), Encoding.UTF8, "application/json"));
                var dealResult = JsonConvert.DeserializeObject<dynamic>(await dealResponse.Content.ReadAsStringAsync());

                if (dealResult.result != null)
                {
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Створена угода: ID {dealResult.result}", Enums.LogLevels.Success);
                }
                else
                {
                    await Log.LogDelegate(typeof(Deal_Bitrix), "Помилка при створенні угоди.", Enums.LogLevels.Error);
                }
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Помилка у CreateNewDealAsync: {ex.Message}", Enums.LogLevels.Error);
            }
        }

        /// <summary>
        /// повертає список відкритих ліній
        /// </summary>
        /// <returns></returns>
        public static async Task<List<OpenLine>> GetOpenLinesAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.PostAsync(WebhookUrl + "imopenlines.config.list.get.json", null);
                string responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

                if (jsonResponse["result"] != null)
                {
                    var lines = jsonResponse["result"].ToObject<List<OpenLine>>();
                    return lines;
                }
                else
                {
                    await Log.LogDelegate(typeof(Deal_Bitrix), $"Error: Не вдалося отримати список відкритих ліній. Response: {responseContent}", Enums.LogLevels.Error);
                }
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Deal_Bitrix), $"Error in GetOpenLinesAsync: {ex.Message}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }

            return null;
        }
    }
}