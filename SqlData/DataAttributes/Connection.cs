using System;
using System.Linq;

namespace Sql.DataAttributes
{
    [System.AttributeUsage(AttributeTargets.Class)]
    public class Connection : System.Attribute
    {
        public string Value { get; set; }

        public Connection(string value)
        {
            this.Value = value;
        }

        public static Connection GetConnection(Type type)
        {
            return (Connection)type.GetCustomAttributes(false).SingleOrDefault(x => x is Connection);
        }      
    }
}
