using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WindingTale.UI.Utils
{
    public static class GlobalVariables
    {
        private static readonly object lockObject = new object();


        private static Dictionary<string, object> variablesDictionary = new Dictionary<string, object>();

        /// <summary>
        /// The underlying key-value storage (dictionary).
        /// </summary>
        /// <value>Gets the underlying variables dictionary</value>
        public static Dictionary<string, object> VariablesDictionary => variablesDictionary;

        /// <summary>
        /// Retrieves all global variables.
        /// </summary>
        /// <returns>The global variables dictionary object.</returns>
        public static Dictionary<string, object> GetAll()
        {
            return variablesDictionary;
        }

        /// <summary>
        /// Gets a variable and casts it to the provided type argument.
        /// </summary>
        /// <typeparam name="T">The type of the variable</typeparam>
        /// <param name="key">The variable key</param>
        /// <returns>The casted variable value</returns>
        public static T Get<T>(string key)
        {
            if (variablesDictionary == null || !variablesDictionary.ContainsKey(key))
            {
                return default(T);
            }

            return (T)variablesDictionary[key];
        }

        /// <summary>
        /// Sets the variable, the existing value gets overridden.
        /// </summary>
        /// <remarks>It uses a lock under the hood to ensure consistensy between threads</remarks>
        /// <param name="key">The variable name/key</param>
        /// <param name="value">The variable value</param>
        public static void Set(string key, object value)
        {
            lock (lockObject)
            {
                if (variablesDictionary == null)
                {
                    variablesDictionary = new Dictionary<string, object>();
                }
                variablesDictionary[key] = value;
            }
        }




    }
}