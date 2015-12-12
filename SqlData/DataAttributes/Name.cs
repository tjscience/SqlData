using System;

namespace Sql.DataAttributes
{
    [System.AttributeUsage(AttributeTargets.Class)]
    public class Name : System.Attribute
    {
        public string name;
        public Name(string name)
        {
            this.name = name;
        }
    }
}
