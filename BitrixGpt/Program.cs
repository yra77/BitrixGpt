

using BitrixGpt.ConnectOpenAI;
using BitrixGpt.Bitrix24;
using BitrixGpt.Helpers;
using BitrixGpt.Enums;
using BitrixGpt.Logs;

using System.Text;


namespace BitrixGpt
{
    class Program
    {
         static async Task Main()
        {
            CancellationTokenSource cancellationToken = new();
            try
            {
                PrintToScreen.AddLine(LogLevels.Success, "program start");
                Console.OutputEncoding = Encoding.UTF8;

                ILog log = new Log();
                var settings = await GetSettingsFile.GetSettingsDataAsync(log);

                CreateFolders.FoldersExist();//create folders if not
                
                Ban_SetGet.Log = log;
                QueueToGpt.Log = log;
                Deal_Bitrix.Log = log;
                SendToBitrix.Log = log;
                GptChat.Log = log;

                GptChat.SETTINGS = settings;
                SendToBitrix.WebhookUrl = settings.BITRIX_HOOK;
                Deal_Bitrix.WebhookUrl = settings.BITRIX_HOOK;

                await Task.Run(async () =>
                {
                    BitrixMain bitrix = new(log, settings);
                    await bitrix.StartAsync(cancellationToken.Token);
                }, cancellationToken.Token);

                // ConsoleKeyInfo cki;
                // // Prevent example from ending if CTL+C is pressed.
                // Console.TreatControlCAsInput = true;

                // Console.WriteLine("Press the Escape (Esc) key to quit: \n");
                // do
                // {
                //     string key = "";
                //     cki = Console.ReadKey();

                //     if ((cki.Modifiers & ConsoleModifiers.Alt) != 0) key += "ALT+";
                //     // if((cki.Modifiers & ConsoleModifiers.Shift) != 0)  key="SHIFT+";
                //     if ((cki.Modifiers & ConsoleModifiers.Control) != 0) key += "CTRL+";
                //     if (cki.Key == ConsoleKey.N) key += cki.Key.ToString();

                //     if (key == "CTRL+ALT+N" || key == "ALT+CTRL+N")
                //         Console.WriteLine(key);

                // } while (cki.Key != ConsoleKey.Escape);


                Console.ReadLine();
                //останавливаем очередь запросов к gpt chat
                QueueToGpt.IsBreak = true;
                // останавливаем потоки
                await cancellationToken.CancelAsync();
            }
            catch (Exception ex)
            {
                PrintToScreen.AddLine(LogLevels.Error, $"Error - {ex.Message}\n{ex.StackTrace}");
                //останавливаем очередь запросов к gpt chat
                QueueToGpt.IsBreak = true;
                // останавливаем потоки
                await cancellationToken.CancelAsync();
            }
        }
    }
}