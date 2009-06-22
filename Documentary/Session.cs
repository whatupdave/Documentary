using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Documentary
{
    public static class ReflectionExtensions
    {
        public static object GetPropertyValue(this object o, string propertyName)
        {
            return o.GetType().GetProperty(propertyName).GetValue(o, null);
        }
    }

    public class Session : ISession
    {
        private readonly ITransaction _transaction;
        public Schema Schema { get; private set; }

        public Session(Schema schema, ITransaction transaction)
        {
            _transaction = transaction;
            Schema = schema;
        }

        public void Save<T>(string uid, T o)
        {
            var document = SerializeObject(o);

            _transaction.ExecuteSql(
                "insert into Documents (uid, document) values (@uid,@document)", uid, document);

            var typeInfo = Schema.TypeInfo[typeof (T)];

            var propertyNames = from p in typeInfo.Properties select p.Name;
            var propertyValues = from p in typeInfo.Properties
                                 select o.GetPropertyValue(p.Name);
                                 
            var indexInsert = string.Format("insert into {0} (uid, {1}) values('{2}', {3})", 
                typeInfo.IndexTable, 
                string.Join(",", propertyNames.ToArray()),
                uid,
                string.Join(",", propertyNames.Select(p => "@" + p).ToArray()));

            _transaction.ExecuteSql(indexInsert, propertyValues.ToArray());
        }

        public T Load<T>(string uid)
        {
            var rows = _transaction.ExecuteQuery("select document from Documents where uid = @uid", uid);
            if (rows.Count == 0 || !rows[0].ContainsKey("document"))
                throw new Exception("No object found at " + uid);

            return GetFirstDocumentFromResults<T>(rows);
        }

        public T First<T>(string clause)
        {
            var indexTable = Schema.TypeInfo[typeof (T)].IndexTable;
            var rows = _transaction.ExecuteQuery(string.Format(
                        "select top 1 document from {0} inner join Documents on {0}.uid = Documents.uid where {1}", indexTable, clause));
            if (rows.Count == 0)
                return default(T);

            return GetFirstDocumentFromResults<T>(rows);
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }

        private T GetFirstDocumentFromResults<T>(IList<SqlServerRow> rows) 
        {
            var xml = rows[0]["document"].ToString();
            var document = XElement.Parse(xml);

            return DeserializeObject<T>(document);
        }

        private T DeserializeObject<T>(XElement document) 
        {
            var type = Schema.FindType(document.Name.ToString());
            var constructor = type.GetConstructor(new Type[] { });
            var o = constructor.Invoke(new object[] { });
            foreach (var property in Schema.TypeInfo[type].Properties)
            {
                var propertyName = property.Name;
                var serializedProperty = document.Descendants().First(e => e.Name == propertyName);
                type.GetProperty(property.Name)
                    .GetSetMethod()
                    .Invoke(o,new object[]{serializedProperty.Value});
            }
            return (T) o;
        }

        private XElement SerializeObject(object o) 
        {
            var type = o.GetType();
            var root = new XElement(type.Name);
            foreach (var property in Schema.TypeInfo[type].Properties)
            {
                var value = type.GetProperty(property.Name).GetValue(o, null);
                root.Add(new XElement(property.Name, value));
            }
            return root;
        }
    }

}