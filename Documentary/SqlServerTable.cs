using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Documentary
{
    public class SqlServerTable 
    {
        public SqlServerAdapter Adapter { get; private set; }
        public string Name { get; private set; }

        private List<SqlServerColumn> _columns;

        public SqlServerTable(SqlServerAdapter adapter, string name)
        {
            Adapter = adapter;
            Name = name;
        }

        public void Clear()
        {
            Adapter.DbSql(@"delete from " + Name);
        }

        public SqlServerSelect Select(string column)
        {
            return new SqlServerSelect(
                Adapter, "select " + column + " from " + Name);
        }

        public bool Exists()
        {
            var sql = string.Format("select id from sysobjects where xtype = 'u' and name = '{0}'", Name);
            var rows = Adapter.DbSql(sql);
            return rows.Count > 0;
        }

        public void Create(params Column[] columns)
        {
            Adapter.CreateTable(Name, columns);
        }

        public SqlServerColumn Column(string name)
        {
            return new SqlServerColumn(Adapter, this, name);
        }

        public List<SqlServerColumn> Columns
        {
            get
            {
                if (_columns == null)
                {
                    var rows = Adapter.DbSql(@"select COLUMN_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = @table",
                                  Name);
                    _columns = (from row in rows
                                select new SqlServerColumn(Adapter, this, row["COLUMN_NAME"].ToString())).ToList();
                }
                return _columns;
            }
        }

        public void CreateColumn(string columnName, DbType type, int length)
        {
            var column = new Column(columnName, type, length);
            var addColumnSql = string.Format(@"alter table {0} add {1}", Name, Adapter.ToSql(column));
            Adapter.DbSql(addColumnSql);
        }
    }
}