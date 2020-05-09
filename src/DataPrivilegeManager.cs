using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using DataPrivilege.DataPrivilegeFields;
using DataPrivilege.Models;
using DataPrivilege.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;

namespace DataPrivilege
{
    public class DataPrivilegeManager<TDbContext,TEntity, TRule, TRuleRole, TRuleUser> : IDataPrivilegeManager<TEntity>
        where TDbContext:DbContext
        where TEntity:class
        where TRule : DataPrivilegeRule
        where TRuleRole : DataPrivilegeRuleRole<TRule>
        where TRuleUser : DataPrivilegeRuleUser<TRule>
    {
        protected readonly TDbContext DbContext;
        protected readonly IDataPrivilegeFieldProvider DataPrivilegeFieldProvider;
        protected readonly IUserSessionInfo UserSessionInfo;
        protected readonly DataPrivilegeVisitor<TDbContext, TEntity> DataPrivilegeVisitor;
        protected readonly IDataPriviegeRepository<TRule, TRuleRole, TRuleUser> DataPriviegeRepository;
        public DataPrivilegeManager(IDataPriviegeRepository<TRule, TRuleRole, TRuleUser> dataPriviegeRepository,
                                    TDbContext dbContext, 
                                    IDataPrivilegeFieldProvider dataPrivilegeFieldProvider,
                                    IUserSessionInfo userSessionInfo,
                                    DataPrivilegeVisitor<TDbContext, TEntity> dataPrivilegeVisitor)
        {
            DbContext = dbContext;
            DataPrivilegeFieldProvider = dataPrivilegeFieldProvider;
            UserSessionInfo = userSessionInfo;
            DataPrivilegeVisitor = dataPrivilegeVisitor;
            DataPriviegeRepository = dataPriviegeRepository;
        }

        private IEnumerable<string> GetRoleIds()
        {
            return UserSessionInfo.RoleIds;
           // return UserAccessor.User?.FindFirst(ClaimTypes.Role)?.Value?.Split(',');
        }

        private string GetUserId()
        {
            return UserSessionInfo.UserId;
           // return UserAccessor.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        
        private List<TRule> GetDataPriviegeRules(DataOperation dataOperation)
        {
            string tableName= DbContext.Model.FindEntityType(typeof(TEntity))?.GetTableName();
            string userId = GetUserId();
            IEnumerable<TRuleUser> ruleUsers = DataPriviegeRepository.GetDataPriviegeRuleUserByUserId(tableName,userId);
            IEnumerable<string> roleIds = GetRoleIds();
            IEnumerable<TRuleRole> ruleRoles = DataPriviegeRepository.GetDataPriviegeRuleRoleByRoleId(tableName,roleIds);
            List<TRule> rules = new List<TRule>();
            IEnumerable<TRule> temp = null;
            if (ruleUsers!=null&&ruleUsers.Any())
            {
                temp = ruleUsers.Where(ru => ru.DataOperation.HasFlag(dataOperation)).Select(ru => ru.DataPriviegeRule);
                if(temp.Any())
                {
                    rules.AddRange(temp);
                }
            }
            if (ruleRoles != null&&ruleRoles.Any())
            {
                temp = ruleRoles.Where(rb => rb.DataOperation.HasFlag(dataOperation)).Select(rb => rb.DataPriviegeRule);
                if(temp.Any())
                {
                    rules.AddRange(temp);
                }
            }
            return GetEffectiveRules(rules);
        }

        /// <summary>
        /// 去重
        /// </summary>
        /// <param name="rules">表达式一样就认为为同一规则</param>
        /// <returns></returns>
        protected List<TRule> GetEffectiveRules(List<TRule> rules)
        {
            if(rules.Count==0)
            {
                return null;
            }
            if(rules.Count==1)
            {
                return rules;
            }
            List<TRule> result = new List<TRule>();
            bool  exist = false;
            foreach (TRule rule in rules)
            {
                if(string.IsNullOrWhiteSpace(rule.ConditionExpression))
                {
                    continue;
                }
                foreach(TRule _rule in result)
                {
                    if(rule.ConditionExpression.Trim().ToUpper()==_rule.ConditionExpression.Trim().ToUpper())
                    {
                        exist = true;
                        break;
                    }
                }
                if(!exist)
                {
                    result.Add(rule);
                }
                exist = false;
            }
            return result;
        }

        private DataPrivilegeContext<TDbContext, TEntity,TRule> CreateDataPrivilegeContext(DataOperation dataOperation)
        {
            var rules = GetDataPriviegeRules(dataOperation);
            if (rules == null||rules.Count==0)
            {
                return null;
            }
            return new DataPrivilegeContext<TDbContext, TEntity,TRule>(rules,DbContext,DataPrivilegeFieldProvider, DataPrivilegeVisitor);
        }
        

        public IQueryable<TEntity> Filter(IQueryable<TEntity> entities)
        {
            DataPrivilegeContext<TDbContext, TEntity,TRule> context = CreateDataPrivilegeContext(DataOperation.Read);
            if(context!=null)
            {
                return context.Filter(entities);
            }
            return entities;
        }

        public IEnumerable<TEntity> Filter(IEnumerable<TEntity> entities)
        {
            DataPrivilegeContext<TDbContext, TEntity,TRule> context = CreateDataPrivilegeContext(DataOperation.Read);
            if (context != null)
            {
                return context.Filter(entities);
            }
            return entities;
        }

        public IQueryable<TEntity> GetAll()
        {
            IQueryable<TEntity> table= DbContext.Set<TEntity>();
            DataPrivilegeContext<TDbContext, TEntity,TRule> context = CreateDataPrivilegeContext(DataOperation.Read);
            if(context!=null)
            {
               table=  context.Filter(table);
            }
            return table;
        }

        public void CheckPermission(IEnumerable<TEntity> entities, DataOperation dataOperation)
        {
            DataPrivilegeContext<TDbContext, TEntity, TRule> context = CreateDataPrivilegeContext(dataOperation);
            if (context != null)
            {
               if(!context.CheckPermission(entities))
                {
                    var data= context.GetNoAccessData(entities);
                    throw new NoAccessException(data, dataOperation);
                }
            }
        }

        public void CheckPermission(TEntity entity, DataOperation dataOperation)
        {
            DataPrivilegeContext<TDbContext, TEntity, TRule> context = CreateDataPrivilegeContext(dataOperation);
            if (context != null)
            {
                if (!context.CheckPermission(entity))
                {
                    throw new NoAccessException(entity, dataOperation);
                }
            }
        }
    }
}
