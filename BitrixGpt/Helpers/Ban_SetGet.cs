

using BitrixGpt.Constants;
using BitrixGpt.Logs;


namespace BitrixGpt.Helpers
{
    /// <summary>
    /// Здесь сохраняем в файл userId, которым бот отвечать не сможет, а только менеджер
    /// Или удаляем userId если уже можно боту общаться
    /// </summary>
    static class Ban_SetGet
    {


        public static ILog Log { set; private get; }
        private static List<string> _banList = [];


        static Ban_SetGet()
        {
            _ = GetListFromFileAsync();
        }

        /// <summary>
        /// if there is a user id, we will return true
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool Is_UserId_Ban(long userId)
        {
            return _banList.Contains(userId.ToString());
        }

        /// <summary>
        /// Set to file one userId
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task SetToFileAsync(long userId)
        {
            _banList.Add(userId.ToString());//add new userId

            try
            {
                using StreamWriter sw = File.AppendText(ConstantFolders.BAN_FILE_PATH);
                await sw.WriteAsync(userId.ToString() + ";");
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Ban_SetGet), $"ERROR SetToFileAsync {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
        }

        /// <summary>
        /// remove userId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task DeleteUserId_BanAsync(long userId)
        {
            try
            {
                if (_banList.Remove(userId.ToString() + ";"))
                {
                    string text = "";
                    if (_banList != null && _banList.Count > 0)
                    {
                        foreach (var item in _banList)
                        {
                            text += item + ";";
                        }

                        await File.WriteAllTextAsync(ConstantFolders.BAN_FILE_PATH, text);
                    }
                }
                else
                    await Log.LogDelegate(typeof(Ban_SetGet), $"ERROR: DeleteUserId_BanAsync не взмозі записати оновлений файл", Enums.LogLevels.Error);
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Ban_SetGet), $"ERROR DeleteUserId_BanAsync {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
        }

        /// <summary>
        /// get list userId from file
        /// </summary>
        /// <returns></returns>
        private static async Task GetListFromFileAsync()
        {
            try
            {
                if (File.Exists(ConstantFolders.BAN_FILE_PATH))
                {
                    string txt = File.ReadAllText(ConstantFolders.BAN_FILE_PATH);
                    _banList = new List<string>(txt.Split(";"));
                }
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(Ban_SetGet), $"ERROR GetListFromFile {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }
        }
    }
}