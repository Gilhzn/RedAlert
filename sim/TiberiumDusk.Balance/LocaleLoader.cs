using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace TiberiumDusk.Balance
{
    /// <summary>Loads data/locale/{lang}.json into a flat key→text table.</summary>
    public static class LocaleLoader
    {
        public static Dictionary<string, string> Load(string dataDir, string language)
        {
            var path = Path.Combine(dataDir, "locale", language + ".json");
            var json = JObject.Parse(File.ReadAllText(path));
            var result = new Dictionary<string, string>();
            foreach (var property in json.Properties())
            {
                result[property.Name] = property.Value.ToString();
            }
            return result;
        }
    }
}
