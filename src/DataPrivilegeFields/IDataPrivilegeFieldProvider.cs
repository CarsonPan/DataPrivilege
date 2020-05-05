using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataPrivilege.DataPrivilegeFields
{
    public interface IDataPrivilegeFieldProvider
    {
        Type GetFieldType(string fieldName);
        object GetFieldValue(string fieldName);
    }
}
