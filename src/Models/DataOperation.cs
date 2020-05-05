using System;
using System.Collections.Generic;
using System.Text;

namespace DataPrivilege.Models
{
    [Flags]
    public enum DataOperation
    {
        None=0,
        Create=8,
        Read=4, 
        Update=2,
        Delete=1
    }
}
