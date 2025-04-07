

using BitrixGpt.Constants;
using BitrixGpt.Bitrix24;
using BitrixGpt.Helpers;
using BitrixGpt.Logs;

using Newtonsoft.Json;
using System.Text;


namespace BitrixGpt.ConnectOpenAI
{
    class GptChat
    {


        public static ILog Log { set; private get; }
        public static Settings_Prop SETTINGS { private get; set; }
        //Записываем кому ответили(предупрелили о выходных) на выходных, чтобы отправить только один раз)
        //и очищаем в рабочее время
        // private static List<long> _IsSendOnWeekends = [];


        /// <summary>
        /// Запрос к gpt-chat
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="userQuestion"></param>
        /// <param name="clientChatHistory"></param>
        /// <param name="returnResponse">зворотній виклик</param>
        /// <returns>Зворотній виклик, Відповідь чату</returns>
        public static async Task StartAsync(int userID, string userQuestion, string clientChatHistory, string projectName, responseChatGpt returnResponse)
        {
            try
            {
                // Завантаження даних з JSON-файлу
                var data = await LoadJsonDataAsync(ConstantFolders.DATASET_PATH + $"/{projectName}.json");
                // Формування контексту
                string context = FormatContext(data);

                //Якщо вихідні з п`ятниці після 18-00
                // if (!_IsSendOnWeekends.Contains(userID))
                // {
                //     if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday
                //         || DateTime.Now.DayOfWeek == DayOfWeek.Sunday
                //         || (DateTime.Now.DayOfWeek == DayOfWeek.Friday && DateTime.Now.Hour > 18))
                //     {
                //         await returnResponse(userID, userQuestion, SETTINGS.WEEKEND_TEXT);
                //         _IsSendOnWeekends.Add(userID);
                //         return;
                //     }

                // if (DateTime.Now.Hour > 18 && DateTime.Now.Hour < 9)
                // {
                //     await returnResponse(userID, userQuestion, "Вибачте, у нас робочий час (Пн-Пт, 09:00-18:00). Прорахунок можемо зробити у робочий час. Вас так влаштовує?");
                //     _IsSendOnWeekends.Add(userID);
                //     return;
                // }
                // }
                // //очищуємо, у робочі дні
                // if (_IsSendOnWeekends.Count > 0 && (DateTime.Now.DayOfWeek != DayOfWeek.Saturday
                //    && DateTime.Now.DayOfWeek != DayOfWeek.Sunday
                //    || (DateTime.Now.DayOfWeek == DayOfWeek.Friday && DateTime.Now.Hour < 18)))
                // {
                //     _IsSendOnWeekends.Clear();
                // }

                // запит до моделі
                string response = await QueryGPT4Async(userQuestion, context, clientChatHistory);
                //return to telegram class
                if (response != null)
                {
                    await returnResponse(userID, userQuestion, response);
                }
            }
            catch (Exception ex)
            { await Log.LogDelegate(typeof(GptChat), $"Error: {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error); }
        }

        /// <summary>
        /// Завантаження даних з JSON
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static async Task<dynamic> LoadJsonDataAsync(string filePath)
        {
            string json = await File.ReadAllTextAsync(filePath);
            return JsonConvert.DeserializeObject(json);
        }

        /// <summary>
        /// Форматування контексту з даних
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string FormatContext(dynamic data)
        {
            StringBuilder contextLines = new();
            foreach (var item in data)
            {
                string prompt = item.prompt ?? "Невідоме питання";
                string response = item.response ?? "Немає відповіді";
                contextLines.AppendLine($"Клієнт: {prompt}\nМенеджер: {response}\n");
            }
            return contextLines.ToString();
        }

        /// <summary>
        /// Функція для запиту до GPT-4
        /// </summary>
        /// <param name="userInput"></param>
        /// <param name="context"></param>
        /// <param name="chatHistory"></param>
        /// <returns></returns>
        private static async Task<string> QueryGPT4Async(string userInput, string context, string chatHistory = null)
        {
            try
            {
                // Історія чату з клієнтом, якщо не має = null
                if (chatHistory != null)
                {
                    context += $"\nТакож ось історія спілкування з цим клієнтом:\n{chatHistory}";
                }

                string weekEnd = "";
                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday
                    || DateTime.Now.DayOfWeek == DayOfWeek.Sunday
                    || (DateTime.Now.DayOfWeek == DayOfWeek.Friday && DateTime.Now.Hour > 18))
                {
                    weekEnd = SETTINGS.WEEKEND_TEXT;
                }

                using (HttpClient client = new HttpClient())
                {
                    // Налаштування заголовків для запиту
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + SETTINGS.OPENAI_KEY);

                    var requestBody = new
                    {
                        model = SETTINGS.MODEL_GPT,
                        messages = new[]
                        {
                    new { role = "system", content = SETTINGS.INSTRUCTS_FOR_GPT + SETTINGS.SEND_MANAGER + weekEnd },
                    // new { role = "system", content = $"Ви є ботом, який відповідає на запити клієнтів у сфері поліграфії, цін не пиши на друк, потрібно зібрати у клієнта всі дані для прорахунку друку. Якщо усі дані зібрано, додайте фразу {Text_Constant.DATA_COLLECTED}.Якщо клієнт допускає помилки в тексті, виправляйте їх і намагайтеся зрозуміти значення за контекстом.Якщо запит складний, неоднозначний або потребує людської участі, додайте фразу {Text_Constant.SEND_MANAGER}" },
                    // new { role = "system", content = "Наші робочі години: Понеділок - П’ятниця, 09:00 - 18:00. Субота та Неділя — вихідні дні. Якщо клієнт звертається у вихідні, ввічливо повідомте, що прорахунок його замовлення буде зроблен у робочий час. Але все рівно треба зібрати у клієнта дані для прорахунку друку." },
                    //new { role = "system", content = "Ви є ботом, який відповідає на запити клієнтів у сфері поліграфії, тільки друк візиток, цін не пиши на друк, потрібно зібрати у клієнта дані для прорахунку друку візиток, а потім написати фразу 'Дані зібрані'.Якщо клієнт допускає помилки в тексті, виправляйте їх і намагайтеся зрозуміти значення за контекстом.Якщо запит складний, неоднозначний або потребує людської участі, додайте фразу 'Потрібна перевірка менеджера'" },
                    //new { role = "system", content = "Ви є ботом, який відповідає на запити клієнтів у сфері поліграфії.Якщо клієнт допускає помилки в тексті, виправляйте їх і намагайтеся зрозуміти значення за контекстом.Якщо запит складний, неоднозначний або потребує людської участі, додайте фразу 'Потрібна перевірка менеджера'" },
                    new { role = "system", content = $"Ось попередні дані для вашої роботи:\n{context}" },
                    new { role = "user", content = userInput }
                    }
                    };

                    string jsonRequest = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    // Відправка POST запиту
                    HttpResponseMessage response = await client.PostAsync(SETTINGS.OPENAI_PATH, content);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Обробка відповіді
                    dynamic result = JsonConvert.DeserializeObject(responseContent);
                    return result.choices[0].message.content;
                }
            }
            catch (Exception ex)
            { await Log.LogDelegate(typeof(GptChat), $"Error: {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error); }
            
            return null;
        }
    }
}