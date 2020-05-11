using DataPrivilege.DataPrivilegeFields.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DataPrivilege.DataPrivilegeFields
{
    public class DataPrivilegeFieldProvider : IDataPrivilegeFieldProvider
    {
        protected readonly IServiceProvider ServiceProvider;
        public DataPrivilegeFieldProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public bool ContainsField(string fieldName)
        {
            var container = ServiceProvider.GetCustomFields();
            return container.ContainsKey(fieldName);
        }

        public IEnumerable<DataPrivilegeFieldDescriptor> GetAllFields()
        {
            var container = ServiceProvider.GetCustomFields();
            DataPrivilegeFieldAttribute attribute;
            List<DataPrivilegeFieldDescriptor> list = new List<DataPrivilegeFieldDescriptor>(container.Count);
            foreach (string name in container.Keys)
            {
                attribute = container[name].GetCustomAttribute<DataPrivilegeFieldAttribute>();
                list.Add(new DataPrivilegeFieldDescriptor()
                {
                    FieldName = name,
                    ImplementationType = container[name],
                    Remarks = attribute.Remarks,
                    Module=attribute.Module
                });
            }
            return list;
        }

        public Type GetFieldType(string fieldName)
        {
            IDataPrivilegeField field = ServiceProvider.GetField(fieldName);
            return field.FieldType;
        }

        public object GetFieldValue(string fieldName)
        {
            IDataPrivilegeField field = ServiceProvider.GetField(fieldName);
            return field.GetValue();
        }
    }
}
