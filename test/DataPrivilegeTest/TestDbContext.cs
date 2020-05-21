using DataPrivilege.Extensions;
using DataPrivilege.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace DataPrivilegeTest
{
    public class TestDbContext<TRule, TRuleRole, TRuleUser>:DbContext
            where TRule : DataPrivilegeRule
            where TRuleRole : DataPrivilegeRuleRole<TRule>
            where TRuleUser : DataPrivilegeRuleUser<TRule>
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            :base(options)
        { }
        public DbSet<TRule> DataPrivilegeRules { get; set; }
        public DbSet<TRuleRole> DataPrivilegeRuleRoles { get; set; }
        public DbSet<TRuleUser> DataPrivilegeRuleUsers { get; set; }

        public DbSet<TestEntity> TestEntities { get; set; }

        public DbSet<TestRelation> TestRelations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureDataPrivilegeStore<TRule, TRuleRole, TRuleUser>();
        }
    }

    public class TestDbContext : TestDbContext<DataPrivilegeRule, DataPrivilegeRuleRole<DataPrivilegeRule>, DataPrivilegeRuleUser<DataPrivilegeRule>>
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }

    public class TestEntity
    {
        public string Id { get; set; }
        public decimal DecimalProp { get; set; }
        public float FloatProp { get; set; }
        public long LongProp { get; set; }
        public int IntProp { get; set; }
        public short ShortProp { get; set; }
        public byte ByteProp { get; set; }
        public bool BoolProp { get; set; }
        public decimal? NullableDecimalProp { get; set; }
        public float? NullableFloatProp { get; set; }
        public long? NullableLongProp { get; set; }
        public int? NullableIntProp { get; set; }
        public short? NullableShortProp { get; set; }
        public byte? NullableByteProp { get; set; }
        public bool? NullableBoolProp { get; set; }

       public string ShadowPropId { get; set; }
        public TestRelation ShadowProp { get; set; }

        //public ICollection<TestMultiple> TestMultiples { get; set; }

    }

    //public class TestMultiple
    //{
    //    public string Id { get; set; }
    //    public string Name { get; set; }

    //    public ICollection<TestEntity> TestEntities { get; set; }
    //}

    public class TestRelation
    { 
        public string Id { get; set; }
        public string Name { get; set; }

        public ICollection<TestEntity> TestEntities { get; set; }
    }
}
