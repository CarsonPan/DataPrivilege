using DataPrivilege.Models;
using DataPrivilege.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DataPrivilegeTest.Repositories
{
    public class DataPriviegeRepositoryTests
    {
        //public void Create_Rule_ReturnsSameRule()
        //{
        //    TestDbContext dbContext = CreateDbContext();
        //    var repository = CteateRepository(dbContext);
        //    DataPrivilegeRule dataPrivilegeRule = new DataPrivilegeRule() { Id = "0" };
        //    dataPrivilegeRule= repository.Create(dataPrivilegeRule);
        //    dataPrivilegeRule.Id.ShouldBe("0");
        //}

        //public void Create_Rule_RuleIsCreated()
        //{
        //    TestDbContext dbContext = CreateDbContext();
        //    var repository = CteateRepository(dbContext);
        //    DataPrivilegeRule dataPrivilegeRule = new DataPrivilegeRule() { Id = "0" };
        //    repository.Create(dataPrivilegeRule);
        //    dbContext = CreateDbContext();
        //    var rule= dbContext.DataPrivilegeRules.Find("0");
        //    rule.ShouldNotBeNull();
        //}
        [Fact]
        public void Update_EntityIsNotFound_ThrowDbUpdateConcurrencyException()
        {
            TestDbContext dbContext = CreateDbContext();
            var respository = CteateRepository(dbContext);
            Should.Throw<DbUpdateConcurrencyException>(() => respository.Update(new DataPrivilegeRule() { Id = "1" }));
        }
        [Fact]
        public void Update_EntityIsFound_EntityIsUpdated()
        {
            TestDbContext dbContext = CreateDbContext();
            dbContext.DataPrivilegeRules.Add(new DataPrivilegeRule() { Id = "0", TableName = "c", ConditionExpression = "a=b" });
            dbContext.SaveChanges();
            dbContext = CreateDbContext();
            var respository = CteateRepository(dbContext);
            var rule = respository.Update(new DataPrivilegeRule() { Id = "0", Description = "hello world" });
            var des = dbContext.DataPrivilegeRules.Find("0");
            rule.Id.ShouldBe(des.Id);
            rule.Description.ShouldBe(des.Description);
            des.Description.ShouldBe("hello world");
        }

        [Fact]
        public void Delete_EntityIsNotFound_ThrowDbUpdateConcurrencyException()
        {
            TestDbContext dbContext = CreateDbContext();
            var respository = CteateRepository(dbContext);
            Should.Throw<DbUpdateConcurrencyException>(() => respository.Delete(new DataPrivilegeRule() { Id = "1" }));

        }

        [Fact]
        public void Delete_EntityIsFound_EntityIsDeleted()
        {
            TestDbContext dbContext = CreateDbContext();
            dbContext.DataPrivilegeRules.Add(new DataPrivilegeRule() { Id = "1", TableName = "c", ConditionExpression = "a=b" });
            dbContext.SaveChanges();
            dbContext = CreateDbContext();
            var respository = CteateRepository(dbContext);
            respository.Delete(new DataPrivilegeRule() { Id = "1" });
            var rule = dbContext.DataPrivilegeRules.Find("1");
            rule.ShouldBeNull();

        }

        private TestDbContext CreateDbContext()
        {
            DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            optionsBuilder.UseInMemoryDatabase("test");
            return new TestDbContext(optionsBuilder.Options);
        }
        private static DataPriviegeRepository<TestDbContext, DataPrivilegeRule, DataPrivilegeRuleRole<DataPrivilegeRule>, DataPrivilegeRuleUser<DataPrivilegeRule>> CteateRepository(TestDbContext dbContext)
        {

            return new DataPriviegeRepository<TestDbContext, DataPrivilegeRule, DataPrivilegeRuleRole<DataPrivilegeRule>, DataPrivilegeRuleUser<DataPrivilegeRule>>(dbContext);
        }
    }
}
