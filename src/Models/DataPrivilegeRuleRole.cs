using System;
using System.Collections.Generic;
using System.Text;

namespace DataPrivilege.Models
{
    public class DataPrivilegeRuleRole<TRule>
    {
        public string Id { get; set; }
        public string RoleId { get; set; }
        public string TableName { get; set; }
        public TRule DataPriviegeRule { get; set; }

        public DataOperation DataOperation { get; set; } = (DataOperation)15;
    }
}
