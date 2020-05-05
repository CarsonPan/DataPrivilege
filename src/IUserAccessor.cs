using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace DataPrivilege
{ 
    public interface IUserAccessor
    {
        ClaimsPrincipal User { get; }
    }
}
