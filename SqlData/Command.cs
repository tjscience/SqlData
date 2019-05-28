using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sql
{
    public class Command
    {
        public string Query { get; set; }
        public string Connection { get; set; }
        private int timeout = 30;
        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; } 
        }
        private List<Parameter> parameters = new List<Parameter>();
        public List<Parameter> Parameters
        {
            get { return parameters; }
            set { parameters = value; }
        }
        // This is only available in SQL Server 2016+!
        public Dictionary<string, object> SessionContext { get; } = new Dictionary<string, object>();
        private CommandStyle style = CommandStyle.Query;
        public CommandStyle Style
        {
            get { return style; }
            set { style = value; }
        }
        // This is only used for QueryMultiple
        private List<SqlResult> tables = new List<SqlResult>();
        public List<SqlResult> Tables
        {
            get { return tables; }
            set { tables = value; }
        }

        public static List<Parameter> AddParameters(params Parameter[] parameters)
        {
            return new List<Parameter>(parameters);
        }

        public static List<SqlResult> AddTables(params SqlResult[] tables)
        {
            return new List<SqlResult>(tables);
        }

        public enum CommandStyle
        {
            Query,
            StoredProcedure
        }
    }
}
