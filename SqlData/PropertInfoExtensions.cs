using Sql.DataAttributes;
using System.Linq;
using System.Reflection;

namespace Sql
{
    public static class PropertInfoExtensions
    {
        public static bool Is<T>(this PropertyInfo pInfo)
        {
            if (pInfo.GetCustomAttributes(false).Count(x => x is T) > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
