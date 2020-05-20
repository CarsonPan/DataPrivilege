using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using DataPrivilege.Models;
using DataPrivilege;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using DataPrivilege.DataPrivilegeFields;
using System.Reflection;
using System.Collections.Concurrent;
using Xunit;
using System.Linq;

namespace DataPrivilegeTest
{
    public class DataPrivilegeContextTests
    {
        [Fact]
        public void VertifyRule_ParseError_ReturnsFalse()
        {
            var visitorMock = new Mock<DataPrivilegeVisitor<TestDbContext, TestEntity>>(null, null, null);
            VisitResult<TestEntity> visitResult = new VisitResult<TestEntity>(null, new List<Exception>() { new Exception()}, new List<string>());
            visitorMock.Setup(v => v.Visit("a=c")).Returns(visitResult);
            DataPrivilegeVisitor<TestDbContext, TestEntity> dataPrivilegeVisitor = visitorMock.Object;
            DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule> context =
                   new DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule>(new List<DataPrivilegeRule>(), null, null, dataPrivilegeVisitor);

            bool result = context.VertifyRule(new DataPrivilegeRule() { TableName = "table", ConditionExpression = "a=c" }, out IList<Exception> exceptions);

            result.ShouldBeFalse();
            exceptions.ShouldNotBeEmpty();
            visitorMock.Verify(v => v.Visit(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void VertifyRule_ParseSuccess_ReturnsTrueAndExceptionsIsEmpty()
        {
            var visitorMock = new Mock<DataPrivilegeVisitor<TestDbContext, TestEntity>>(null,null,null);
            VisitResult<TestEntity> visitResult = new VisitResult<TestEntity>(t => t.BoolProp, new List<Exception>(), new List<string>());
            visitorMock.Setup(v => v.Visit(It.IsNotNull<string>())).Returns(visitResult);
            DataPrivilegeVisitor<TestDbContext, TestEntity> dataPrivilegeVisitor = visitorMock.Object;
            DataPrivilegeContext <TestDbContext, TestEntity, DataPrivilegeRule> context = 
                   new DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule>(new List<DataPrivilegeRule>(),null,null,dataPrivilegeVisitor);

            bool result = context.VertifyRule(new DataPrivilegeRule() { TableName="t",ConditionExpression="a=b"}, out IList<Exception> exceptions);

            result.ShouldBeTrue();
            exceptions.ShouldBeEmpty();
            visitorMock.Verify(v => v.Visit(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void VertifyRule_CacheHits_ReturnsTrueAndExceptionsIsNull()
        {
            var visitorMock = new Mock<DataPrivilegeVisitor<TestDbContext, TestEntity>>(null,null,null);
            DataPrivilegeVisitor<TestDbContext, TestEntity> dataPrivilegeVisitor = visitorMock.Object;
            DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule> context =
                  new DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule>(new List<DataPrivilegeRule>(), null, null, dataPrivilegeVisitor);
            DataPrivilegeRule rule = new DataPrivilegeRule() { TableName = "t0", ConditionExpression = "a=b" };
            DataPrivilegeInfo<TestEntity> dataPrivilegeInfo = new DataPrivilegeInfo<TestEntity>(t => t.BoolProp, new List<string>());
            //反射设置缓存
            FieldInfo field = typeof(DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule>).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static);
            var cache = field.GetValue(null) as ConcurrentDictionary<string, DataPrivilegeInfo<TestEntity>>;
            MethodInfo methodInfo = typeof(DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule>).GetMethod("GetCacheKey",BindingFlags.NonPublic|BindingFlags.Instance,Type.DefaultBinder, new Type[] { typeof(DataPrivilegeRule) },null);
            object key = methodInfo.Invoke(context, new object[] { rule });
            cache.TryAdd(key.ToString(), dataPrivilegeInfo);
           
            bool result = context.VertifyRule(rule, out IList<Exception> exceptions);

            result.ShouldBeTrue();
            exceptions.ShouldBeNull();
            visitorMock.Verify(v => v.Visit(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void QueryableFilter_NoCustomFields_Success()
        {
            var visitorMock = new Mock<DataPrivilegeVisitor<TestDbContext, TestEntity>>(null, null, null);
            visitorMock.Setup(d => d.Visit("BoolProp=1 and IntProp!=0"))
                       .Returns(new VisitResult<TestEntity>(_p => _p.BoolProp == true && _p.IntProp != 0, new List<Exception>(), new List<string>()));
            visitorMock.Setup(d => d.Visit("ByteProp!=0"))
                       .Returns(new VisitResult<TestEntity>(_p => _p.ByteProp!=0, new List<Exception>(), new List<string>()));
            TestDbContext dbContext = CreateDbContext();
            DataPrivilegeVisitor<TestDbContext, TestEntity> dataPrivilegeVisitor = visitorMock.Object;
            List<DataPrivilegeRule> rules = new List<DataPrivilegeRule>()
            {
                new DataPrivilegeRule(){TableName="TestEntity",ConditionExpression="BoolProp=1 and IntProp!=0"},
                new DataPrivilegeRule(){TableName="TestEntity",ConditionExpression="ByteProp!=0"}
            };
            DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule> context =
                  new DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule>(rules, dbContext, null, dataPrivilegeVisitor);
            dbContext.TestEntities.Add(new TestEntity() {Id="0", BoolProp = true, IntProp = 1 });
            dbContext.TestEntities.Add(new TestEntity() {Id="1", ByteProp = 1 });
            dbContext.TestEntities.Add(new TestEntity() { Id = "11" });
            dbContext.TestEntities.Add(new TestEntity() { Id = "12" });
            dbContext.SaveChanges();

            var entities= context.Filter(dbContext.TestEntities).ToList();

            entities.Count.ShouldBe(2);
            entities.ShouldContain(t => t.Id == "0");
            entities.ShouldContain(t => t.Id == "1");
        }


        [Fact]
        public void EnumerableFilter_NoCustomFields_Success()
        {
            var visitorMock = new Mock<DataPrivilegeVisitor<TestDbContext, TestEntity>>(null, null, null);
            visitorMock.Setup(d => d.Visit("BoolProp=1 and IntProp!=0"))
                       .Returns(new VisitResult<TestEntity>(_p => _p.BoolProp == true && _p.IntProp != 0, new List<Exception>(), new List<string>()));
            visitorMock.Setup(d => d.Visit("ByteProp!=0"))
                       .Returns(new VisitResult<TestEntity>(_p => _p.ByteProp != 0, new List<Exception>(), new List<string>()));
            TestDbContext dbContext = CreateDbContext();
            DataPrivilegeVisitor<TestDbContext, TestEntity> dataPrivilegeVisitor = visitorMock.Object;
            List<DataPrivilegeRule> rules = new List<DataPrivilegeRule>()
            {
                new DataPrivilegeRule(){TableName="TestEntity",ConditionExpression="BoolProp=1 and IntProp!=0"},
                new DataPrivilegeRule(){TableName="TestEntity",ConditionExpression="ByteProp!=0"}
            };
            DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule> context =
                  new DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule>(rules, dbContext, null, dataPrivilegeVisitor);
            List<TestEntity> list = new List<TestEntity>();
            list.Add(new TestEntity() { Id = "0", BoolProp = true, IntProp = 1 });
            list.Add(new TestEntity() { Id = "1", ByteProp = 1 });
            list.Add(new TestEntity() { Id = "11" });
            list.Add(new TestEntity() { Id = "12" });

            var entities = context.Filter(list).ToList();

            entities.Count.ShouldBe(2);
            entities.ShouldContain(t => t.Id == "0");
            entities.ShouldContain(t => t.Id == "1");
        }

        [Fact]
        public void Filter_CustomFields_Success()
        {
            TestDbContext dbContext = CreateDbContext();

           var fieldProvider = new Mock<IDataPrivilegeFieldProvider>();
            fieldProvider.Setup(f =>
            f.GetFieldValue("num")).Returns(0);
            DataPrivilegeContext context =
                  new DataPrivilegeContext(new List<DataPrivilegeRule>(), dbContext, fieldProvider.Object, null);
            dbContext.TestEntities.Add(new TestEntity() { Id = "0", BoolProp = true, IntProp = 1 });
            dbContext.TestEntities.Add(new TestEntity() { Id = "1", ByteProp = 1 });
            dbContext.TestEntities.Add(new TestEntity() { Id = "11" });
            dbContext.TestEntities.Add(new TestEntity() { Id = "12" });
            dbContext.SaveChanges();

            var entities = context.Filter(dbContext.TestEntities).ToList();

            entities.Count.ShouldBe(1);
            entities.ShouldContain(t => t.Id == "0");
        }

        private class DataPrivilegeContext : DataPrivilegeContext<TestDbContext, TestEntity, DataPrivilegeRule>
        {
            public DataPrivilegeContext(List<DataPrivilegeRule> rules, TestDbContext dbContext, IDataPrivilegeFieldProvider dataPrivilegeFieldProvider, DataPrivilegeVisitor<TestDbContext, TestEntity> dataPrivilegeVisitor) : base(rules, dbContext, dataPrivilegeFieldProvider, dataPrivilegeVisitor)
            {
            }

            public new  IQueryable<TestEntity> Filter(IQueryable<TestEntity> entities)
            {
                return entities.Where(DataPrivilegeInfo.PredicateExpression);
            }
            public override DataPrivilegeInfo<TestEntity> DataPrivilegeInfo => new DataPrivilegeInfo<TestEntity>(_p => _p.BoolProp == true && _p.IntProp != (int)Parameters["num"], new List<string>() { "num" });
        }

        private TestDbContext CreateDbContext()
        {
            DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            optionsBuilder.UseInMemoryDatabase("test_"+Guid.NewGuid().ToString());
            return new TestDbContext(optionsBuilder.Options);
        }
    }
}
