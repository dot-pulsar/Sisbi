using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Models.Entities;
using Npgsql;
using Org.BouncyCastle.Math.EC.Multiplier;

namespace Models
{
    public static class SisbiContext
    {
        private const string ConnectionString =
            "Server=localhost; Port=5432; Database=sisbi_db; User Id=postgres;";

        public static NpgsqlConnection Connection { get; } = GetPostgresConnection();

        private static NpgsqlConnection GetPostgresConnection()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        #region CRUD

        public static async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(string schema = "public")
        {
            var table = GetTableName<TEntity>();
            var sql = $"SELECT * FROM {schema}.{table}";
            Console.WriteLine(sql);
            return await Connection.QueryAsync<TEntity>(sql);
        }

        public static async Task<TEntity> GetAsync<TEntity>(Guid id, string schema = "public")
        {
            var table = GetTableName<TEntity>();
            var sql = $"SELECT * FROM {schema}.{table} WHERE id = '{id}'";

            Console.WriteLine(sql);
            return await Connection.QuerySingleOrDefaultAsync<TEntity>(sql);
        }

        public static async Task<TEntity> CreateAsync<TEntity>(object param, bool returning = false,
            string schema = "public")
        {
            var table = GetTableName<TEntity>();
            var properties = param.GetType().GetProperties();

            var sql = new StringBuilder($"INSERT INTO {schema}.{table} ");
            var names = new StringBuilder("(");
            var values = new StringBuilder("VALUES (");

            for (var i = 0; i < properties.Length; i++)
            {
                var info = properties[i];
                var name = GetPropertyName(properties[i].Name);
                var value = info.GetValue(param);

                names.Append(name);

                if (value == null)
                {
                    values.Append("NULL");
                }
                else if (info.PropertyType == typeof(string))
                {
                    values.Append((string) value == "NULL" ? "NULL" : $"'{value}'");
                }
                else if (info.PropertyType.IsEnum)
                {
                    values.Append($"'{value}'");
                }
                else if (info.PropertyType == typeof(Guid))
                {
                    values.Append($"'{value}'");
                }
                else
                {
                    values.Append(value);
                }

                var isLastIndex = i + 1 == properties.Length;
                names.Append(isLastIndex ? ')' : ", ");
                values.Append(isLastIndex ? ')' : ", ");
            }

            sql.AppendFormat("{0} {1}", names, values);

            if (returning)
            {
                sql.Append(" RETURNING *");
            }

            Console.WriteLine(sql.ToString());
            return await Connection.QuerySingleOrDefaultAsync<TEntity>(sql.ToString());
        }

        public static async Task<TEntity> UpdateAsync<TEntity>(Guid id, object param, bool returning = false,
            string schema = "public")
        {
            var table = GetTableName<TEntity>();
            var sql = new StringBuilder($"UPDATE {schema}.{table} SET ");
            UpdateProperties(param, sql);

            sql.Append($"WHERE id = '{id}' ");

            if (returning)
            {
                sql.Append("RETURNING *");
            }

            Console.WriteLine(sql.ToString());
            return await Connection.QuerySingleOrDefaultAsync<TEntity>(sql.ToString());
        }

        public static async Task DeleteAsync<TEntity>(Guid id, string schema = "public")
        {
            var table = GetTableName<TEntity>();
            var sql = $"DELETE FROM {schema}.{table} WHERE id = '{id}'";

            Console.WriteLine(sql);
            await Connection.QueryAsync(sql);
        }

        #endregion

        public static async Task<User> GetUserAsync(string login, LoginType loginType, string schema = "public")
        {
            var sql = $"SELECT * FROM {schema}.user WHERE {loginType} = '{login}'";

            Console.WriteLine(sql);
            return await Connection.QuerySingleOrDefaultAsync<User>(sql);
        }

        public static async Task<User> UpdateUserAsync(string login, LoginType loginType, object param,
            bool returning = false, string schema = "public")
        {
            var sql = new StringBuilder($"UPDATE {schema}.user SET ");
            UpdateProperties(param, sql);

            sql.Append($"WHERE {loginType} = '{login}' ");

            if (returning)
            {
                sql.Append("RETURNING *");
            }

            Console.WriteLine(sql.ToString());
            return await Connection.QuerySingleOrDefaultAsync<User>(sql.ToString());
        }

        #region Other

        public static string GetTableName<TEntity>()
        {
            var type = typeof(TEntity).Name;
            var tableName = new StringBuilder();
            for (var i = 0; i < type.Length; i++)
            {
                if (i != 0 && char.IsUpper(type[i]))
                {
                    tableName.Append('_');
                }

                tableName.Append(char.ToLower(type[i]));
            }

            return tableName.ToString();
        }

        private static string GetPropertyName(string name)
        {
            var tableName = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                if (i != 0 && char.IsUpper(name[i]))
                {
                    tableName.Append('_');
                }

                tableName.Append(char.ToLower(name[i]));
            }

            return tableName.ToString();
        }

        private static void UpdateProperties(object param, StringBuilder sql)
        {
            var properties = param.GetType().GetProperties();

            for (var i = 0; i < properties.Length; i++)
            {
                var info = properties[i];
                var infoType = info.PropertyType;

                var name = GetPropertyName(info.Name);
                var value = info.GetValue(param);

                if (value == null)
                {
                    sql.Append($"{name} = NULL");
                }
                else if (infoType == typeof(string))
                {
                    sql.Append((string) value == "NULL" ? $"{name} = {value}" : $"{name} = '{value}'");
                }
                else if (infoType.IsEnum)
                {
                    sql.Append($"{name} = '{value}'");
                }
                else if (infoType == typeof(Guid))
                {
                    sql.Append($"{name} = '{value}'");
                }
                else
                {
                    sql.Append($"{name} = {value}");
                }

                sql.Append(i + 1 == properties.Length ? " " : ", ");
            }
        }

        #endregion
    }
}