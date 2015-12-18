using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
