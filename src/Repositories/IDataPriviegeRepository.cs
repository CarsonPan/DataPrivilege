using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataPrivilege.Models
{
    public interface IDataPriviegeRepository
    {
        IEnumerable<DataPrivilegeRuleRole> GetDataPriviegeRuleRoleByRoleId(string tableName, string roleId);
        IEnumerable<DataPrivilegeRuleUser> GetDataPriviegeRuleUserByUserId(string tableName, string userId);

        IEnumerable<DataPrivilegeRuleRole> GetDataPriviegeRuleRoleByRoleId(string tableName, IEnumerable<string> roleIds);
    }
}
