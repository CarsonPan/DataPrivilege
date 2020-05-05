using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataPrivilege.DataPrivilegeFields
{
    public interface IDataPrivilegeField
    {
        Type FieldType { get; }
        object GetValue();
    }
}
