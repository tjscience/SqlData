using Sql.DataAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql.TestEntities
{
    [Connection("TestDB")]
    public class Person : Entity<Person>
    {
        [Key]
        public int PersonId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        [Ignore]
        public bool IsActive { get; set; }

        public override string ToString()
        {
            return string.Format("( PersonId: {0}, FirstName: {1}, LastName: {2}, Age: {3} )", 
                PersonId, FirstName, LastName, Age);
        }
    }
}
