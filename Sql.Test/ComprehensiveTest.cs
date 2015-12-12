using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Sql.TestEntities;
using System.Linq;

namespace Sql.Test
{
    [TestClass]
    public class ComprehensiveTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var dir = Util.DirectoryHelper.GetDataDirectory();
            string connStr = string.Format("Data Source=(LocalDB)\\v11.0;AttachDbFilename=\"{0}\\TestDB.mdf\";Integrated Security=True", dir);
            Sql.Data.Store.AddConnection("TestDB", connStr);
            Sql.Data.Store.GenerateQueryText = true;
        }

    }
}
