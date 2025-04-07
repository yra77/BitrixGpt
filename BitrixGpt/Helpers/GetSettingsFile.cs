

using BitrixGpt.Logs;
using Newtonsoft.Json;


namespace BitrixGpt.Helpers
{

    public class Settings_Prop
    {
        public string OPENAI_KEY { get; set; }
        public string OPENAI_PATH { get; set; }
        public string MODEL_GPT { get; set; }
        //текст от бота битрикс сделка закрыта 
        public string DEAL_CLOSED { get; set; }
        //текст когда нужно вмешательство менеджера
        public string SEND_MANAGER { get; set; }
        //даные собрано
        public string DATA_COLLECTED { get; set; }
        //сообщение от бота битрикс
        public string FIRST_OUT_MSG { get; set; }
        //текст для клиентов в выходные дни
        public string WEEKEND_TEXT { get; set; }
        //інструкції поведінки для моделі
        public string INSTRUCTS_FOR_GPT { get; set; }
        //Bitrix24 webhook
        public string BITRIX_HOOK { get; set; }
        //источник откуда идут сообщения в битриксе
        public string BITRIX_SOURCE_NAME { get; set; }
        // назви проєктів як в відкритих лініях наприклад (Веселка інстаграм - треба тільки Веселка)
        // записуємо через кому а одне й туж назву на різних мовах через |
        public string BITRIX_MY_PROJECTS_NAME { get; set; }
        //бітрікс воронки через кому 
        public string BITRIX_VORONKA { get; set; }
        // ID адміна, від кого відправляються webhooks та повідомлення менеджерам 
        public string BITRIX_ADMIN_ID { get; set; }
    }

    class GetSettingsFile
    {
        public static async Task<Settings_Prop> GetSettingsDataAsync(ILog log)
        {
            try
            {
                if (File.Exists(Constants.ConstantFolders.SETTINGS_PATH))
                {
                    string strJson = await File.ReadAllTextAsync(Constants.ConstantFolders.SETTINGS_PATH);
                    Settings_Prop settings = JsonConvert.DeserializeObject<Settings_Prop>(strJson);
                    return settings;
                }
                await log.LogDelegate(typeof(GetSettingsFile), "Не знайдено папки чи файлу Settings", Enums.LogLevels.Error);
            }
            catch (System.Exception ex)
            { 
                await log.LogDelegate(typeof(GetSettingsFile), $"Error get Settings {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
            }

            return null;
        }
    }
}