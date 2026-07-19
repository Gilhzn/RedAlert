using System.Collections.Generic;
using System.Text;
using TiberiumDusk.Balance;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Localization service: current language table + RTL handling. Unity's
    /// IMGUI has no bidi support, so Hebrew runs are reversed for display
    /// (numbers/latin substrings keep their order).
    /// </summary>
    public static class Loc
    {
        public static string Language { get; private set; } = "en";
        public static bool IsRtl => Language == "he";

        private static Dictionary<string, string> _table = new Dictionary<string, string>();
        private static string _dataDir;

        public static void Init(string dataDir)
        {
            _dataDir = dataDir;
            SetLanguage(Language);
        }

        public static void SetLanguage(string language)
        {
            Language = language;
            _table = LocaleLoader.Load(_dataDir, language);
        }

        public static string T(string key)
        {
            if (!_table.TryGetValue(key, out var text)) return key;
            return IsRtl ? Bidi(text) : text;
        }

        /// <summary>Raw untranslated lookup (for building composite strings before Bidi).</summary>
        public static string Raw(string key) => _table.TryGetValue(key, out var text) ? text : key;

        private static bool IsHebrewChar(char c) => c >= 0x0590 && c <= 0x05FF;

        /// <summary>
        /// Minimal visual-order shaping for IMGUI: reverse the string, but keep
        /// embedded latin/digit runs in logical order.
        /// </summary>
        public static string Bidi(string text)
        {
            bool hasHebrew = false;
            foreach (var c in text)
            {
                if (IsHebrewChar(c)) { hasHebrew = true; break; }
            }
            if (!hasHebrew) return text;

            var result = new StringBuilder(text.Length);
            var ltrRun = new StringBuilder();
            for (int i = text.Length - 1; i >= 0; i--)
            {
                char c = text[i];
                bool ltr = char.IsLetterOrDigit(c) && !IsHebrewChar(c);
                if (ltr)
                {
                    ltrRun.Insert(0, c);
                }
                else
                {
                    if (ltrRun.Length > 0)
                    {
                        result.Append(ltrRun);
                        ltrRun.Clear();
                    }
                    // Mirror paired punctuation.
                    result.Append(c switch { '(' => ')', ')' => '(', '[' => ']', ']' => '[', _ => c });
                }
            }
            if (ltrRun.Length > 0) result.Append(ltrRun);
            return result.ToString();
        }
    }
}
