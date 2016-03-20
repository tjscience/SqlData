using System;

namespace Sql.DataAttributes
{
    /// <summary>
    /// When used, all properties on the class will be ignored by default.
    /// In order to include a property, you have to use the Include attribute.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class)]
    public class IgnoreAll : System.Attribute
    {

    }
}
