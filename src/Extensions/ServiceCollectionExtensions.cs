using DataPrivilege;
using DataPrivilege.Converters;
using DataPrivilege.DataPrivilegeFields;
using DataPrivilege.Models;
using DataPrivilege.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private static void CheckDbContext<TDbContext, TRule, TRuleRole, TRuleUser>()
        {
            IEnumerable<PropertyInfo> propertyInfos = typeof(TDbContext).GetProperties().Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));
            var rule = propertyInfos.SingleOrDefault(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(typeof(TRule)));
            var ruleRole= propertyInfos.SingleOrDefault(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(typeof(TRuleRole)));
            var ruleUser = propertyInfos.SingleOrDefault(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(typeof(TRuleUser)));
            string errorMessage = null;
            if(rule==null)
            {
                errorMessage = "DbSet<" + typeof(TRule).Name + "> ";
            }
            if(ruleRole==null)
            {
                errorMessage=errorMessage+ "DbSet<" + typeof(TRuleRole).Name + "> ";
            }
            if(ruleUser==null)
            {
                errorMessage = errorMessage + "DbSet<" + typeof(TRuleUser).Name + "> ";
            }
            if(errorMessage!=null)
            {
                throw new Exception("类" + typeof(TDbContext).Name + " 种缺少必要公共属性：" + errorMessage);
            }
        }

        public static IServiceCollection AddEfCoreDataPrivilege<TDbContext, TUserSessionInfo>(this IServiceCollection services)
            where TDbContext : DbContext
            where TUserSessionInfo : class, IUserSessionInfo
        {
            return services.AddEfCoreDataPrivilege<TDbContext, DataPrivilegeRule, DataPrivilegeRuleRole<DataPrivilegeRule>, DataPrivilegeRuleUser<DataPrivilegeRule>, TUserSessionInfo>();
        }

        public static IServiceCollection AddEfCoreDataPrivilege<TDbContext,TRule,TRuleRole,TRuleUser, TUserSessionInfo>(this IServiceCollection services)
            where TDbContext:DbContext
            where TRule:DataPrivilegeRule
            where TRuleRole:DataPrivilegeRuleRole<TRule>
            where TRuleUser:DataPrivilegeRuleUser<TRule>
            where TUserSessionInfo: class,IUserSessionInfo
        {
            CheckDbContext<TDbContext, TRule, TRuleRole, TRuleUser>();
            services.AddScoped(typeof(DataPrivilegeVisitor<,>));
            services.AddScoped<ExpressionConverter>();
            services.AddScoped<IExpressionConvert, DateTimeExpressionConverter>();
            services.AddScoped<IExpressionConvert, NumericExpressionConverter>();
            services.AddScoped<IExpressionConvert, BooleanExpressionConverter>();
            services.AddScoped<IDataPrivilegeFieldProvider, DataPrivilegeFieldProvider>();
            services.AddScoped(typeof(DataPrivilegeVisitor<,>));
            services.AddScoped<IDataPriviegeRepository<TRule,TRuleRole,TRuleUser>, DataPriviegeRepository<TDbContext,TRule,TRuleRole,TRuleUser>>();
            services.LoadDataPrivilegeField();
            services.AddDataPrivilegeManager<TDbContext, TRule, TRuleRole, TRuleUser>();
            services.AddScoped<IUserSessionInfo, TUserSessionInfo>();
            return services;
        }

        private static IServiceCollection AddDataPrivilegeManager<TDbContext, TRule, TRuleRole, TRuleUser>(this IServiceCollection services)
        {
            IEnumerable<PropertyInfo> propertyInfos= typeof(TDbContext).GetProperties().Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));
            if(propertyInfos.Any())
            {
                Type interfaceType;
                Type implType;
                foreach(PropertyInfo property in propertyInfos)
                {
                    interfaceType = typeof(IDataPrivilegeManager<>).MakeGenericType(property.PropertyType);
                    implType = typeof(DataPrivilegeManager<,,,,>).MakeGenericType(typeof(TDbContext), property.PropertyType,typeof(TRule),typeof(TRuleRole),typeof(TRuleUser));
                    services.AddScoped(interfaceType, implType);
                }
            }
            return services;
        }
    }
}
