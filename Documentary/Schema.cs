using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Documentary
{
    public class Schema
    {
        public List<Assembly> Assemblies { get; private set; }
        public SqlServerAdapter Adapter { get; private set; }
        public string DatabaseName { get; private set; }
        public SchemaTypeMap TypeInfo { get; private set; }

        public Schema(string databaseName, SqlServerAdapter adapter)
        {
            DatabaseName = databaseName;
            Adapter = adapter;
            TypeInfo = new SchemaTypeMap(
                adapter);
            Assemblies = new List<Assembly>();

            if (!Adapter.DatabaseExists(databaseName))
                Adapter.CreateDatabase(databaseName);

            if (!Adapter.TableExists("Documents"))
                Adapter.CreateTable("Documents", 
                                    new Column("uid", DbType.String, 256),
                                    new Column("document", DbType.Xml));
        }

        public ISession StartSession()
        {
            return new Session(this, Adapter.StartTransaction());
        }

        public void Define<T>(Expression<Func<T,object>> property)
        {
            var type = typeof (T);
            if (!Assemblies.Contains(type.Assembly))
                Assemblies.Add(type.Assembly);

            var memberExpression = (MemberExpression)(property.Body);
            var propertyName = memberExpression.Member.Name;
            var propertyType = memberExpression.Member.ReflectedType;
            TypeInfo[type].Properties.Add(propertyName);
        }

        public Type FindType(string name)
        {
            return (from a in Assemblies
                    from t in a.GetExportedTypes()
                    where t.Name == name
                    select t).First();
        }
    }
}