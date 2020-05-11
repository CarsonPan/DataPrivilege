using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataPrivilege.DataPrivilegeFields.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple =false,Inherited =false)]
    public class DataPrivilegeFieldAttribute:Attribute
    { 
        public string FieldName { get; set; }

        public DataPrivilegeFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }

       
        public ServiceLifetime ServiceLifetime = ServiceLifetime.Scoped;

        public string Remarks { get; set; }

        public string Module { get; set; }
    }
}
