

using BitrixGpt.Bitrix24;
using BitrixGpt.Logs;

using System.Collections.Concurrent;


namespace BitrixGpt.ConnectOpenAI
{
    static class QueueToGpt
    {


        public static bool IsBreak;
        public static ILog Log { set; private get; }
        private static responseChatGpt _responseChatGpt_delegate;

        /// <summary>
        /// dealID, questions from client, history dialog, project name
        /// </summary>
        private static ConcurrentQueue<(int, string, string, string)> _msgQueue;

        //blocking in multithreding
        private static long _check;
        private static bool _IsWait
        {
            get => Interlocked.Read(ref _check) == 1;
            set => Interlocked.Exchange(ref _check, Convert.ToInt64(value));
        }


        static QueueToGpt()
        {
            IsBreak = false;
            _msgQueue = [];
            //запускаем проверку очереди в отдельном потоке
            _ = Task.Run(StartWhileAsync);
        }

        /// <summary>
        /// Ставим вопрос от клиента в очередь
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="userQuestion"></param>
        /// <param name="clientChatHistory"></param>
        /// <returns></returns>
        public static async Task SetToQueueAsync(int userID,
                                                 string userQuestion,
                                                 string clientChatHistory,
                                                 string projectName,
                                                 responseChatGpt responseChatGpt_delegate)
        {
            _responseChatGpt_delegate = responseChatGpt_delegate;

            try
            {
                //если уже есть вопрос от такого userId, то добавляем к нему и этот вопрос
                if (_msgQueue.Any(v => v.Item1 == userID))
                {
                    _IsWait = true;
                    ConcurrentQueue<(int, string, string, string)> tmp = [];
                    foreach (var elem in _msgQueue)
                    {
                        if (elem.Item1 == userID)
                        {
                            string str = elem.Item2 + $" {userQuestion}";
                            (int, string, string, string) newelem = new(elem.Item1, str, elem.Item3, elem.Item4);
                            tmp.Enqueue(newelem);
                        }
                        else
                            tmp.Enqueue(elem);
                    }
                    _msgQueue = new(tmp);
                    _IsWait = false;
                }
                else
                {
                    _msgQueue.Enqueue((userID, userQuestion, clientChatHistory, projectName));
                }
            }
            catch (Exception ex)
            {
                await Log.LogDelegate(typeof(QueueToGpt), $"Помилка, повідомлення не добавлено у чергу на запит до gpt \n{ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error);
                _IsWait = false;
            }
            await Task.CompletedTask;
        }

        private static async Task StartWhileAsync()
        {
            while (true)
            {
                if (IsBreak)
                {
                    _msgQueue = new ConcurrentQueue<(int, string, string, string)>();
                    IsBreak = false;
                    break;
                }
                if (!_msgQueue.IsEmpty && !_IsWait)
                {
                    (int, string, string, string) result = new();
                    if (_msgQueue.TryDequeue(out result))
                    {
                        if (result.Item1 > 0 && result.Item2 != null)
                        {
                            try
                            {
                                //запит до gpt 
                                await GptChat.StartAsync(result.Item1, result.Item2, result.Item3, result.Item4, _responseChatGpt_delegate);
                            }
                            catch (Exception ex)
                            { await Log.LogDelegate(typeof(QueueToGpt), $"Помилка відправки запроса з черги до gpt {ex.Source}\n{ex.StackTrace}", Enums.LogLevels.Error); }
                        }
                    }
                    else await Log.LogDelegate(typeof(QueueToGpt), "Помилка, не взмозі узяти запит до gpt з черги", Enums.LogLevels.Error);
                }
                //проверяем раз в 1 мин.
                await Task.Delay(60000);
            }
        }
    }
}