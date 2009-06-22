using System.Collections.Generic;

namespace Documentary
{
    public class SqlServerSelect 
    {
        public SqlServerAdapter Adapter { get; set; }
        public string Sql { get; set; }

        public SqlServerSelect(SqlServerAdapter adapter, string sql)
        {
            Adapter = adapter;
            Sql = sql;
        }

        public List<SqlServerRow> ToList()
        {
            var rows = new List<SqlServerRow>();
            Adapter.DbCommand(cmd =>
                              {
                                  cmd.CommandText = Sql;
                                  rows = cmd.ExecuteToRows();
                              });
            return rows;
        }
    }
}