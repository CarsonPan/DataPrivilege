using DataPrivilege.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DataPrivilege.Repositories
{
    public class DataPriviegeRepository<TDbContext,TRule,TRuleRole,TRuleUser> : IDataPriviegeRepository<TRule, TRuleRole, TRuleUser>
            where TDbContext:DbContext
            where TRule : DataPrivilegeRule
            where TRuleRole : DataPrivilegeRuleRole<TRule>
            where TRuleUser : DataPrivilegeRuleUser<TRule>
    {
        protected readonly TDbContext DbContext;
        public DataPriviegeRepository(TDbContext dbContext)
        {
            DbContext = dbContext;
        }
        protected T Create<T>(T entity)
            where T:class
        {
            DbContext.Set<T>().Add(entity);
            DbContext.SaveChanges();
            return entity;
        }

        protected IEnumerable<T> Create<T>(IEnumerable<T> entities)
            where T : class
        {
            DbContext.Set<T>().AddRange(entities);
            DbContext.SaveChanges();
            return entities;
        }

        protected T Update<T>(T entity)
            where T:class
        {
            DbContext.Attach<T>(entity).State = EntityState.Modified;
            DbContext.SaveChanges();
            return entity;
        }

        protected IEnumerable<T> Update<T>(IEnumerable<T> entities)
            where T : class
        {
            foreach (T entity in entities)
            {
                DbContext.Attach<T>(entity).State = EntityState.Modified;
            }
            DbContext.SaveChanges();
            return entities;
        }

        protected void Delete<T>(T entity)
            where T : class
        {
            DbContext.Attach<T>(entity).State = EntityState.Deleted;
            DbContext.SaveChanges();
        }

        protected void Delete<T>(IEnumerable<T> entities)
            where T : class
        {
            foreach (T entity in entities)
            {
                DbContext.Attach<T>(entity).State = EntityState.Deleted;
            }
            DbContext.SaveChanges();
        }

        protected IEnumerable<T> GetAll<T>(Expression<Func<T, bool>> expression)
            where T:class
        {
            return DbContext.Set<T>().Where(expression);
        }

        public TRule Create(TRule rule)
        {
            return Create<TRule>(rule);
        }

        public TRuleUser Create(TRuleUser ruleUser)
        {
            return Create<TRuleUser>(ruleUser);
        }

        public TRuleRole Create(TRuleRole ruleRole)
        {
            return Create<TRuleRole>(ruleRole);
        }

        public IEnumerable<TRule> Create(IEnumerable<TRule> rules)
        {
            return Create<TRule>(rules);
        }

        public IEnumerable<TRuleUser> Create(IEnumerable<TRuleUser> ruleUsers)
        {
            return Create<TRuleUser>(ruleUsers);
        }

        public IEnumerable<TRuleRole> Create(IEnumerable<TRuleRole> ruleRoles)
        {
            return Create<TRuleRole>(ruleRoles);
        }

        public void Delete(TRule rule)
        {
             Delete<TRule>(rule);
        }

        public void Delete(TRuleUser ruleUser)
        {
             Delete<TRuleUser>(ruleUser);
        }

        public void Delete(TRuleRole ruleRole)
        {
            Delete<TRuleRole>(ruleRole);
        }

        public void Delete(IEnumerable<TRule> rules)
        {
            Delete<TRule>(rules);
        }

        public void Delete(IEnumerable<TRuleUser> ruleUsers)
        {
            Delete<TRuleUser>(ruleUsers);
        }

        public void Delete(IEnumerable<TRuleRole> ruleRoles)
        {
            Delete<TRuleRole>(ruleRoles);
        }

        public IEnumerable<TRule> GetAll(Expression<Func<TRule, bool>> expression)
        {
            return DbContext.Set<TRule>().Where(expression);
        }

        public IEnumerable<TRuleUser> GetAll(Expression<Func<TRuleUser, bool>> expression)
        {
            return DbContext.Set<TRuleUser>().Where(expression).Include(ru=>ru.DataPriviegeRule);
        }

        public IEnumerable<TRuleRole> GetAll(Expression<Func<TRuleRole, bool>> expression)
        {
            return DbContext.Set<TRuleRole>().Where(expression).Include(ru => ru.DataPriviegeRule);
        }

        public IEnumerable<TRuleRole> GetDataPriviegeRuleRoleByRoleId(string tableName, string roleId)
        {
            return GetAll(rr => rr.TableName == tableName && rr.RoleId == roleId).ToList();
        }

        public IEnumerable<TRuleRole> GetDataPriviegeRuleRoleByRoleId(string tableName, IEnumerable<string> roleIds)
        {
            return GetAll(rr => rr.TableName == tableName && roleIds.Contains(rr.RoleId)).ToList();
        }

        public IEnumerable<TRuleUser> GetDataPriviegeRuleUserByUserId(string tableName, string userId)
        {
            return GetAll(ru => ru.TableName == tableName && ru.UserId==userId).ToList();
        }

        public TRule Update(TRule rule)
        {
            return Update<TRule>(rule);
        }

        public TRuleUser Update(TRuleUser ruleUser)
        {
            return Update<TRuleUser>(ruleUser);
        }

        public TRuleRole Update(TRuleRole ruleRole)
        {
            return Update<TRuleRole>(ruleRole);
        }

        public IEnumerable<TRule> Update(IEnumerable<TRule> rules)
        {
            return Update<TRule>(rules);
        }

        public IEnumerable<TRuleUser> Update(IEnumerable<TRuleUser> ruleUsers)
        {
            return Update<TRuleUser>(ruleUsers);
        }

        public IEnumerable<TRuleRole> Update(IEnumerable<TRuleRole> ruleRoles)
        {
            return Update<TRuleRole>(ruleRoles);
        }
    }
}
