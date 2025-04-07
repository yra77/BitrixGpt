

using BitrixGpt.Constants;


namespace BitrixGpt.Helpers
{
    internal class CreateFolders
    {
        public static void FoldersExist()
        {
            if (!System.IO.Directory.Exists(ConstantFolders.BAN_FOLDER) || 
                !System.IO.Directory.Exists(ConstantFolders.LOGS_FOLDER) || 
                !System.IO.Directory.Exists(ConstantFolders.MSG_FOLDER)|| 
                !System.IO.Directory.Exists(ConstantFolders.ARHIV_FOLDER))
            {
                System.IO.Directory.CreateDirectory(ConstantFolders.ARHIV_FOLDER);
                System.IO.Directory.CreateDirectory(ConstantFolders.LOGS_FOLDER);
                System.IO.Directory.CreateDirectory(ConstantFolders.BAN_FOLDER);
                System.IO.Directory.CreateDirectory(ConstantFolders.MSG_FOLDER);
            }
        }
    }
}