using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Documentary
{
    public interface ITransaction : IDisposable
    {
        void ExecuteSql(string sql, params object[] args);
        List<SqlServerRow> ExecuteQuery(string sql, params object[] args);
    }

    public class SqlServerTransaction : ITransaction
    {
        public SqlTransaction Transaction { get; private set; }
        public SqlConnection Connection { get; private set; }

        public SqlServerTransaction(SqlTransaction transaction)
        {
            Transaction = transaction;
            Connection = transaction.Connection;
        }

        public void ExecuteSql(string sql, params object[] args)
        {
            var parameters = sql.GetSqlParameters(args);

            using(var cmd = Connection.CreateCommand())
            {
                cmd.Transaction = Transaction;
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        public List<SqlServerRow> ExecuteQuery(string sql, params object[] args)
        {
            var parameters = sql.GetSqlParameters(args);

            using(var cmd = Connection.CreateCommand())
            {
                cmd.Transaction = Transaction;
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteToRows();
            }
        }

        public void Dispose()
        {
            Transaction.Commit();
            Connection.Close();
        }
    }
}