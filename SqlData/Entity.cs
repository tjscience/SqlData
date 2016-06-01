
using Sql.DataAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sql
{
    public abstract class Entity<T>
    {
        public virtual ValidationResult Validate(DataOperation operation)
        {
            return new ValidationResult();
        }

        public void Sync(T newer)
        {
            var entityType = this.GetType();

            foreach (var pInfo in entityType.GetProperties())
            {
                if (pInfo.GetSetMethod() != null)
                {
                    pInfo.SetValue(this, pInfo.GetValue(newer, null), null);
                }
            }
        }

        /// <summary>
        /// Creates the entity in the data store.
        /// </summary>
        /// <returns>true if success, otherwise false</returns>
        public virtual ValidationResult Create()
        {
            // perform validation
            var validationResult = this.Validate(DataOperation.Insert);

            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            var entityType = this.GetType();
            var tableName = this.EntityName;
            var connection = Data.Store.GetConnectionNameForType(entityType);
            var sql = "INSERT INTO [{Table}] ({Columns}) VALUES ({Values}); SELECT TOP 1 {PrimaryKey},{Columns} FROM [{Table}] WHERE [{PrimaryKey}] = SCOPE_IDENTITY();";
            var primaryKey = string.Empty;
            var columns = new StringBuilder();
            var values = new StringBuilder();
            var parameters = new List<Parameter>();

            var first = true;

            var isIgnoreAll = entityType.Is<IgnoreAll>();

            foreach (var pInfo in entityType.GetProperties())
            {
                if (pInfo.Is<Key>())
                {
                    primaryKey = pInfo.Name;
                    continue;
                }

                if (pInfo.Is<Ignore>() || pInfo.Is<ReadOnly>())
                    continue;

                if (isIgnoreAll && !pInfo.Is<Include>() && !pInfo.Is<Key>())
                    continue;

                if (!first)
                {
                    columns.Append(",");
                    values.Append(",");
                }

                var name = pInfo.Name;
                var value = pInfo.GetValue(this, null);
                columns.Append(string.Format("[{0}]", name));
                values.Append(string.Format("@{0}", name));
                parameters.Add(Parameter.Create(name, value ?? DBNull.Value));
                first = false;
            }

            sql = sql.ReplaceDictionary(new Dictionary<string, string>()
            {
                { "{Table}", tableName },
                { "{Columns}", columns.ToString() },
                { "{Values}", values.ToString() },
                { "{PrimaryKey}", primaryKey }
            });

            var result = Data.Store.Scalar<T>(new Command
            {
                Connection = connection,
                Parameters = parameters,
                Query = sql,
                Style = Command.CommandStyle.Query
            });
            
            // update this
            this.Sync(result);

            return validationResult;
        }

        /// <summary>
        /// Updates the entity in the data store.
        /// </summary>
        /// <returns>true if success, otherwise false</returns>
        public virtual ValidationResult Update()
        {
            // perform validation
            var validationResult = this.Validate(DataOperation.Update);

            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            var entityType = this.GetType();
            var tableName = this.EntityName;
            var connection = Data.Store.GetConnectionNameForType(entityType);
            var sql = "UPDATE [{Table}] SET {Columns} WHERE [{PrimaryKey}] = @Id; SELECT TOP 1 {PrimaryKey},{Columns} FROM [{Table}] WHERE [{PrimaryKey}] = @Id;";
            var primaryKey = string.Empty;
            var columns = new StringBuilder();
            var parameters = new List<Parameter>();

            var first = true;

            var isIgnoreAll = entityType.Is<IgnoreAll>();

            foreach (var pInfo in entityType.GetProperties())
            {
                // do not insert key
                if (pInfo.Is<Key>())
                {
                    primaryKey = pInfo.Name;
                    parameters.Add(Parameter.Create("Id", pInfo.GetValue(this, null)));
                    continue;
                }

                if (pInfo.Is<Ignore>() || pInfo.Is<ReadOnly>())
                    continue;

                if (isIgnoreAll && !pInfo.Is<Include>() && !pInfo.Is<Key>())
                    continue;

                if (!first)
                {
                    columns.Append(",");
                }

                var name = pInfo.Name;
                var value = pInfo.GetValue(this, null);
                columns.Append(string.Format("[{0}] = @{0}", name));
                parameters.Add(Parameter.Create(name, value ?? DBNull.Value));
                first = false;
            }

            sql = sql.ReplaceDictionary(new Dictionary<string, string>()
            {
                { "{Table}", tableName },
                { "{Columns}", columns.ToString() },
                { "{PrimaryKey}", primaryKey }
            });

            var result = Data.Store.Scalar<T>(new Command
            {
                Connection = connection,
                Parameters = parameters,
                Query = sql,
                Style = Command.CommandStyle.Query
            });

            // update this
            this.Sync(result);

            return validationResult;
        }

        /// <summary>
        /// Deletes the entity from the data store.
        /// </summary>
        /// <returns>true if success, otherwise false</returns>
        public virtual ValidationResult Delete()
        {
            // perform validation
            var validationResult = this.Validate(DataOperation.Delete);

            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            var entityType = this.GetType();
            var tableName = this.EntityName;
            var connection = Data.Store.GetConnectionNameForType(entityType);
            var sql = "DELETE FROM [{Table}] WHERE [{PrimaryKey}] = @Id;";
            var primaryKey = string.Empty;
            var parameters = new List<Parameter>();

            foreach (var pInfo in entityType.GetProperties())
            {
                if (pInfo.Is<Key>())
                {
                    // we found the key which is all we need, now break 
                    primaryKey = pInfo.Name;
                    parameters.Add(Parameter.Create("Id", pInfo.GetValue(this, null)));
                    break;
                }
            }

            sql = sql.ReplaceDictionary(new Dictionary<string, string>()
            {
                { "{Table}", tableName },
                { "{PrimaryKey}", primaryKey }
            });

            Data.Store.Query(new Command
            {
                Connection = connection,
                Parameters = parameters,
                Query = sql,
                Style = Command.CommandStyle.Query
            });

            return validationResult;
        }

        public string EntityName
        {
            get
            {
                Type type = this.GetType();
                var tName = (Name)type.GetCustomAttributes(false).SingleOrDefault(x => x is Name);
                return tName == null ? type.Name : tName.name;
            }
        }

        public string Key
        {
            get
            {
                Type type = this.GetType();

                foreach (var pInfo in type.GetProperties())
                {
                    if (pInfo.Is<Key>())
                    {
                        return pInfo.Name;
                    }
                }

                return null;
            }
        }
    }
}
