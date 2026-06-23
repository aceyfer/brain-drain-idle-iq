using UnityEngine;
using Plankton.Proxy;
using System.Collections.Generic;

namespace Plankton
{
    public class Utils
    {
        private const string logName = "[Utils]";

        private static readonly ProxyUtils proxyUtils = new ProxyUtils();

        public static void ShowRateUs(System.Action<bool> callback)
        {
            Debug.Log($"{logName} ShowRateus");
            try
            {
                PlanktonMono.onRateAppResult = callback;

                proxyUtils.ShowRateus();

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(() => callback?.Invoke(true));
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void ShareText(string text)
        {
            Debug.Log($"{logName} ShareText '{text}'");
            try
            {
                proxyUtils.ShareText(text);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }


        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        internal static class Jsoner
        {
            private static readonly List<string> builder = new List<string>(64);

            public static void AddParams(params object[] args)
            {
                if (args == null) return;

                if (args.Length % 2 != 0)
                {
                    Debug.LogError("Params must contain pair of Key-Value!");
                    return;
                }

                for (int i = 0; i < args.Length - 1; i += 2)
                {
                    if (args[i + 1] is string s)
                        Add((string)args[i], s);
                    else if (args[i + 1] is float f)
                        Add((string)args[i], f);
                    else if (args[i + 1] is double d)
                        Add((string)args[i], d);
                    else if (args[i + 1] is int t)
                        Add((string)args[i], t);
                    else if (args[i + 1] is long l)
                        Add((string)args[i], l);
                    else if (args[i + 1] is bool b)
                        Add((string)args[i], b);
                }
            }

            public static void Add(string key, string value)
            {
                if (value == "[]")
                    builder.Add($"\"{key}\":[]");
                else
                    builder.Add($"\"{key}\":\"{value}\"");
            }

            public static void Add(string key, float value)
            {
                builder.Add($"\"{key}\":{value}");
            }

            public static void Add(string key, double value)
            {
                builder.Add($"\"{key}\":{value}");
            }

            public static void Add(string key, int value)
            {
                builder.Add($"\"{key}\":{value}");
            }

            public static void Add(string key, long value)
            {
                builder.Add($"\"{key}\":{value}");
            }

            public static void Add(string key, bool value)
            {
                builder.Add($"\"{key}\":{value.ToString().ToLower()}");
            }

            public static void AddJson(string key, string value)
            {
                builder.Add($"\"{key}\":{value}");
            }

            public static void Add(Dictionary<string, string> dictionary)
            {
                foreach (var item in dictionary)
                    Add(item.Key, item.Value);
            }

            public static string GetJsonAndClear()
            {
                var result = builder.Count > 0 ? "{" + string.Join(",", builder) + "}" : "{}";
                builder.Clear();
                return result;
            }

            public static T FromJson<T>(string json, T defaultValue)
            {
                try
                {
                    return JsonUtility.FromJson<T>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    return defaultValue;
                }
            }
        }

    }
}