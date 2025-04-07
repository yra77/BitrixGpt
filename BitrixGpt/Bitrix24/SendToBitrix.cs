

using BitrixGpt.Logs;
using Newtonsoft.Json;
using System.Text;


namespace BitrixGpt.Bitrix24
{
    class SendToBitrix
    {


        public static ILog Log { set; private get; }
        public static string WebhookUrl { private get; set; }
        private static HttpClient _client = new HttpClient();


        /// <summary>
        /// Відправка сповіщення менеджеру
        /// </summary>
        /// <param name="manangerID">DIALOG_ID такий самий</param>
        /// <param name="msg"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task SendToMenegerAsync(string manangerID, string msg)
        {
            //"chat_id":2321
            //chat_id":2520 люда? 5
            //катя? - 3012

            try
            {
                var payload = new
                {
                    DIALOG_ID = manangerID,
                    MESSAGE = msg
                };

                string jsonData = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(WebhookUrl + "im.message.add.json", content);
                if (response.IsSuccessStatusCode)
                {
                    // string responseString = await response.Content.ReadAsStringAsync();
                    await Log.LogDelegate(typeof(SendToBitrix), $"Сповіщення відправлено у битрикс", Enums.LogLevels.Info);
                }
                else
                    await Log.LogDelegate(typeof(SendToBitrix), $"Error: сповіщення не відправлено до bitrix", Enums.LogLevels.Error);
            }
            catch (Exception ex)
            { await Log.LogDelegate(typeof(SendToBitrix), $"Error send to bitrix {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error); }
        }

        /// <summary>
        /// Метод переадресації діалогу іншому менеджеру в Бітрікс24
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="transferId">визначає, чи передаємо чат конкретному оператору (його USER_ID) або в чергу (queue#ID_лінії).</param>
        /// <returns></returns>
        public static async Task<bool> TransferOpenLineDialogAsync(int chatId, string transferId)
        {
            var requestData = new
            {
                CHAT_ID = chatId,   // ID чату, який потрібно переадресувати
                TRANSFER_ID = transferId // ID менеджера або "queue#ID_лінії"
            };

            try
            {
                string json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(WebhookUrl + "imopenlines.operator.transfer.json", content);

                string responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (response.IsSuccessStatusCode && result?.result == true)
                {
                    await Log.LogDelegate(typeof(SendToBitrix), $"Чат {chatId} успішно передано {transferId}", Enums.LogLevels.Success);
                    return true;
                }
                else
                {
                    await Log.LogDelegate(typeof(SendToBitrix), $"Помилка передачі чату іншому менеджеру {transferId}:\n{responseContent}", Enums.LogLevels.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(SendToBitrix), $"Помилка запиту до Бітрікс24: {ex.Message}", Enums.LogLevels.Error);
                return false;
            }
        }

        /// <summary>
        /// список чатів робітників
        /// </summary>
        /// <returns></returns>
        public static async Task GetListChatAsync()
        {
            HttpResponseMessage response = await _client.GetAsync(WebhookUrl + "im.recent.list.json");
            string responseString = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseString);
        }
    }
}