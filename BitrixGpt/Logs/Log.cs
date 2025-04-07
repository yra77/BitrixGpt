

using BitrixGpt.Enums;
using Microsoft.VisualStudio.Threading;


namespace BitrixGpt.Logs
{
    /// <summary>
    /// логірованиіє помилок та сповіщень програми
    /// obj - class
    /// str - message
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="str"></param>
    public delegate Task LogDelegate(object obj, string str, LogLevels logLevels);
    /// <summary>
    /// логірованиіє питання/відповідь
    /// pass userID and msg
    /// </summary>
    /// <param name="userID"></param>
    /// <param name="msg"></param>
    public delegate Task LogMsgDelegate(string userID, string msg);


    internal class Log : ILog
    {


        public LogDelegate LogDelegate { get; set; }
        public LogMsgDelegate LogMsgDelegate { get; set; }

        /// <summary>
        /// (string of text, enum LogLevels)
        /// </summary>
        private static AsyncQueue<(string, LogLevels)> _asyncQueue;
        /// <summary>
        /// <string, string> - <userID, msgText>
        /// </summary>
        private static AsyncQueue<(string, string)> _asyncMsgQueue;
        private readonly object _lockObj = new();


        public Log()
        {
            LogDelegate = HandleOutAsync;
            LogMsgDelegate = AddToQueueAsync;
            _asyncQueue = new AsyncQueue<(string, LogLevels)>();
            _asyncMsgQueue = new AsyncQueue<(string, string)>();
            _ = LogDelegate(this, "Логування працює.", LogLevels.Info);
        }


        //логірованиіє помилок та сповіщень програми
        private async Task HandleOutAsync(object obj, string str, LogLevels logLevels)
        {
            if (obj != null)
            {
                _ = _asyncQueue.TryEnqueue((DateTime.Now.ToString() + "\n\t" +
                                           str + "\n\t" + obj.ToString(), logLevels));
            }

            await Task.Run(() =>
            {
                Start();
            });
        }

        private void Start()
        {
            do
            {
                (string, LogLevels) result = new();
                _ = _asyncQueue.TryDequeue(out result);

                if (result.Item1 != null)
                {
                    PrintToScreen.AddLine(result.Item2, result.Item1);
                    LogToFile(result.Item1);
                }
            } while (!_asyncQueue.IsEmpty);
        }

        private void LogToFile(string str)
        {
            try
            {
                using StreamWriter sw = File.AppendText(RealPath());
                sw.WriteLine(str);
            }
            catch (Exception e)
            {
                PrintToScreen.AddLine(LogLevels.Error, DateTime.Now.ToString() + "\n" + e.Message + "\n" + this);
            }
        }

        //логірованиіє питання/відповідь
        private async Task AddToQueueAsync(string userID, string msg)
        {
            _ = _asyncMsgQueue.TryEnqueue((userID, msg));

            await Task.Run(() =>
            {
                StartWhileMsg();
            });
        }

        private void StartWhileMsg()
        {
            do
            {
                (string, string) result = new();
                _ = _asyncMsgQueue.TryDequeue(out result);

                if (result.Item1 != null && result.Item2 != null)
                {
                    PrintToScreen.AddLine(LogLevels.Success, result.Item1 + ": " + result.Item2);
                    MsgToFile(result);
                }
            } while (!_asyncQueue.IsEmpty);
        }

        private void MsgToFile((string, string) obj)
        {
            try
            {
                //using StreamWriter sw = File.AppendAllLines(RealPath(obj.Item1));
                using StreamWriter sw = File.AppendText(RealPath(obj.Item1));
                sw.WriteLine(obj.Item2);
            }
            catch (Exception e)
            {
                PrintToScreen.AddLine(LogLevels.Error, DateTime.Now.ToString() + "\n" + e.Message + "\n" + this);
            }
        }


        //----------------------------------------//
        /// <summary>
        /// find file path for saving
        /// </summary>
        /// /// <param name="path"></param>
        /// <returns>path</returns>
        private string RealPath(string path = null)
        {
            // Код, который может выполняться несколькими потоками
            lock (_lockObj)
            {
                // Код, выполняемый одним потоком
                if (path == null)
                {
                    path = Constants.ConstantFolders.LOGS_FOLDER +
                                  DateTime.Now.Date.ToString().Split(' ').First() + ".txt";
                }
                else
                {
                    path = Constants.ConstantFolders.MSG_FOLDER + $"/{path}.txt";
                }

                if (!File.Exists(path))
                {
                    var a = File.Create(path);
                    a.Close();
                }

                return path;
            }
        }
    }
}