using System;
using System.Data;
using System.Linq;

namespace Documentary
{
    public class SqlServerColumn
    {
        public SqlServerAdapter Adapter { get; private set; }
        public SqlServerTable Table { get; private set; }
        public string Name { get; private set; }

        public SqlServerColumn(SqlServerAdapter adapter, SqlServerTable table, string name)
        {
            Adapter = adapter;
            Table = table;
            Name = name;
        }

        public bool Exists()
        {
            return Table.Columns.Any(c => c.Name == Name);
        }

        public void Create(DbType dbType, int length)
        {
            Table.CreateColumn(Name, dbType, length);
        }
    }
}