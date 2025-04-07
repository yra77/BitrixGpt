

using BitrixGpt.Enums;


namespace BitrixGpt.Logs
{
    static class PrintToScreen
    {
        
        public static void AddLine(LogLevels logLevels, string text)
        {
            switch (logLevels)
                {
                    case LogLevels.Warning:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                    case LogLevels.Important:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case LogLevels.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogLevels.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        break;
                    case LogLevels.Success:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case LogLevels.Info:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    default:
                        Console.ResetColor();
                        break;
                }

                Console.WriteLine(text);
                Console.ResetColor();
        }
    }
}