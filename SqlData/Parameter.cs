
namespace Sql
{
    public class Parameter
    {
        private bool parse = true;
        public string Name { get; set; }
        public object Value { get; set; }
        public bool Parse
        {
            get { return parse; }
            set { parse = value; }
        }
        public System.Data.SqlDbType? Type { get; set; }
        public string TypeName { get; set; }

        public Parameter()
        {

        }

        public Parameter(string name, object value, bool parse = true)
        {
            Name = name;
            Value = value;
            Parse = parse;
        }

        public static Parameter Create(string name, object value, bool parse = true)
        {
            return new Parameter(name, value, parse);
        }
    }
}
