using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Common
{
    public class ResourceJsonFile
    {
        public static T Load<T>(string resourcePath)
        {
            TextAsset text = Resources.Load<TextAsset>(resourcePath);
            if (text == null)
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(text.text);
        }
    }
}