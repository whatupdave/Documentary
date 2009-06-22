using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using Documentary;

namespace Documentary
{
    public static class SqlExtensions
    {
        public static SqlParameter[] GetSqlParameters(this string sql, object[] args)
        {
            var paramaterMatches = Regex.Matches(sql, @"@[a-zA-Z0-9_]+");
            var parameters = new List<SqlParameter>();
            for (int i = 0; i < paramaterMatches.Count; i++)
            {
                var name = paramaterMatches[i].Value;
                var value = args[i].ToString();

                parameters.Add(new SqlParameter(name, value));
            }
            return parameters.ToArray();
        }

        public static List<SqlServerRow> ExecuteToRows(this SqlCommand cmd)
        {
            using (var reader = cmd.ExecuteReader())
            {
                var rows = new List<SqlServerRow>();
                while (reader.Read())
                {
                    var row = new SqlServerRow();
                    rows.Add(row);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                }
                return rows;
            }
        }
    }

    public class SqlServerAdapter
    {
        private readonly SqlConnectionStringBuilder _connectionString;
        private SqlConnectionStringBuilder _masterConnectionString;

        public SqlServerAdapter(SqlConnectionStringBuilder connectionStringBuilder)
        {
            _connectionString = connectionStringBuilder;
            _masterConnectionString = new SqlConnectionStringBuilder(connectionStringBuilder.ConnectionString)
                                      {
                                          InitialCatalog = "master"
                                      };
        }

        public void CreateDatabase(string name)
        {
            MasterSql(string.Format("create database {0}", name));
        }

        public void CreateTable(string name, params Column[] columns)
        {
            var columnSql = from c in columns
                            select ToSql(c);
            var sql = string.Format(@"create table {0} ({1})", name, string.Join(",", columnSql.ToArray()));
            DbSql(sql);
        }

        public bool DatabaseExists(string name)
        {
            short? dbId = null;
            MasterCommand(cmd =>
                          {
                              cmd.CommandText = string.Format("select dbid from sysdatabases where name = '{0}'", name);
                              dbId = cmd.ExecuteScalar() as short?;
                          });
            return dbId != null;
        }

        public bool TableExists(string name)
        {
            int? tableId = null;
            DbCommand(cmd =>
                      {
                          cmd.CommandText = string.Format("select id from sysobjects where xtype = 'u' and name = '{0}'", name);
                          var value = cmd.ExecuteScalar();
                          tableId = value as int?;
                      });
            return tableId != null;
        }

        public List<SqlServerRow> DbSql(string sql, params object[] args)
        {
            var sqlParams = sql.GetSqlParameters(args);

            List<SqlServerRow> rows = null;
            DbCommand(cmd =>
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(sqlParams);
                rows = cmd.ExecuteToRows();
            });
            return rows;
        }


        public string ToSql(Column column)
        {
            var modifiers = column.Length != null ? string.Format("({0})", column.Length) : "";
            return string.Format("[{0}] [{1}]{2}", column.Name, ToSql(column.DbType), modifiers);
        }

        public string ToSql(DbType dbType)
        {
            var map = new Dictionary<DbType, string>
                      {
                          {DbType.String, "nvarchar"}
                      };

            if (map.ContainsKey(dbType))
                return map[dbType];

            return dbType.ToString();
        }

        private void MasterSql(string sql)
        {
            MasterCommand(cmd =>
                          {
                              cmd.CommandText = sql;
                              cmd.ExecuteReader();
                          });
        }


        private void MasterCommand(Action<SqlCommand> action)
        {
            Command(action, _masterConnectionString);
        }

        public void DbCommand(Action<SqlCommand> action)
        {
            Command(action, _connectionString);
        }

        private static void Command(Action<SqlCommand> action, SqlConnectionStringBuilder connectionStringBuilder)
        {
            using (var connection = new SqlConnection(connectionStringBuilder.ToString()))
            using (var cmd = connection.CreateCommand())
            {
                connection.Open();
                action(cmd);
            }
        }

        public SqlServerTable Table(string name)
        {
            return new SqlServerTable(this, name);
        }

        public ITransaction StartTransaction()
        {
            var connection = new SqlConnection(_connectionString.ToString());
            connection.Open();
            var transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
            return new SqlServerTransaction(transaction);
        }
    }
}