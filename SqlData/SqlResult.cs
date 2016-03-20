namespace Sql
{
    public class SqlResult
    {
        public string Name { get; set; }
        private ResultType type = ResultType.Table;
        public ResultType Type
        {
            get { return type; }
            set { type = value; }
        }

        public static SqlResult Create(string name)
        {
            return new SqlResult
            {
                Name = name,
                Type = ResultType.Table
            };
        }

        public static SqlResult Create(string name, ResultType type)
        {
            return new SqlResult
            {
                Name = name,
                Type = type
            };
        }

        public enum ResultType
        {
            Scalar,
            Table
        }
    }
}
