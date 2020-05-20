using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using DataPrivilege;
using DataPrivilege.Converters;
using DataPrivilege.DataPrivilegeFields;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Xunit;

namespace DataPrivilegeTest
{
    public class DataPrivilegeVisitorTests
    {
        private class DataPrivilegeVisitor : DataPrivilegeVisitor<TestDbContext, TestEntity>
        {
            public new ParameterExpression GetCurrentParameter()
            {
                return base.GetCurrentParameter();
            }

            private static TestDbContext CreateDbContext()
            {
                DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
                optionsBuilder.UseInMemoryDatabase("test_" + Guid.NewGuid().ToString());
                return new TestDbContext(optionsBuilder.Options);
            }

            public static DataPrivilegeVisitor CreateVisitor(IDataPrivilegeFieldProvider dataPrivilegeFieldProvider)
            {
                IEnumerable<IExpressionConvert> expressionConverts = new List<IExpressionConvert>()
                {
                    new DateTimeExpressionConverter(),new NumericExpressionConverter(),new BooleanExpressionConverter()
                };
                ExpressionConverter converter = new ExpressionConverter(expressionConverts);
                TestDbContext dbContext = CreateDbContext();
                return new DataPrivilegeVisitor(converter, dataPrivilegeFieldProvider, dbContext);
            }
            public DataPrivilegeVisitor(ExpressionConverter converter, IDataPrivilegeFieldProvider dataPrivilegeFieldProvider, TestDbContext dbContext)
                : base(converter, dataPrivilegeFieldProvider, dbContext)
            {
                InitTableContainer();
                FieldInfo field = typeof(DataPrivilegeVisitor<TestDbContext, TestEntity>).GetField("_tableContainer", BindingFlags.NonPublic | BindingFlags.Instance);
                TableContainer = field.GetValue(this) as List<(string AliasName, string TableName, ParameterExpression Parameter)>;
            }
            public List<(string AliasName, string TableName, ParameterExpression Parameter)> TableContainer;

            public void AddTable<TEntiry>(string aliasName)
            {

                string tableName = DbContext.Model.FindEntityType(typeof(TEntiry))?.GetTableName();
                ParameterExpression parameterExpression = Expression.Parameter(typeof(TEntiry), $"_p{TableContainer.Count}");
                TableContainer.Add((aliasName, tableName, parameterExpression));
            }

            public bool ContainsException(Exception ex)
            {
                return this.Exceptions.Count(ex => ex.Message == ex.Message) > 0;
            }

            public bool ContainsException()
            {
                return this.Exceptions.Count > 0;
            }
        }
        [Fact]
        public void VisitColumnElem_NoTablePrefixAndPropertyIsFound_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("id");
            var context = dataPrivilegeParser.columnElem();
            var v = DataPrivilegeVisitor.CreateVisitor(null);
            Expression expression = v.VisitColumnElem(context);
            Expression expectedExpression = Expression.Property(v.GetCurrentParameter(), "Id");
            ExpressionEqualityComparer.Instance.Equals(expectedExpression, expression).ShouldBeTrue();
        }

