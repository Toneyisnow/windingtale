using System.Collections.Generic;
using Newtonsoft.Json;
using WindingTale.Core.Files;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// 
    /// </summary>
    public class StringsDefinition
    {
        [JsonProperty(PropertyName = "data")]
        public StringDefinition[] Data
        {
            get; set;
        }

        public string GetString(string key)
        {
            foreach (StringDefinition def in Data)
            {
                if (def.Key.Equals(key))
                {
                    return def.Value;
                }
            }
            return null;
        }
    }

    public class StringDefinition
    {

        [JsonProperty(PropertyName = "key")]
        public string Key
        {
            get; set;
        }

        [JsonProperty(PropertyName = "value")]
        public string Value
        {
            get; set;
        }
    }
}