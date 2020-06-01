using Sql.DataAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Sql
{
    public sealed class Data
    {
        private static volatile Data instance;
        private static object syncRoot = new object();
        // The available connections to data stores
        private static Dictionary<string, string> connections;
        // If the application is partial trust, we can use any code that requires
        // ReflectionPermission.
        public bool IsPartialTrust { get; set; }

        private Data()
        {
            connections = new Dictionary<string, string>();
        }

        /// <summary>
        /// The public facing instance of the singleton
        /// </summary>
        public static Data Store
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new Data();
                        }
                    }
                }

                return instance;
            }
        }

        #region Generate Query Text

        private bool generateQueryText = false;
        public bool GenerateQueryText
        {
            get { return generateQueryText; }
            set
            {
                generateQueryText = value;
                if (!generateQueryText)
                    currentSqlQuery = string.Empty;
            }
        }

        #endregion Generate Query Text

        #region Current SQL Query

        private string currentSqlQuery = string.Empty;
        public string CurrentSqlQuery
        {
            get { return currentSqlQuery; }
        }

        #endregion CurrentSQL Query

        #region Connections

        private KeyValuePair<string, string> DefaultConnection
        {
            get { return connections.First(); }
        }

        public void AddConnection(string name, string connectionString)
        {
            connections.Add(name, connectionString);
        }

        public string GetConnection(string name)
        {
            return connections[name];
        }

        public string GetConnection()
        {
            return DefaultConnection.Value;
        }

        public bool ContainsConnection(string name)
        {
            return connections.ContainsKey(name);
        }

        internal string GetConnectionForType(Type type)
        {
            // get the correct connection information
            var assignedConnection = Connection.GetConnection(type);
            return assignedConnection == null ? DefaultConnection.Value : connections[assignedConnection.Value];
        }

        internal string GetConnectionNameForType(Type type)
        {
            // get the correct connection information
            var assignedConnection = Connection.GetConnection(type);
            return assignedConnection == null ? DefaultConnection.Key : assignedConnection.Value;
        }

        #endregion Connections

        #region Query

        /// <summary>
        /// Executes a query without returning any results.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connection"></param>
        /// <param name="parameters"></param>
        public int Query(Command command)
        {
            // get the correct connection information
            var connectionStr = command.Connection == null ? DefaultConnection.Value : connections[command.Connection];

            // Handle null parameters
            if (command.Parameters == null)
            {
                command.Parameters = new List<Sql.Parameter>();
            }

            if (command.Style == Command.CommandStyle.StoredProcedure)
                command.Query = GenerateStoredProcedureQuery(command.Query, command.Parameters);

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                ExecuteSessionContext(command, conn);

                using (var sqlCommand = new SqlCommand(command.Query, conn))
                {
                    sqlCommand.CommandTimeout = command.Timeout;
                    BuildParameterList(sqlCommand, command.Parameters.ToArray());

                    if (GenerateQueryText)
                        GenerateSqlQuery(sqlCommand);

                    var result = sqlCommand.ExecuteNonQuery();

                    PopulateParameters(sqlCommand, command.Parameters.ToArray());

                    return result;
                }
            }
        }

        #endregion Query

        #region QueryToDataTable

        /// <summary>
        /// Queries a result set into a DataTable.
        /// </summary>
        /// <param name="command"></param>
        /// <returns>A DataTable filled with the query results.</returns>
        public DataTable QueryToDataTable(Command command)
        {
            // get the correct connection information
            var connectionStr = command.Connection == null ? DefaultConnection.Value : connections[command.Connection];

            // Handle null parameters
            if (command.Parameters == null)
            {
                command.Parameters = new List<Sql.Parameter>();
            }

            if (command.Style == Command.CommandStyle.StoredProcedure)
                command.Query = GenerateStoredProcedureQuery(command.Query, command.Parameters);

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                ExecuteSessionContext(command, conn);

                using (var sqlCommand = new SqlCommand(command.Query, conn))
                {
                    sqlCommand.CommandTimeout = command.Timeout;
                    BuildParameterList(sqlCommand, command.Parameters.ToArray());

                    if (GenerateQueryText)
                        GenerateSqlQuery(sqlCommand);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(sqlCommand))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        PopulateParameters(sqlCommand, command.Parameters.ToArray());
                        return dt;
                    }
                }
            }
        }

        #endregion QueryToDataTable

        #region Query<T>

        /// <summary>
        /// Executes a query and returns an IEnumerable of results.
        /// </summary>
        /// <typeparam name="T">The entity or type of results to return</typeparam>
        /// <param name="connection">The named connction</param>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal IEnumerable<T> QueryInternal<T>(Command command)
        {
            var type = typeof(T);

            if (command.Connection == null)
            {
                command.Connection = GetConnectionNameForType(type);
            }

            string connectionStr = command.Connection;
            if (connections.ContainsKey(command.Connection))
            {
                // get the correct connection information
                connectionStr = connections[command.Connection];
            }

            if (command.Style == Command.CommandStyle.StoredProcedure)
                command.Query = GenerateStoredProcedureQuery(command.Query, command.Parameters);

            if (type.IsValueType)
            {
                #region T is a Value Type
                // The type is a simple value type or string. We do not need the complext activator.
                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();
                    ExecuteSessionContext(command, conn);

                    using (var sqlCommand = new SqlCommand(command.Query, conn))
                    {
                        sqlCommand.CommandTimeout = command.Timeout;
                        BuildParameterList(sqlCommand, command.Parameters.ToArray());

                        if (GenerateQueryText)
                            GenerateSqlQuery(sqlCommand);

                        using (var reader = sqlCommand.ExecuteReader())
                        {
                            PopulateParameters(sqlCommand, command.Parameters.ToArray());

                            while (reader.Read())
                            {
                                var instance = (T)System.Runtime.Serialization.FormatterServices
                                    .GetUninitializedObject(type);
                                var item = reader[0];
                                instance = Convert.IsDBNull(item) ? default(T) : (T)item;
                                yield return instance;
                            }
                        }
                    }
                }
                #endregion T is a Value Type
            }
            else if (type == typeof(string))
            {
                #region T is a String
                // The type is a simple value type or string. We do not need the complext activator.
                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();
                    ExecuteSessionContext(command, conn);

                    using (var sqlCommand = new SqlCommand(command.Query, conn))
                    {
                        sqlCommand.CommandTimeout = command.Timeout;
                        BuildParameterList(sqlCommand, command.Parameters.ToArray());

                        if (GenerateQueryText)
                            GenerateSqlQuery(sqlCommand);

                        using (var reader = sqlCommand.ExecuteReader())
                        {
                            TypeConverter converter = TypeDescriptor.GetConverter(type);

                            PopulateParameters(sqlCommand, command.Parameters.ToArray());

                            while (reader.Read())
                            {
                                var instance = string.Empty;
                                var item = reader[0];
                                instance = Convert.IsDBNull(item) ? string.Empty : item.ToString();
                                yield return (T)converter.ConvertFromString(null, CultureInfo.InvariantCulture, instance);
                            }
                        }
                    }
                }
                #endregion T is a String
            }
            else
            {
                #region T is a Complex Type
                // get activator
                var activator = ObjectGenerator<T>();

                var isIgnoreAll = type.Is<IgnoreAll>();

                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();
                    ExecuteSessionContext(command, conn);

                    using (var sqlCommand = new SqlCommand(command.Query, conn))
                    {
                        sqlCommand.CommandTimeout = command.Timeout;
                        BuildParameterList(sqlCommand, command.Parameters.ToArray());

                        if (GenerateQueryText)
                            GenerateSqlQuery(sqlCommand);

                        using (var reader = sqlCommand.ExecuteReader())
                        {
                            var map = GetPropertyMap<T>(reader, isIgnoreAll);

                            PopulateParameters(sqlCommand, command.Parameters.ToArray());

                            while (reader.Read())
                            {
                                var instance = activator();

                                foreach (var property in map)
                                {
                                    var item = reader[property.Value];
                                    var realType = Nullable.GetUnderlyingType(property.Key.PropertyType) ?? property.Key.PropertyType;
                                    var value = Convert.IsDBNull(item) ? null : Convert.ChangeType(item, realType);
                                    DataCache.Cache.GetSetter<T>(property.Key)(instance, value);
                                }

                                yield return instance;
                            }
                        }
                    }
                }
                #endregion T is a Complex Type
            }
        }

        internal IEnumerable<T> QueryInternalWithPartialTrust<T>(Command command)
        {
            var type = typeof(T);

            if (command.Connection == null)
            {
                command.Connection = GetConnectionNameForType(type);
            }

            string connectionStr = command.Connection;
            if (connections.ContainsKey(command.Connection))
            {
                // get the correct connection information
                connectionStr = connections[command.Connection];
            }

            if (command.Style == Command.CommandStyle.StoredProcedure)
                command.Query = GenerateStoredProcedureQuery(command.Query, command.Parameters);

            if (type.IsValueType || type == typeof(string))
            {
                #region T is a Value Type or String
                // The type is a simple value type or string. We do not need the complext activator.
                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();
                    ExecuteSessionContext(command, conn);

                    using (var sqlCommand = new SqlCommand(command.Query, conn))
                    {
                        sqlCommand.CommandTimeout = command.Timeout;
                        BuildParameterList(sqlCommand, command.Parameters.ToArray());

                        if (GenerateQueryText)
                            GenerateSqlQuery(sqlCommand);

                        using (var reader = sqlCommand.ExecuteReader())
                        {
                            var map = GetPropertyMap<T>(reader, false);

                            PopulateParameters(sqlCommand, command.Parameters.ToArray());

                            while (reader.Read())
                            {
                                var instance = Activator.CreateInstance<T>();
                                var item = reader[0];
                                instance = Convert.IsDBNull(item) ? default(T) : (T)item;
                                yield return instance;
                            }
                        }
                    }
                }
                #endregion T is a Value Type or String
            }
            else
            {
                #region T is a Complex Type
                var isIgnoreAll = type.Is<IgnoreAll>();

                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();
                    ExecuteSessionContext(command, conn);

                    using (var sqlCommand = new SqlCommand(command.Query, conn))
                    {
                        sqlCommand.CommandTimeout = command.Timeout;
                        BuildParameterList(sqlCommand, command.Parameters.ToArray());

                        if (GenerateQueryText)
                            GenerateSqlQuery(sqlCommand);

                        using (var reader = sqlCommand.ExecuteReader())
                        {
                            var map = GetPropertyMap<T>(reader, isIgnoreAll);

                            PopulateParameters(sqlCommand, command.Parameters.ToArray());

                            while (reader.Read())
                            {
                                var instance = Activator.CreateInstance<T>();

                                foreach (var property in map)
                                {
                                    var item = reader[property.Value];
                                    var value = Convert.IsDBNull(item) ? null : item;
                                    property.Key.SetValue(instance, value, null);
                                }

                                yield return instance;
                            }
                        }
                    }
                }
                #endregion T is a Complex Type
            }
        }

        /// <summary>
        /// Executes a query and returns an IEnumerable of results.
        /// </summary>
        /// <typeparam name="T">The entity or type of results to return</typeparam>
        /// <param name="connection">The named connction</param>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(Command command)
        {
            // Handle null parameters
            if (command.Parameters == null)
            {
                command.Parameters = new List<Sql.Parameter>();
            }

            if (IsPartialTrust)
                return QueryInternalWithPartialTrust<T>(command);
            else
                return QueryInternal<T>(command);
        }

        #endregion Query<T>

        #region QueryMultiple<T>

        public dynamic QueryMultiple(Command command)
        {
            if (command.Connection == null)
            {
                command.Connection = DefaultConnection.Key;
            }

            string connectionStr = command.Connection;
            if (connections.ContainsKey(command.Connection))
            {
                // get the correct connection information
                connectionStr = connections[command.Connection];
            }

            // Handle null parameters
            if (command.Parameters == null)
            {
                command.Parameters = new List<Sql.Parameter>();
            }

            if (command.Style == Command.CommandStyle.StoredProcedure)
                command.Query = GenerateStoredProcedureQuery(command.Query, command.Parameters);

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                ExecuteSessionContext(command, conn);

                using (var sqlCommand = new SqlCommand(command.Query, conn))
                {
                    sqlCommand.CommandTimeout = command.Timeout;
                    BuildParameterList(sqlCommand, command.Parameters.ToArray());

                    if (GenerateQueryText)
                        GenerateSqlQuery(sqlCommand);

                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        var result = (IDictionary<string, object>)new ExpandoObject();
                        var schemas = new List<dynamic>();

                        PopulateParameters(sqlCommand, command.Parameters.ToArray());

                        int count = 1;

                        do
                        {
                            var results = new List<dynamic>();
                            string tableName = "Table" + count;
                            SqlResult tableResult = null;

                            if (command.Tables != null && command.Tables.Count >= count)
                            {
                                tableResult = command.Tables[count - 1];
                                tableName = tableResult.Name;
                            }

                            if (tableResult == null || tableResult.Type == SqlResult.ResultType.Table)
                            {
                                while (reader.Read())
                                {
                                    dynamic item = new ExpandoObject();

                                    for (var a = 0; a < reader.FieldCount; a++)
                                    {
                                        var name = reader.GetName(a);
                                        var value = reader[a];

                                        ((IDictionary<string, object>)item)[name] = Convert.IsDBNull(value) ? null : value;
                                    }

                                    results.Add(item);
                                }

                                result[tableName] = results;
                            }
                            else if (tableResult.Type == SqlResult.ResultType.Scalar)
                            {
                                if (reader.Read())
                                {
                                    dynamic item = new ExpandoObject();

                                    for (var a = 0; a < reader.FieldCount; a++)
                                    {
                                        var name = reader.GetName(a);
                                        var value = reader[a];

                                        ((IDictionary<string, object>)item)[name] = Convert.IsDBNull(value) ? null : value;
                                    }

                                    result[tableName] = item;
                                }
                            }

                            count++;
                        } while (reader.NextResult());

                        return result;
                    }
                }
            }
        }

        #endregion QueryMultiple<T>

        #region QueryDynamic

        /// <summary>
        /// Returns dynamic results of a SQL query
        /// </summary>
        /// <param name="query">The query to be run against the database</param>
        /// <param name="connection">The connection to run the query through</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>A collection of dynamic objects</returns>
        public IEnumerable<dynamic> QueryDynamic(Command command)
        {
            if (command.Connection == null)
            {
                command.Connection = DefaultConnection.Key;
            }

            string connectionStr = command.Connection;
            if (connections.ContainsKey(command.Connection))
            {
                // get the correct connection information
                connectionStr = connections[command.Connection];
            }

            // Handle null parameters
            if (command.Parameters == null)
            {
                command.Parameters = new List<Sql.Parameter>();
            }

            if (command.Style == Command.CommandStyle.StoredProcedure)
                command.Query = GenerateStoredProcedureQuery(command.Query, command.Parameters);

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                ExecuteSessionContext(command, conn);

                using (var sqlCommand = new SqlCommand(command.Query, conn))
                {
                    sqlCommand.CommandTimeout = command.Timeout;
                    BuildParameterList(sqlCommand, command.Parameters.ToArray());

                    if (GenerateQueryText)
                        GenerateSqlQuery(sqlCommand);

                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        PopulateParameters(sqlCommand, command.Parameters.ToArray());

                        while (reader.Read())
                        {
                            dynamic item = new ExpandoObject();

                            for (var a = 0; a < reader.FieldCount; a++)
                            {
                                var name = reader.GetName(a);
                                var value = reader[a];

                                ((IDictionary<string, object>)item)[name] = Convert.IsDBNull(value) ? null : value;
                            }

                            yield return item;
                        }
                    }
                }
            }
        }

        #endregion QueryDynamic

        #region Scalar

        internal T ScalarInternal<T>(Command command)
        {
            var type = typeof(T);
            T result = default(T);

            if (command.Connection == null)
            {
                command.Connection = GetConnectionNameForType(type);
            }

            string connectionStr = command.Connection;
            if (connections.ContainsKey(command.Connection))
            {
                // get the correct connection information
                connectionStr = connections[command.Connection];
            }

            if (command.Style == Command.CommandStyle.StoredProcedure)
                command.Query = GenerateStoredProcedureQuery(command.Query, command.Parameters);

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                ExecuteSessionContext(command, conn);

                using (var sqlCommand = new SqlCommand(command.Query, conn))
                {
                    sqlCommand.CommandTimeout = command.Timeout;
                    BuildParameterList(sqlCommand, command.Parameters.ToArray());

                    if (GenerateQueryText)
                        GenerateSqlQuery(sqlCommand);

                    var reader = sqlCommand.ExecuteReader();

                    PopulateParameters(sqlCommand, command.Parameters.ToArray());

                    // only fetch first row
                    if (reader.Read())
                    {
                        if (type.IsValueType || type == typeof(string))
                        {
                            var item = reader[0];
                            result = Convert.IsDBNull(item) ? default(T) : (T)item;
                        }
                        else
                        {
                            var isIgnoreAll = type.Is<IgnoreAll>();
                            result = (T)Activator.CreateInstance(type, true);
                            var ordinalDictionary = type.GetOrdinalValuesFromDataReader(reader);

                            // read the data from the row into each property in the type
                            foreach (var pInfo in type.GetProperties())
                            {
                                int ordinal = 0;

                                if (ordinalDictionary.TryGetValue(pInfo.Name, out ordinal))
                                {
                                    var item = reader.GetValue(ordinal);
                                    var realType = Nullable.GetUnderlyingType(pInfo.PropertyType) ?? pInfo.PropertyType;
                                    var value = Convert.IsDBNull(item) ? null : Convert.ChangeType(item, realType);
                                    DataCache.Cache.GetSetter<T>(pInfo)(result, value);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        internal T ScalarInternalWithPartialTrust<T>(Command command)
        {
            var type = typeof(T);
            T result = default(T);

            if (command.Connection == null)
            {
                command.Connection = GetConnectionNameForType(type);
            }

            string connectionStr = command.Connection;
            if (connections.ContainsKey(command.Connection))
            {
                // get the correct connection information
                connectionStr = connections[command.Connection];
            }

            if (command.Style == Command.CommandStyle.StoredProcedure)
                command.Query = GenerateStoredProcedureQuery(command.Query, command.Parameters);

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                ExecuteSessionContext(command, conn);

                using (var sqlCommand = new SqlCommand(command.Query, conn))
                {
                    sqlCommand.CommandTimeout = command.Timeout;
                    BuildParameterList(sqlCommand, command.Parameters.ToArray());

                    if (GenerateQueryText)
                        GenerateSqlQuery(sqlCommand);

                    var reader = sqlCommand.ExecuteReader();

                    PopulateParameters(sqlCommand, command.Parameters.ToArray());

                    // only fetch first row
                    if (reader.Read())
                    {
                        if (type.IsValueType || type == typeof(string))
                        {
                            var item = reader[0];
                            result = Convert.IsDBNull(item) ? default(T) : (T)item;
                        }
                        else
                        {
                            var isIgnoreAll = type.Is<IgnoreAll>();
                            result = (T)Activator.CreateInstance(type, true);
                            var ordinalDictionary = type.GetOrdinalValuesFromDataReader(reader);

                            // read the data from the row into each property in the type
                            foreach (var pInfo in type.GetProperties())
                            {
                                int ordinal = 0;

                                if (ordinalDictionary.TryGetValue(pInfo.Name, out ordinal))
                                {
                                    var item = reader.GetValue(ordinal);
                                    var value = Convert.IsDBNull(item) ? null : item;
                                    pInfo.SetValue(result, value, null);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the scalar result of a SQL query
        /// </summary>
        /// <typeparam name="T">The type of the scalar result</typeparam>
        /// <param name="query">The query to be run against the database</param>
        /// <param name="connection">The connection to run the query through</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>A scalar result</returns>
        public T Scalar<T>(Command command)
        {
            // Handle null parameters
            if (command.Parameters == null)
            {
                command.Parameters = new List<Sql.Parameter>();
            }

            if (IsPartialTrust)
                return ScalarInternalWithPartialTrust<T>(command);
            else
                return ScalarInternal<T>(command);
        }

        #endregion Scalar

        #region ScalarDynamic

        /// <summary>
        /// Returns single dynamic result of a SQL query
        /// </summary>
        /// <param name="query">The query to be run against the database</param>
        /// <param name="connection">The connection to run the query through</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>A dynamic object</returns>
        public dynamic ScalarDynamic(Command command)
        {
            if (command.Connection == null)
            {
                command.Connection = DefaultConnection.Key;
            }

            string connectionStr = command.Connection;
            if (connections.ContainsKey(command.Connection))
            {
                // get the correct connection information
                connectionStr = connections[command.Connection];
            }

            // Handle null parameters
            if (command.Parameters == null)
            {
                command.Parameters = new List<Sql.Parameter>();
            }

            if (command.Style == Command.CommandStyle.StoredProcedure)
                command.Query = GenerateStoredProcedureQuery(command.Query, command.Parameters);

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                ExecuteSessionContext(command, conn);

                using (var sqlCommand = new SqlCommand(command.Query, conn))
                {
                    sqlCommand.CommandTimeout = command.Timeout;
                    BuildParameterList(sqlCommand, command.Parameters.ToArray());

                    if (GenerateQueryText)
                        GenerateSqlQuery(sqlCommand);

                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        PopulateParameters(sqlCommand, command.Parameters.ToArray());

                        if (reader.Read())
                        {
                            dynamic item = new ExpandoObject();

                            for (var a = 0; a < reader.FieldCount; a++)
                            {
                                var name = reader.GetName(a);
                                var value = reader[a];

                                ((IDictionary<string, object>)item)[name] = Convert.IsDBNull(value) ? null : value;
                            }

                            return item;
                        }
                    }
                }
            }

            return null;
        }

        #endregion QueryDynamic

        public IEnumerable<T> All<T>()
        {
            var type = typeof(T);

            var tableName = string.Empty;

            var connection = Data.Store.GetConnectionNameForType(type);

            #region Get Table Name
            var tName = (Name)type.GetCustomAttributes(false).SingleOrDefault(x => x is Name);
            tableName = tName == null ? type.Name : tName.name;
            #endregion Get Table Name

            var sql = "SELECT {Columns} FROM [{Table}]";
            var columns = new StringBuilder();

            var isIgnoreAll = type.Is<IgnoreAll>();
            var first = true;

            foreach (var pInfo in type.GetProperties())
            {
                if (pInfo.Is<Ignore>())
                    continue;

                if (isIgnoreAll && !pInfo.Is<Include>() && !pInfo.Is<Key>())
                    continue;

                if (!first)
                    columns.Append(",");

                var name = pInfo.Name;
                columns.Append(string.Format("[{0}]", name));
                first = false;
            }

            sql = sql.ReplaceDictionary(new Dictionary<string, string>()
            {
                { "{Table}", tableName },
                { "{Columns}", columns.ToString() }
            });

            return Data.Store.Query<T>(new Command
            {
                Connection = connection,
                Query = sql,
                Style = Command.CommandStyle.Query
            });
        }

        private Dictionary<PropertyInfo, int> GetPropertyMap<T>(IDataReader reader, bool isClassIgnoreAll)
        {
            var type = typeof(T);

            // get the properties of T and build a map
            var map = new Dictionary<PropertyInfo, int>();
            foreach (var pInfo in type.GetProperties())
            {
                if (pInfo.Is<Ignore>())
                    continue;
                // do not populate property on a class that is marked IgnoreAll
                // unless the property is marked with include
                if (isClassIgnoreAll && !pInfo.Is<Include>() && !pInfo.Is<Key>())
                    continue;

                if (!HasColumn(reader, pInfo.Name))
                    continue;

                map.Add(pInfo, reader.GetOrdinal(pInfo.Name));
            }

            return map;
        }

        private static Func<T> ObjectGenerator<T>()
        {
            var type = typeof(T);
            var target = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, Type.EmptyTypes, null);
            var dynamic = new DynamicMethod(string.Empty, type, new Type[0], target.DeclaringType);
            var il = dynamic.GetILGenerator();
            il.DeclareLocal(target.DeclaringType);
            il.Emit(OpCodes.Newobj, target);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            return (Func<T>)dynamic.CreateDelegate(typeof(Func<T>));
        }

        private void BuildParameterList(SqlCommand command, Parameter[] parameters)
        {
            // add parameters if present
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.Parse)
                    {
                        var p = new SqlParameter(parameter.Name, parameter.Value);

                        if (parameter.Value == null)
                        {
                            p.Value = DBNull.Value;
                        }

                        if (parameter.Type != null)
                        {
                            p.SqlDbType = parameter.Type.Value;
                        }

                        if (!string.IsNullOrWhiteSpace(parameter.TypeName))
                        {
                            p.TypeName = parameter.TypeName;
                        }

                        if (parameter.Direction != null)
                        {
                            p.Direction = parameter.Direction.Value;
                        }

                        command.Parameters.Add(p);
                    }
                    else
                    {
                        // We do not want to parse the parameter, just replace it with the value.
                        command.CommandText = command.CommandText.Replace("@" + parameter.Name, (string)parameter.Value);
                    }
                }
            }
        }

        private void PopulateParameters(SqlCommand command, Parameter[] parameters)
        {
            foreach (var parameter in parameters.Where(x => x.Direction == ParameterDirection.Output))
            {
                foreach (SqlParameter p in command.Parameters)
                {
                    if (p.ParameterName == parameter.Name)
                    {
                        parameter.Value = p.Value;
                        break;
                    }
                }
            }
        }

        private void GenerateSqlQuery(SqlCommand command)
        {
            currentSqlQuery = command.CommandText;

            foreach (SqlParameter parameter in command.Parameters)
            {
                var pType = parameter.SqlDbType;

                if (parameter.Value == DBNull.Value)
                {
                    currentSqlQuery = currentSqlQuery.Replace("@" + parameter.ParameterName, "NULL");
                }
                else if (pType == SqlDbType.DateTime || pType == SqlDbType.DateTime2 || pType == SqlDbType.NChar
                    || pType == SqlDbType.NText || pType == SqlDbType.NVarChar || pType == SqlDbType.SmallDateTime
                    || pType == SqlDbType.Text || pType == SqlDbType.VarChar)
                {
                    currentSqlQuery = currentSqlQuery.Replace("@" + parameter.ParameterName, string.Format("'{0}'", parameter.Value));
                }
                else
                {
                    currentSqlQuery = currentSqlQuery.Replace("@" + parameter.ParameterName, parameter.Value.ToString());
                }
            }
        }

        private string GenerateStoredProcedureQuery(string query, List<Parameter> parameters)
        {
            var sb = new StringBuilder();
            sb.Append("exec ");
            sb.Append(query);
            sb.Append(" ");
            var strings = new List<string>();

            foreach (var parameter in parameters)
            {
                if (parameter.Direction == ParameterDirection.Output)
                {
                    strings.Add("@" + parameter.Name + " OUTPUT");
                }
                else
                {
                    strings.Add("@" + parameter.Name);
                }
            }

            sb.Append(string.Join(", ", strings));

            return sb.ToString();
        }

        public static bool HasColumn(IDataReader Reader, string ColumnName)
        {
            foreach (DataRow row in Reader.GetSchemaTable().Rows)
            {
                if (row["ColumnName"].ToString() == ColumnName)
                    return true;
            } //Still here? Column not found. 
            return false;
        }

        private void ExecuteSessionContext(Command command, SqlConnection conn)
        {
            foreach (var variable in command.SessionContext)
            {
                var query = $"exec sys.sp_set_session_context @Key, @Value;";
                using (var sqlCommand = new SqlCommand(query, conn))
                {
                    sqlCommand.Parameters.Add(new SqlParameter("Key", variable.Key));
                    sqlCommand.Parameters.Add(new SqlParameter("Value", variable.Value));
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
    }

    public static class StoreEntityExtensions
    {
        public static void CreateAll<T>(this IList<T> data)
        {
            Type type = typeof(T);
            var connectionStr = Data.Store.GetConnectionForType(type);

            #region Get Table Name
            var tName = (Name)type.GetCustomAttributes(false).SingleOrDefault(x => x is Name);
            var tableName = tName == null ? type.Name : tName.name;
            tableName = string.Format("[{0}]", tableName);
            #endregion Get Table Name

            var dt = data.ToDataTable();

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();

                using (SqlBulkCopy copy = new SqlBulkCopy(conn, SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.CheckConstraints,
                    null))
                {
                    copy.DestinationTableName = tableName;
                    copy.WriteToServer(dt);
                }
            }
        }

        private static DataTable ToDataTable<T>(this IList<T> data)
        {
            var type = typeof(T);
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();

            var isIgnoreAll = type.Is<IgnoreAll>();

            foreach (PropertyDescriptor prop in properties)
            {
                var pInfo = prop.ComponentType.GetProperty(prop.Name);

                if (pInfo.Is<Ignore>())
                    continue;

                if (pInfo.Is<ReadOnly>())
                    continue;

                if (isIgnoreAll && !pInfo.Is<Include>() && !pInfo.Is<Key>())
                    continue;

                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    var pInfo = prop.ComponentType.GetProperty(prop.Name);

                    if (pInfo.Is<Ignore>())
                        continue;

                    if (pInfo.Is<ReadOnly>())
                        continue;

                    if (isIgnoreAll && !pInfo.Is<Include>() && !pInfo.Is<Key>())
                        continue;

                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
