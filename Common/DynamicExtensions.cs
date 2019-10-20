using System.Collections.Generic;

namespace NCTSongApi.Common
{
    internal static class DynamicExtensions
    {
        public static T GetValue<T>(dynamic input, string key) where T : class => ((IDictionary<string, object>)input)[key] as T;
    }
}