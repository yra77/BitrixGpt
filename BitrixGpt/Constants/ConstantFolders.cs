

namespace BitrixGpt.Constants
{
    internal class ConstantFolders
    {
        public static readonly string BASE_FOLDER = Directory.GetCurrentDirectory();
        public static readonly string LOGS_FOLDER = BASE_FOLDER + "\\Logs\\";
        public static readonly string MSG_FOLDER = LOGS_FOLDER + "Messages\\";
        public static readonly string BAN_FOLDER = BASE_FOLDER + "\\BanList\\";
        public static readonly string ARHIV_FOLDER = LOGS_FOLDER + "\\Archive\\" + DateTime.Now.Date.ToString().Split(' ').First() + "\\";
        public static string DATASET_PATH = BASE_FOLDER;// + "/data_chat.json";
        public static string BAN_FILE_PATH = BAN_FOLDER + "ban_list.txt";
        public static string SETTINGS_PATH = BASE_FOLDER + "\\Settings\\Settings.json";
    }
}