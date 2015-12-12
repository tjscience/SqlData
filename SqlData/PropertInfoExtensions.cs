using Sql.DataAttributes;
using System.Linq;
using System.Reflection;

namespace Sql
{
    public static class PropertInfoExtensions
    {
        public static bool IsKey(this PropertyInfo pInfo)
        {
            if (pInfo.GetCustomAttributes(false).Count(x => x is Key) > 0)
                return true;
            else
                return false;
        }

        public static bool IsIgnore(this PropertyInfo pInfo)
        {
            if (pInfo.GetCustomAttributes(false).Count(x => x is Ignore) > 0)
                return true;
            else
                return false;
        }

        public static bool IsReadOnly(this PropertyInfo pInfo)
        {
            if (pInfo.GetCustomAttributes(false).Count(x => x is ReadOnly) > 0)
                return true;
            else
                return false;
        }
    }
}
