using System.Collections.Generic;
using System.Text;

namespace Sql
{
    public static class StringExtensions
    {
        public static string ReplaceDictionary(this string s, Dictionary<string, string> dictionary)
        {
            var sb = new StringBuilder(s, s.Length * 2);
            foreach (var entry in dictionary)
            {
                sb.Replace(entry.Key, entry.Value);
            }

            return sb.ToString();
        }
    }
}
