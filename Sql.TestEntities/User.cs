using Sql.DataAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sql.TestEntities
{
    [Connection("testDB")]
    [IgnoreAll]
    public class User : Entity<User>
    {
        [Key]
        public int UserId { get; set; }
        [Include]
        public string FirstName { get; set; }
        [Include]
        public string LastName { get; set; }
        [Include]
        public string LowerCaseColumn { get; set; }
        [Ignore]
        public string FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }
    }
}
