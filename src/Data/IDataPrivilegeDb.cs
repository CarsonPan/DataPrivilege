using DataPrivilege.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataPrivilege.Data
{
    public interface IDataPrivilegeDb<TRule, TRuleRole, TRuleUser>
            where TRule : DataPrivilegeRule
            where TRuleRole : DataPrivilegeRuleRole<TRule>
            where TRuleUser : DataPrivilegeRuleUser<TRule>
    {
        DbSet<TRule> DataPrivilegeRules { get;set;}
        DbSet<TRuleRole> DataPrivilegeRuleRoles { get;set;}
        DbSet<TRuleUser> DataPrivilegeRuleUsers { get;set;}
    }
}
