using System;
using System.Data;
using System.Linq;

namespace Documentary
{
    public interface ISession : IDisposable 
    {
        void Save<T>(string uid, T o);
        T Load<T>(string posts);
        T First<T>(string clause);
    }

    public class Column
    {
        public string Name { get; set; }
        public DbType DbType { get; set; }
        public int? Length { get; set; }

        public Column(string name, DbType dbType, int length)
        {
            Name = name;
            DbType = dbType;
            Length = length;
        }

        public Column(string name, DbType dbType)
        {
            Name = name;
            DbType = dbType;
        }
    }

    public class UidColumn : Column
    {
        public UidColumn() : base("uid", DbType.String, 256) {}
    }
}
