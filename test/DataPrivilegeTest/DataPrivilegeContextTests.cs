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

namespace DataPrivilegeTest
{
    public class DataPrivilegeContextTests
    {
       
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

        private TestDbContext CreateDbContext()
        {
            DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            optionsBuilder.UseInMemoryDatabase("test");
            return new TestDbContext(optionsBuilder.Options);
        }
    }
}
