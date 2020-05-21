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

        [Fact]
        public void VisitBetweenAndExpression_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("IntProp between 0 and 10");
            var context = dataPrivilegeParser.betweenAndExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);

            Expression actualExpression = visitor.VisitBetweenAndExpression(context);

            Expression parameter = Expression.Property(visitor.GetCurrentParameter(), "IntProp");
            Expression expectedExpression = Expression.AndAlso(Expression.GreaterThanOrEqual(Expression.ConvertChecked(parameter,typeof(decimal)), Expression.Constant((decimal)0)),
                                                             Expression.LessThanOrEqual(Expression.ConvertChecked(parameter, typeof(decimal)), Expression.Constant((decimal)10)));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitBetweenAndExpression_Not_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("IntProp not between 0 and 10");
            var context = dataPrivilegeParser.betweenAndExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);

            Expression actualExpression = visitor.VisitBetweenAndExpression(context);

            Expression parameter = Expression.Property(visitor.GetCurrentParameter(), "IntProp");
            Expression expectedExpression = Expression.OrElse(Expression.LessThan(Expression.ConvertChecked(parameter, typeof(decimal)), Expression.Constant((decimal)0)),
                                                             Expression.GreaterThan(Expression.ConvertChecked(parameter, typeof(decimal)), Expression.Constant((decimal)10)));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitComparisonExpression_Equals_NoCustomFields_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("id='a'");
            var context = dataPrivilegeParser.comparisonExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);

            Expression actualExpression = visitor.VisitComparisonExpression(context);
            Expression expectedExpression = Expression.Equal(Expression.Property(visitor.GetCurrentParameter(), "Id"), Expression.Constant("a"));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitComparsionExpression_Equals_CustomFields_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("id={field}");
            var context = dataPrivilegeParser.comparisonExpression();
            Mock<IDataPrivilegeFieldProvider> fieldProviderMock = new Mock<IDataPrivilegeFieldProvider>();
            fieldProviderMock.Setup(d => d.GetFieldType("field")).Returns(typeof(IEnumerable<string>));
            fieldProviderMock.Setup(d => d.ContainsField("field")).Returns(true);
            var visitor = DataPrivilegeVisitor.CreateVisitor(fieldProviderMock.Object);

            Expression actualExpression = visitor.VisitComparisonExpression(context);

            MethodInfo containsMethod = typeof(Enumerable).GetMethods().FirstOrDefault(m => m.Name == "Contains" && m.GetParameters()?.Length == 2);
            containsMethod = containsMethod.MakeGenericMethod(typeof(string));
            var parameters = Expression.PropertyOrField(Expression.Constant(visitor), "Parameters");
            var parameterValue = Expression.Call(parameters, typeof(IDictionary<string, object>).GetMethod("get_Item"), Expression.Constant("field"));
            Expression arrayExpression = Expression.ConvertChecked(parameterValue, typeof(IEnumerable<string>));
            Expression expectedExpression =Expression.Call(containsMethod, arrayExpression, Expression.Property(visitor.GetCurrentParameter(), "Id"));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitComparisonExpression_GreaterThanOrEqual_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("IntProp>  =1");
            var context = dataPrivilegeParser.comparisonExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);

            Expression actualExpression = visitor.VisitComparisonExpression(context);

            Expression parameter = Expression.Property(visitor.GetCurrentParameter(), "IntProp");
            Expression expectedExpression = Expression.GreaterThanOrEqual(Expression.ConvertChecked(parameter, typeof(decimal)), Expression.Constant((decimal)1));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }
        [Fact]
        public void VisitComparisonExpression_LessThanOrEqual_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("IntProp<  =1");
            var context = dataPrivilegeParser.comparisonExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);

            Expression actualExpression = visitor.VisitComparisonExpression(context);

            Expression parameter = Expression.Property(visitor.GetCurrentParameter(), "IntProp");
            Expression expectedExpression = Expression.LessThanOrEqual(Expression.ConvertChecked(parameter, typeof(decimal)), Expression.Constant((decimal)1));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitComparisonExpression_LessThan_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("IntProp<  1");
            var context = dataPrivilegeParser.comparisonExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);

            Expression actualExpression = visitor.VisitComparisonExpression(context);

            Expression parameter = Expression.Property(visitor.GetCurrentParameter(), "IntProp");
            Expression expectedExpression = Expression.LessThan(Expression.ConvertChecked(parameter, typeof(decimal)), Expression.Constant((decimal)1));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitComparisonExpression_GreaterThan_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("IntProp>  1");
            var context = dataPrivilegeParser.comparisonExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);

            Expression actualExpression = visitor.VisitComparisonExpression(context);

            Expression parameter = Expression.Property(visitor.GetCurrentParameter(), "IntProp");
            Expression expectedExpression = Expression.GreaterThan(Expression.ConvertChecked(parameter, typeof(decimal)), Expression.Constant((decimal)1));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitExistsExpression_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("exists (select Id from TestRelation t where t.id=TestEntity.ShadowPropId)");
            var context = dataPrivilegeParser.existsExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);

            Expression actualExpression = visitor.VisitExistsExpression(context);

            Expression dataSourceExpression= Expression.Property(Expression.Property(Expression.Constant(visitor), "DbContext"), "TestRelations");
            MethodInfo anyMethod = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "Any" && m.GetParameters()?.Length == 2);
            anyMethod = anyMethod.MakeGenericMethod(typeof(TestRelation));
            ParameterExpression parameter= Expression.Parameter(typeof(TestRelation), "_p1");
            Expression body= Expression.Equal(Expression.Property(parameter, "Id"), Expression.Property(visitor.GetCurrentParameter(), "ShadowPropId"));
            Expression expectedExpression= Expression.Call(anyMethod, dataSourceExpression, Expression.Lambda<Func<TestRelation, bool>>(body,parameter));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitGetDateExpression_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("getdate()");
            var context = dataPrivilegeParser.getDateExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression =visitor.VisitGetDateExpression(context);
            Expression expectedExpression = Expression.Property(null,typeof(DateTime),"Now");
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }
        [Fact]
        public void VisitStringExpression_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("N'aaa'");
            var context = dataPrivilegeParser.stringExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression= visitor.VisitStringExpression(context);
            Expression expectedExpression = Expression.Constant("aaa");
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }
        [Fact]
        public void VisitNumericExpression_NegativeNumber_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("-123");
            var context = dataPrivilegeParser.numericExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitNumericExpression(context);
            Expression expectedExpression = Expression.Constant((decimal)-123);
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitNumericExpression_Float_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser(".123");
            var context = dataPrivilegeParser.numericExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitNumericExpression(context);
            Expression expectedExpression = Expression.Constant((double)0.123);
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitNumericExpression_binary_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("0x1aF");
            var context = dataPrivilegeParser.numericExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            ConstantExpression actualExpression = visitor.VisitNumericExpression(context) as ConstantExpression;
            ConstantExpression expectedExpression = Expression.Constant(new byte[] {1,10,15 });
            byte[] actualBytes = actualExpression.Value as byte[];
            byte[] expectedBytes = expectedExpression.Value as byte[];
            actualBytes.Length.ShouldBe(expectedBytes.Length);
            for(int i=0;i<actualBytes.Length;i++)
            {
                actualBytes[i].ShouldBe(expectedBytes[i]);
            }
        }

        [Fact]
        public void VisitNullExpression()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("null");
            var context = dataPrivilegeParser.nullExpression();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitNullExpression(context);
            Expression expectedExpression = Expression.Constant(null);
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitCustomField_ExistsField_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("{field}");
            var context = dataPrivilegeParser.customField();
            Mock<IDataPrivilegeFieldProvider> fieldProviderMock = new Mock<IDataPrivilegeFieldProvider>();
            fieldProviderMock.Setup(d => d.GetFieldType("field")).Returns(typeof(IEnumerable<string>));
            fieldProviderMock.Setup(d => d.ContainsField("field")).Returns(true);
            var visitor = DataPrivilegeVisitor.CreateVisitor(fieldProviderMock.Object);
            Expression actualExpression = visitor.VisitCustomField(context);
            var parameters = Expression.PropertyOrField(Expression.Constant(visitor), "Parameters");
            var parameterValue = Expression.Call(parameters, typeof(IDictionary<string, object>).GetMethod("get_Item"), Expression.Constant("field"));
            Expression expectedExpression = Expression.ConvertChecked(parameterValue, typeof(IEnumerable<string>));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        [Fact]
        public void VisitCustomField_NotExistsField_Success()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("{field}");
            var context = dataPrivilegeParser.customField();
            Mock<IDataPrivilegeFieldProvider> fieldProviderMock = new Mock<IDataPrivilegeFieldProvider>();
            fieldProviderMock.Setup(d => d.GetFieldType("field")).Returns(typeof(IEnumerable<string>));
            fieldProviderMock.Setup(d => d.ContainsField("field")).Returns(false);
            var visitor = DataPrivilegeVisitor.CreateVisitor(fieldProviderMock.Object);
            Expression actualExpression = visitor.VisitCustomField(context);
            Expression expectedExpression = Expression.Constant(null);
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
            visitor.ContainsException().ShouldBeTrue();
        }

        [Fact]
        public void VisitSimpleSubquery()
        {
            DataPrivilegeParser dataPrivilegeParser = CreateParser("select id from TestRelation");
            var context = dataPrivilegeParser.simpleSubquery();
            var visitor = DataPrivilegeVisitor.CreateVisitor(null);
            Expression actualExpression = visitor.VisitSimpleSubquery(context);
            //MethodInfo whereMethod = typeof(Queryable).GetMethods().FirstOrDefault(m => {
            //    if (m.Name != "Where")
            //    {
            //        return false;
            //    }
            //    var parameters = m.GetParameters();
            //    if (parameters.Length != 2)
            //    {
            //        return false;
            //    }
            //    var generocTypes = parameters[1].ParameterType.GenericTypeArguments;
            //    if (generocTypes?.Length != 1)
            //    {
            //        return false;
            //    }
            //    if (generocTypes[0].Name != "Func`2")
            //    {
            //        return false;
            //    }
            //    return true;
            //});
            //whereMethod = whereMethod.MakeGenericMethod(typeof(TestRelation));
            MethodInfo selectMethod = typeof(Queryable).GetMethods().FirstOrDefault(m => {
                if (m.Name != "Select")
                {
                    return false;
                }
                var parameters = m.GetParameters();
                if (parameters.Length != 2)
                {
                    return false;
                }
                var generocTypes = parameters[1].ParameterType.GenericTypeArguments;
                if (generocTypes?.Length != 1)
                {
                    return false;
                }

                if (generocTypes[0].Name != "Func`2")
                {
                    return false;
                }
                return true;
            });
            selectMethod = selectMethod.MakeGenericMethod(typeof(TestRelation), typeof(string));
            
            Expression expectedExpression=Expression.Call(selectMethod, Expression.Property(Expression.PropertyOrField(Expression.Constant(visitor), "DbContext"), "TestRelations"),
                                                          Expression.Lambda<Func<TestRelation,string>>(Expression.Property(Expression.Parameter(typeof(TestRelation),"_p1"),"Id"),
                                                                                                         Expression.Parameter(typeof(TestRelation), "_p1")));
            ExpressionEqualityComparer.Instance.Equals(actualExpression, expectedExpression).ShouldBeTrue();
        }

        //[Fact]
        //public void VisitComparisonExpression_GreaterThanOrEqual_Success()
        //{
        //    DataPrivilegeParser dataPrivilegeParser = CreateParser("id!='a'");
        //    var context = dataPrivilegeParser.comparisonExpression();
        //    var visitor = DataPrivilegeVisitor.CreateVisitor(null);
        //}


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
