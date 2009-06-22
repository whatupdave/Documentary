using System;
using System.Collections.Generic;
using System.Data;

namespace Documentary
{
    public class SchemaTypeMap 
    {
        public SqlServerAdapter Adapter { get; private set; }

        private readonly Dictionary<Type, SchemaTypeInfo> _typeProperties = new Dictionary<Type, SchemaTypeInfo>();

        public SchemaTypeMap(SqlServerAdapter adapter)
        {
            Adapter = adapter;
        }

        public SchemaTypeInfo this[Type type]
        {
            get
            {
                if (!_typeProperties.ContainsKey(type))
                    _typeProperties[type] = new SchemaTypeInfo(Adapter, type);
                
                return _typeProperties[type];
            }
        }
    }

    public class SchemaTypeInfo 
    {
        public SqlServerAdapter Adapter { get; private set; }
        public Type Type { get; private set; }
        public string IndexTable { get; private set; }
        public SchemaTypeProperties Properties { get; private set; }

        public SchemaTypeInfo(SqlServerAdapter adapter, Type type)
        {
            Adapter = adapter;
            Type = type;
            IndexTable = Type.Name;
            Properties = new SchemaTypeProperties(adapter, type);
        }
    }


    public class SchemaTypeProperties : List<SchemaTypeProperty>
    {
        public SqlServerAdapter Adapter { get; private set; }
        public Type Type { get; private set; }

        public SchemaTypeProperties(SqlServerAdapter adapter, Type type)
        {
            Adapter = adapter;
            Type = type;
        }


        public void Add(string propertyName)
        {
            var table = Adapter.Table(Type.Name);
            if (!table.Exists())
                table.Create(new UidColumn(), new Column(propertyName, DbType.String, 256));
            else
            {
                var column = table.Column(propertyName);
                if (!column.Exists())
                    column.Create(DbType.String, 256);
            }

            Add(new SchemaTypeProperty(Type, propertyName));
        }
    }

    public class SchemaTypeProperty 
    {
        public Type Type { get; private set; }
        public string Name { get; private set; }

        public SchemaTypeProperty(Type type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}