using System;
using System.Collections.Generic;
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
