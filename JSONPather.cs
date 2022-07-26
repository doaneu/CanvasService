using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CanvasService
{
    //Toot toot here comes the JSONPather
    public static class JSONPather
    {
        public static string ResolveTemplate(string pathTemplate, JObject jsonObject)
        {      
            //strip out all JSONPath items ({ }) from the string, run the JSONPath, place result in a list of key value pairs
            List<KeyValuePair<string, string>> pathTemplates = new List<KeyValuePair<string, string>>();
            string[] subs = pathTemplate.Split('{', StringSplitOptions.RemoveEmptyEntries);

            foreach (var sub in subs)
            {
                string jsonPath = sub.Substring(0, sub.IndexOf('}'));
                string jsonValue = (string)jsonObject.SelectToken(jsonPath);

                pathTemplates.Add(new KeyValuePair<string, string>("{" + jsonPath + "}", jsonValue));
            }

            //We allegedly now have values for everything. Check and if one of the key value pair values is null or blank return a blank string. 
            foreach (KeyValuePair<string, string> kvPair in pathTemplates)
            {
                if (string.IsNullOrEmpty(kvPair.Value))
                {
                    return string.Empty;
                }
            }

            //Now that we are sure there is a value for every templated item resolve the string
            foreach (KeyValuePair<string, string> kvPair in pathTemplates)
            {
                pathTemplate=pathTemplate.Replace(kvPair.Key, kvPair.Value);
            }

            //Return the string with values populated
            return pathTemplate;
        }

        public static string GetValue(string pathTemplate, JObject jsonObject)
        {
            return (string)jsonObject.SelectToken(pathTemplate);
        }

    }
}
