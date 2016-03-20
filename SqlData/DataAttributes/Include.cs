using System;

namespace Sql.DataAttributes
{
    /// <summary>
    /// Includes the property in data related operations 
    /// (This property is only applicable if the IgnoreAll class attribute is used).
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property)]
    public class Include : System.Attribute
    {

    }
}
