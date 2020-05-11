using System;
using System.Collections.Generic;
using System.Text;

namespace DataPrivilege.DataPrivilegeFields
{
    public class DataPrivilegeFieldDescriptor
    {
        public string FieldName { get; set; }

        public Type ImplementationType { get; set; }

        public string Remarks { get; set; }

        public string Module { get; set; }
    }
}
