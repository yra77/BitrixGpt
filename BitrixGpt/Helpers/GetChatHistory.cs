

using BitrixGpt.Logs;
using System.Text;


namespace BitrixGpt.Helpers
{
    class GetChatHistory
    {

        public static async Task<string> GetListHistoryAsync(string fileName, ILog log)
        {
            if (File.Exists(Constants.ConstantFolders.MSG_FOLDER + fileName + ".txt"))
            {
                string[] text = await File.ReadAllLinesAsync(Constants.ConstantFolders.MSG_FOLDER + fileName + ".txt");
                await log.LogDelegate(typeof(GetChatHistory), $"Додаємо історію чату з клієнтом {fileName}.txt", Enums.LogLevels.Info);
                return AddListToString(text);
            }
            else
            {
                await log.LogDelegate(typeof(GetChatHistory), $"Історії чату {fileName}.txt не знайдено", Enums.LogLevels.Info);
                return null;
            }
        }

        private static string AddListToString(string[] text)
        {
            List<(string Role, string Message)> chatHistory = [];

            foreach (var item in text)
            {
                //обрізаємо до першого пробілу
                int index = item.IndexOf(' ') + 1;
                var result = item.Substring(index, item.Length - index);

                try
                {
                    string firstWord = item.Substring(0, index - 1);

                    if (firstWord == "Питання:")
                    {
                        chatHistory.Add(("Клієнт", result));
                    }
                    else if (firstWord == "Відповідь:")
                    {
                        chatHistory.Add(("Менеджер", result));
                    }
                }
                catch
                { continue; }
            }

            StringBuilder history = new();
            foreach (var message in chatHistory)
            {
                history.AppendLine($"{message.Role}: {message.Message}");
            }
            return history.ToString();
        }
    }
}