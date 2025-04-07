

using System.Text.RegularExpressions;


namespace BitrixGpt.Helpers
{
    internal class Verification
    {

        /// <summary>
        /// проверяем фразу на вхождение
        /// </summary>
        /// <param name="text"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool CheckStr(string source, string toCheck)
        {
            // Послідовно замінюємо кожен знак на порожній рядок
            // toCheck = toCheck.Replace(".", "").Replace(",", "").Replace("!", "");
            source = source.ToLower();
            toCheck = toCheck.ToLower();
            // Використовуємо регулярний вираз для пошуку фрази у тексті
            string pattern = Regex.Escape(toCheck) + @"[\s\W]*";
            // Створюємо патерн із ігноруванням регістру
            // string pattern = Regex.Escape(toCheck);
            if (Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                || source.IndexOf(toCheck, StringComparison.InvariantCultureIgnoreCase) >= 0
                || source.Contains(toCheck.ToLower(), StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public static bool VerifyText(string text)
        {
            string pattern = @"[а-я'іїє]+ [а-я'іїє]+ [а-я'іїє]+ [0-9]{1,2}\.[0-9]{1,2}\.[0-9]{4}";
            string antiPattern = @"[ыъэё]+";

            if (text != null &&
                Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase) &&
               !Regex.IsMatch(text, antiPattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}