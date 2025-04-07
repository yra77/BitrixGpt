

using BitrixGpt.Constants;
using BitrixGpt.Logs;


namespace BitrixGpt.Helpers
{
    class MoveFile_To_Archive
    {

        /// <summary>
        /// Якщо файл існує переносимо його у архів і видаляємо старий файл
        /// </summary>
        /// <param name="fileName">DealID + txt</param>
        /// <param name="log"></param>
        public static async Task File_MoveArchivAsync(string fileName, ILog log)
        {
            try
            {
                if (File.Exists(ConstantFolders.MSG_FOLDER + fileName + ".txt"))
                {
                    File.Move(ConstantFolders.MSG_FOLDER + fileName + ".txt",
                              ConstantFolders.ARHIV_FOLDER + fileName + ".txt",
                              true);

                    if (File.Exists(ConstantFolders.MSG_FOLDER + fileName + ".txt"))
                        await log.LogDelegate(typeof(MoveFile_To_Archive), $"Не вдалося видалити файл {ConstantFolders.MSG_FOLDER + fileName}.txt, він зайнятий іншим процесом", Enums.LogLevels.Error);
                    else
                    await log.LogDelegate(typeof(MoveFile_To_Archive), $"Файл історії User - {fileName} перемістили у архів.", Enums.LogLevels.Info);
                }
                else
                 await log.LogDelegate(typeof(MoveFile_To_Archive), $"Не вдалося знайти файл {ConstantFolders.MSG_FOLDER + fileName}.txt", Enums.LogLevels.Error);
            }
            catch (System.Exception ex)
            {
                await log.LogDelegate(typeof(MoveFile_To_Archive), $"Error move to archive: {ex.Message}", Enums.LogLevels.Error);
            }
        }
    }
}