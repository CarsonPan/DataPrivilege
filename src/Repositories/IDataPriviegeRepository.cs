using DataPrivilege.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataPrivilege.Repositories
{
    public interface IDataPriviegeRepository<TRule,TRuleRole,TRuleUser>
            where TRule : DataPrivilegeRule
            where TRuleRole : DataPrivilegeRuleRole<TRule>
            where TRuleUser : DataPrivilegeRuleUser<TRule>
    {
        IEnumerable<TRuleRole> GetDataPriviegeRuleRoleByRoleId(string tableName, string roleId);
        IEnumerable<TRuleUser> GetDataPriviegeRuleUserByUserId(string tableName, string userId);
        IEnumerable<TRuleRole> GetDataPriviegeRuleRoleByRoleId(string tableName, IEnumerable<string> roleIds);

       TRule Create(TRule rule);
       TRuleUser Create(TRuleUser ruleUser);
       TRuleRole Create(TRuleRole ruleRole);
       IEnumerable<TRule> Create(IEnumerable<TRule> rules);
       IEnumerable<TRuleUser> Create(IEnumerable<TRuleUser> ruleUsers);
       IEnumerable<TRuleRole> Create(IEnumerable<TRuleRole> ruleRoles);

        TRule Update(TRule rule);
        TRuleUser Update(TRuleUser ruleUser);
        TRuleRole Update(TRuleRole ruleRole);
        IEnumerable<TRule> Update(IEnumerable<TRule> rules);
        IEnumerable<TRuleUser> Update(IEnumerable<TRuleUser> ruleUsers);
        IEnumerable<TRuleRole> Update(IEnumerable<TRuleRole> ruleRoles);

        void Delete(TRule rule);
        void Delete(TRuleUser ruleUser);
        void Delete(TRuleRole ruleRole);
        void Delete(IEnumerable<TRule> rules);
        void Delete(IEnumerable<TRuleUser> ruleUsers);
        void  Delete(IEnumerable<TRuleRole> ruleRoles);

        IEnumerable<TRule> GetAll(Expression<Func<TRule, bool>> expression);
        IEnumerable<TRuleUser> GetAll(Expression<Func<TRuleUser, bool>> expression);
        IEnumerable<TRuleRole> GetAll(Expression<Func<TRuleRole, bool>> expression);
    }
}
