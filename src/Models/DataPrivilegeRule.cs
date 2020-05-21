using System;
using System.Collections.Generic;
using System.Text;

namespace DataPrivilege.Models
{
    public class DataPrivilegeRule
    {
        public string Id { get; set; }
        public string TableName { get; set; }
        public string ConditionExpression { get; set; }
        public string Description { get; set; }

    }
}
