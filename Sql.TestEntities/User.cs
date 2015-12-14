using Sql.DataAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sql.TestEntities
{
    [Connection("testDB")]
    public class User : Entity<User>
    {
        [Key]
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
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
