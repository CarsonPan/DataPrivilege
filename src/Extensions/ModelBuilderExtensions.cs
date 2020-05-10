using DataPrivilege.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataPrivilege.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void ConfigureDataPrivilegeStore<TRule, TRuleRole, TRuleUser>(this ModelBuilder modelBuilder,
                                                                                 Action<EntityTypeBuilder<TRule>> ruleBuildAction = null,
                                                                                 Action<EntityTypeBuilder<TRuleRole>> ruleRoleBuildAction = null,
                                                                                 Action<EntityTypeBuilder<TRuleUser>> ruleUserBuildAction = null)
            where TRule : DataPrivilegeRule
            where TRuleRole : DataPrivilegeRuleRole<TRule>
            where TRuleUser : DataPrivilegeRuleUser<TRule>
        {
            modelBuilder.Entity<TRule>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property<string>(t => t.Id).HasMaxLength(50).IsRequired();
                b.Property<string>(t => t.TableName).HasMaxLength(150).IsRequired();
                b.Property<string>(t => t.ConditionExpression).HasMaxLength(1024).IsRequired();
                b.Property<string>(t => t.Description).HasMaxLength(1024);
                ruleBuildAction?.Invoke(b);
            });
            modelBuilder.Entity<TRuleRole>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property<string>(t => t.Id).HasMaxLength(50).IsRequired();
                b.Property<string>(t => t.RoleId).HasMaxLength(50).IsRequired();
                b.Property<string>(t => t.TableName).HasMaxLength(150).IsRequired();
                b.Property<DataOperation>(t => t.DataOperation).IsRequired();
                b.HasOne<TRule>(t=>t.DataPriviegeRule).WithMany();
                ruleRoleBuildAction?.Invoke(b);
            });

            modelBuilder.Entity<TRuleUser>(b => {
                b.HasKey(ru => ru.Id);
                b.Property<string>(t => t.UserId).HasMaxLength(50).IsRequired();
                b.Property<string>(t => t.TableName).HasMaxLength(150).IsRequired();
                b.Property<DataOperation>(t => t.DataOperation).IsRequired();
                b.HasOne<TRule>(t => t.DataPriviegeRule).WithMany();
                ruleUserBuildAction?.Invoke(b);
            });
        }
    }
}
