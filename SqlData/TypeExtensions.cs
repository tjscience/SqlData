using Sql.DataAttributes;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Sql
{
    public static class TypeExtensions
    {
        public static bool Is<T>(this Type pInfo)
        {
            if (pInfo.GetCustomAttributes(false).Count(x => x is T) > 0)
                return true;
            else
                return false;
        }

        public static Dictionary<string, int> GetOrdinalValuesFromDataReader(this Type type, SqlDataReader reader)
        {
            var dict = new Dictionary<string, int>();
            var isIgnoreAll = type.Is<IgnoreAll>();

            foreach (var pInfo in type.GetProperties())
            {
                // do not populate ignored properties
                if (pInfo.Is<Ignore>())
                    continue;

                if (isIgnoreAll && !pInfo.Is<Include>() && !pInfo.Is<Key>())
                    continue;

                try
                {
                    var ordinal = reader.GetOrdinal(pInfo.Name);
                    dict.Add(pInfo.Name, ordinal);
                }
                catch
                {
                    continue;
                }
            }

            return dict;
        }
    }
}