        [Fact]
        public void VisitColumnElem_TableAndPropertyIsFound_success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("NullableBoolProp");
            var context = dataPrivilegeParser.columnElem();
            var v = DataPrivilegeVisitor.CreateVisitor(null);
            v.AddTable<TestRelation>("a");
            Expression expression = v.VisitColumnElem(context);
            Expression expectedExpression = Expression.Property(v.TableContainer[0].Parameter, "NullableBoolProp");
            ExpressionEqualityComparer.Instance.Equals(expectedExpression, expression).ShouldBeTrue();
        }

        [Fact]
        public void VisitColumnElem_TableIsNotFound_ThrowException()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("a.id");
            var context = dataPrivilegeParser.columnElem();
            var v = DataPrivilegeVisitor.CreateVisitor(null);
            Exception exception = Should.Throw<Exception>(() => v.VisitColumnElem(context));
            exception.Message.ShouldBe("列名前缀a无效！");
        }

        [Fact]
        public void VisitColumnElem_PropertyIsNotFound_ThrowException()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("_");
            var context = dataPrivilegeParser.columnElem();
            var v = DataPrivilegeVisitor.CreateVisitor(null);
            Exception exception = Should.Throw<Exception>(() => v.VisitColumnElem(context));
            exception.Message.ShouldBe("列名_无效！");
        }

        [Fact]
        public void VisitTableName_TableIsFound_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("TestEntity");
            var context = dataPrivilegeParser.tableName();
            var v = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = v.VisitTableName(context);
            Expression expectedExpression = Expression.Property(Expression.Property(Expression.Constant(v), "DbContext"), "TestEntities");
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitTableName_TableIsNotFound_ThrowException()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("_");
            var context = dataPrivilegeParser.tableName();
            var v = DataPrivilegeVisitor.CreateVisitor(null);
            Exception ex = Should.Throw<Exception>(() => v.VisitTableName(context));
            ex.Message.ShouldBe("表名_无效！");
        }

        [Fact]
        public void VisitIsNullExpression_IsNull_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("Id is Null");
            var context = dataPrivilegeParser.isNullExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitIsNullExpression(context);
            Expression expectedExpression = Expression.Equal(Expression.Property(visitor.GetCurrentParameter(), "Id"), Expression.Constant(null));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitIsNullExpression_IsNotNull_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("Id is not Null");
            var context = dataPrivilegeParser.isNullExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitIsNullExpression(context);
            Expression expectedExpression = Expression.NotEqual(Expression.Property(visitor.GetCurrentParameter(), "Id"), Expression.Constant(null));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitLikeExpression_like_theLiftParameterTypeIsString_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("Id like N'a'");
            var context = dataPrivilegeParser.likeExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitLikeExpression(context);
            MethodInfo containsMethod = typeof(string).GetMethod("Contains", new Type[] { typeof(string) });
            Expression expectedExpression = Expression.Call(Expression.Property(visitor.GetCurrentParameter(), "Id"), containsMethod, Expression.Constant("a"));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitLikeExpression_NotLike_theLiftParameterTypeIsString_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("Id not like 'a'");
            var context = dataPrivilegeParser.likeExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitLikeExpression(context);
            MethodInfo containsMethod = typeof(string).GetMethod("Contains", new Type[] { typeof(string) });
            Expression expectedExpression = Expression.Not(Expression.Call(Expression.Property(visitor.GetCurrentParameter(), "Id"), containsMethod, Expression.Constant("a")));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitLikeExpression_Like_theLiftParameterTypeIsNotString_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("BoolProp not like 'a'");
            var context = dataPrivilegeParser.likeExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitLikeExpression(context);
            MethodInfo containsMethod = typeof(string).GetMethod("Contains", new Type[] { typeof(string) });
            Expression expectedExpression = Expression.Not(Expression.Call(Expression.Property(visitor.GetCurrentParameter(), "Id"), containsMethod, Expression.Constant("a")));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeFalse();
            visitor.ContainsException().ShouldBeTrue();
        }

        [Fact]
        public void VisitInExpression_NoCustomField_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("Id in ('a','b','c')");
            var context = dataPrivilegeParser.inExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitInExpression(context);
            MethodInfo containsMethod = typeof(Enumerable).GetMethods().FirstOrDefault(m => m.Name == "Contains" && m.GetParameters()?.Length == 2);
            containsMethod = containsMethod.MakeGenericMethod(typeof(string));
            Expression arrayExpression= Expression.NewArrayInit(typeof(string), Expression.Constant("a"), Expression.Constant("b"), Expression.Constant("c"));
            Expression expectedExpression = Expression.Call(containsMethod, arrayExpression, Expression.Property(visitor.GetCurrentParameter(), "Id"));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitInExpression_CustomFields_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("Id in ('a','b','c',{field})");
            var context = dataPrivilegeParser.inExpression();
            Mock<IDataPrivilegeFieldProvider> fieldProviderMock = new Mock<IDataPrivilegeFieldProvider>();
            fieldProviderMock.Setup(d => d.GetFieldType("field")).Returns(typeof(IEnumerable<string>));
            fieldProviderMock.Setup(d => d.ContainsField("field")).Returns(true);
            var visitor = DataPrivilegeVisitor.CreateVisitor(fieldProviderMock.Object);
            Expression actualExpression = visitor.VisitInExpression(context);
            MethodInfo containsMethod = typeof(Enumerable).GetMethods().FirstOrDefault(m => m.Name == "Contains" && m.GetParameters()?.Length == 2);
            containsMethod = containsMethod.MakeGenericMethod(typeof(string));
            Expression arrayExpression = Expression.NewArrayInit(typeof(string), Expression.Constant("a"), Expression.Constant("b"), Expression.Constant("c"));
            Expression expectedExpression = Expression.Call(containsMethod, arrayExpression, Expression.Property(visitor.GetCurrentParameter(), "Id"));
            var parameters = Expression.PropertyOrField(Expression.Constant(visitor), "Parameters");
            var parameterValue = Expression.Call(parameters, typeof(IDictionary<string, object>).GetMethod("get_Item"), Expression.Constant("field"));
            Expression subArrayExpression = Expression.ConvertChecked(parameterValue, typeof(IEnumerable<string>));
            expectedExpression = Expression.OrElse(Expression.Call(containsMethod, subArrayExpression, Expression.Property(visitor.GetCurrentParameter(), "Id")), expectedExpression);
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }
        //public void 


        private static DataPrivilegeParser CreateParser(string conditionExpression)
        {
            ICharStream charStream = new AntlrInputStream(conditionExpression);
            DataPrivilegeLexer dataPrivilegeParserLexer = new DataPrivilegeLexer(charStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(dataPrivilegeParserLexer);
            DataPrivilegeParser dataPrivilegeParser = new DataPrivilegeParser(commonTokenStream);
            return dataPrivilegeParser;
        }
    }
}
