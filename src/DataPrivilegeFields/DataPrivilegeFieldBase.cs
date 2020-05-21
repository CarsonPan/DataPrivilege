using DataPrivilege.DataPrivilegeFields.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DataPrivilege.DataPrivilegeFields
{
    public abstract class DataPrivilegeFieldBase<TResult> : IDataPrivilegeField
    {
        public string FieldName => this.GetType().GetCustomAttribute<DataPrivilegeFieldAttribute>()?.FieldName;
        public Type FieldType => typeof(TResult);

        protected abstract TResult GetValueCore();

        public object GetValue()
        {
            return GetValueCore();
        }
    }
}
