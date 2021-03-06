using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotNetSimpleOrm.Model
{
    public abstract class Model
    {
        public bool IsExists = false;

        protected Model()
        {
            OriginAttributes = new Dictionary<string, object>();
        }

        public static Query.Query Query<T>()
        {
            return new Query.Query((Model)typeof(T).Assembly.CreateInstance(typeof(T).ToString()));
        }

        public abstract string TableName();

        public void Save ()
        {
            if (!IsExists)
            {
                ConnectorManager.GetConnector().Insert(this);
            }
            else
            {
                ConnectorManager.GetConnector().Update(this);
            }
        }

        public Dictionary<string, object> OriginAttributes { get; private set; }

        private void InitOriginAttributes()
        {
            OriginAttributes = GetAttributes();
        }

        public Dictionary<string, object> GetAttributes()
        {
            var attributes = new Dictionary<string, object>();
            foreach (var propertyInfo in GetType().GetProperties())
            {
                var attrs = propertyInfo.GetCustomAttributes(false);
                
                if (attrs.Length <= 0) continue;
                
                foreach (var attr in attrs)
                {
                    if (!(attr is ColumnAttribute column)) continue;
                    
                    var refColumnName = column.Name ?? Helper.ToSnakeCase(propertyInfo.Name);

                    attributes[refColumnName] = propertyInfo.GetValue(this);
                }
            }

            return attributes;
        }

        public string GetPrimaryColumnName ()
        {
            var primaryColumnProperty = GetPrimaryColumnProperty();

            var attrs = primaryColumnProperty.GetCustomAttributes(false);
            
            foreach (var attr in attrs)
            {
                if (!(attr is ColumnAttribute column)) continue;
                
                if (column.Primary) return column.Name ?? Helper.ToSnakeCase(primaryColumnProperty.Name);
            }
            
            throw new Exception("miss primary key on model.");
        }
        
        public PropertyInfo GetPrimaryColumnProperty ()
        {
            foreach (var propertyInfo in GetType().GetProperties())
            {
                var attrs = propertyInfo.GetCustomAttributes(false);
                
                if (attrs.Length <= 0) continue;
                
                foreach (var attr in attrs)
                {
                    if (!(attr is ColumnAttribute column)) continue;
                    
                    if (column.Primary) return propertyInfo;
                }
            }
            
            throw new Exception("miss primary key on model.");
        }

        public void AfterFillByDb ()
        {
            InitOriginAttributes();

            IsExists = true;
        }
    }
}