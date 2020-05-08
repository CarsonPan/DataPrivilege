using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using DataPrivilege.Converters;
using DataPrivilege.DataPrivilegeFields;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DataPrivilege
{

    public class DataPrivilegeVisitor<TDbContext, TEntity> : DataPrivilegeBaseVisitor<Expression>
        where TDbContext:DbContext
        where TEntity:class
    {
        
        public IDictionary<string, object> Parameters { get; set; }
        public TDbContext DbContext { get; set; }
        protected List<Exception> Exceptions = new List<Exception>();
        protected List<string> CustomFields = new List<string>();
        List<(string AliasName, string TableName, ParameterExpression Parameter)> _tableContainer = new List<(string AliasName, string TableName, ParameterExpression Parameter)>();
        protected readonly ExpressionConverter ExpressionConverter;
        protected readonly IDataPrivilegeFieldProvider DataPrivilegeFieldProvider;
        public DataPrivilegeVisitor(ExpressionConverter converter,
                                    IDataPrivilegeFieldProvider dataPrivilegeFieldProvider,
                                    TDbContext dbContext)
        {
            ExpressionConverter = converter;
            DataPrivilegeFieldProvider = dataPrivilegeFieldProvider;
            DbContext = dbContext;
        }
        private void AddExceptionIfExists(ParserRuleContext context)
        {
            if (context.exception != null)
            {
                Exceptions.Add(context.exception);
            }
        }
        public override Expression VisitSearchCondition(DataPrivilegeParser.SearchConditionContext context)
        {
            AddExceptionIfExists(context);
            var children = context.searchConditionAnd();
            Expression expression = VisitSearchConditionAnd(children[0]);
            if (children.Length > 1)
            {
                Expression right = null;
                for (int i = 1; i < children.Length; i++)
                {
                    right = VisitSearchConditionAnd(children[i]);
                    expression = Expression.OrElse(expression, right);
                }
            }
            return expression;
        }

        public override Expression VisitSearchConditionAnd(DataPrivilegeParser.SearchConditionAndContext context)
        {
            AddExceptionIfExists(context);
            var children = context.searchConditionNot();
            var firstChildren = children[0];
            Expression expression = VisitSearchConditionNot(firstChildren);
            if (children.Length > 1)
            {
                Expression right = null;
                for (int i = 1; i < children.Length; i++)
                {
                    right = VisitSearchConditionNot(children[i]);
                    expression = Expression.AndAlso(expression, right);
                }
            }
            return expression;
        }

        public override Expression VisitSearchConditionNot(DataPrivilegeParser.SearchConditionNotContext context)
        {
            AddExceptionIfExists(context);
            var child = context.predicate();
            Expression expression = VisitPredicate(child);
            if (context.ChildCount > 1)
            {
                expression = Expression.Not(expression);
            }
            return expression;
        }

        public override Expression VisitPredicate(DataPrivilegeParser.PredicateContext context)
        {
            AddExceptionIfExists(context);
            var existsExpressionContext = context.existsExpression();
            if (existsExpressionContext != null)
            {
                return VisitExistsExpression(existsExpressionContext);
            }
            var comparisonExressionContext = context.comparisonExpression();
            if (comparisonExressionContext != null)
            {
                return VisitComparisonExpression(comparisonExressionContext);
            }
            var betweenAndExpressionContext = context.betweenAndExpression();
            if (betweenAndExpressionContext != null)
            {
                return VisitBetweenAndExpression(betweenAndExpressionContext);
            }
            var inExpressionContext = context.inExpression();
            if (inExpressionContext != null)
            {
                return VisitInExpression(inExpressionContext);
            }
            var likeExpressionContext = context.likeExpression();
            if (likeExpressionContext != null)
            {
                return VisitLikeExpression(likeExpressionContext);
            }
            var isNullExpressionContext = context.isNullExpression();
            if (isNullExpressionContext != null)
            {
                return VisitIsNullExpression(isNullExpressionContext);
            }

            return VisitSearchCondition(context.searchCondition());
        }

        public override Expression VisitExistsExpression(DataPrivilegeParser.ExistsExpressionContext context)
        {
            AddExceptionIfExists(context);
            bool not = context.NOT() != null;
            if (not)
            {
            }
            return base.VisitExistsExpression(context);
        }
        private void Convert(ref Expression left, ref Expression right)
        {
            if (left.Type != right.Type)
            {
                var leftType = typeof(IEnumerable<>).MakeGenericType(left.Type);
                var rightType = typeof(IEnumerable<>).MakeGenericType(right.Type);
                if (leftType.IsAssignableFrom(right.Type) || rightType.IsAssignableFrom(left.Type))
                {
                    return;
                }
                ExpressionConverter.Convert(ref left, ref right);
                //if (left.NodeType == ExpressionType.MemberAccess)
                //{
                //    right = Expression.Constant(right, left.Type);
                //}
                //else
                //{
                //    left = Expression.Convert(left, right.Type);
                //}
            }
        }

        public override Expression VisitComparisonExpression(DataPrivilegeParser.ComparisonExpressionContext context)
        {
            AddExceptionIfExists(context);
            var expressionContexts = context.expression();
            //if(expressionContexts[0].GetText()==expressionContexts[1].GetText())
            //{
            //    Exceptions.Add(new Exception("布尔表达式两边不能为同一个值"));
            //}

            Expression left = VisitExpression(expressionContexts[0]);
            Expression right = VisitExpression(expressionContexts[1]);

            Convert(ref left, ref right);
            string op = context.comparisonOperator().GetText();
            Expression expression = null;
            switch (op)
            {
                case "=":
                    var leftType = typeof(IEnumerable<>).MakeGenericType(left.Type);
                    var rightType = typeof(IEnumerable<>).MakeGenericType(right.Type);
                    if (leftType.IsAssignableFrom(right.Type))
                    {
                        MethodInfo containsMethod = typeof(Enumerable).GetMethod("Contains", new Type[] { typeof(IEnumerable<>).MakeGenericType(left.Type), left.Type });
                        expression = Expression.Call(containsMethod, right, left);
                    }
                    else if (rightType.IsAssignableFrom(left.Type))
                    {
                        MethodInfo containsMethod = typeof(Enumerable).GetMethod("Contains", new Type[] { typeof(IEnumerable<>).MakeGenericType(right.Type), right.Type });
                        expression = Expression.Call(containsMethod, left, right);
                    }
                    else
                    {
                        expression = Expression.Equal(left, right);
                    }
                    break;
                case ">":
                    expression = Expression.GreaterThan(left, right);
                    break;
                case "<":
                    expression = Expression.LessThan(left, right);
                    break;
                case "<=":
                case "!>":
                    expression = Expression.LessThanOrEqual(left, right);
                    break;
                case ">=":
                case "!<":
                    expression = Expression.GreaterThan(left, right);
                    break;
                default:
                    expression = Expression.NotEqual(left, right);
                    break;
            }
            return expression;
        }
       
     
        private Expression GetTableExpression(string tableName)
        {
            IEnumerable<PropertyInfo> propertyInfos = typeof(TDbContext).GetProperties(BindingFlags.Public)
                                                                       .Where(p => p.PropertyType.IsGenericType
                                                                              && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            PropertyInfo p = propertyInfos.FirstOrDefault(p =>
            {
                Type entityType = p.PropertyType.GenericTypeArguments[0];
                return DbContext.Model.FindEntityType(entityType)?.GetTableName().ToLower() == tableName.ToLower();
            });
            if(p==null)
            {
                throw new Exception($"表名{tableName}无效！");
            }
            return Expression.Property(Expression.PropertyOrField(Expression.Constant(this), "DbContext"),p);
        }

       

        private  IProperty GetProperty(string tableName, string columnName)
        {
            IEnumerable<PropertyInfo> propertyInfos = typeof(TDbContext).GetProperties()
                                                                       .Where(p => p.PropertyType.IsGenericType
                                                                              && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            PropertyInfo p = propertyInfos.FirstOrDefault(p =>
            {
                Type entityType = p.PropertyType.GenericTypeArguments[0];
                return DbContext.Model.FindEntityType(entityType)?.GetTableName().ToLower() == tableName.ToLower();
            });
            if (p == null)
            {
                return null;
            }
            var entity = DbContext.Model.FindEntityType(p.PropertyType.GenericTypeArguments[0]);
            IProperty columnProperty=  entity.GetProperties().FirstOrDefault(p => p.GetColumnName().ToLower() == columnName.ToLower());
            return columnProperty;
        }
        public override Expression VisitColumnElem(DataPrivilegeParser.ColumnElemContext context)
        {
            AddExceptionIfExists(context);
            string aliasTableName = null;
            string colName = null;
            if (context.ChildCount == 3)
            {
                aliasTableName = context.ID(0).GetText();
                colName = context.ID(1).GetText();
            }
            else {
                colName = context.ID(0).GetText();
            }
            Expression parameter =null;
            IProperty property = null;
            if (aliasTableName == null)
            {
                
                for (int i = _tableContainer.Count - 1; i >= 0; i--)
                {
                    property = GetProperty(_tableContainer[i].TableName, colName);
                    if (property != null)
                    {
                        parameter = _tableContainer[i].Parameter;
                        break;
                    }
                }
               
            }
            else
            {
                for (int i=_tableContainer.Count-1;i>=0;i--)
                {
                    if(_tableContainer[i].AliasName== aliasTableName)
                    {
                        parameter = _tableContainer[i].Parameter;
                        property = GetProperty(_tableContainer[i].TableName, colName);
                        break;
                    }
                }
                if (parameter == null)
                {
                    throw new Exception($"列名前缀{aliasTableName}无效！");
                }
            }
            if (property == null)
            {
                throw new Exception($"列名{colName}无效！");
            }
            return GetPropertyExpression(colName, parameter, property);

        }

        private static Expression GetPropertyExpression(string colName, Expression parameter, IProperty property)
        {
            
            if (property.IsShadowProperty())
            {
                MethodInfo propertyMethod = typeof(EF).GetMethod("Property");
                propertyMethod = propertyMethod.MakeGenericMethod(property.ClrType);
                return Expression.Call(propertyMethod, parameter, Expression.Constant(property.Name));
            }
            else
            {
                return Expression.Property(parameter, property.PropertyInfo);
            }
        }

        public override Expression VisitStringExpression(DataPrivilegeParser.StringExpressionContext context)
        {
            AddExceptionIfExists(context);
            var node = context.STRING();
            string value = node.GetText();
            int index = value.StartsWith('N') ? 2 : 1;
            int length = value.Length - index - 1;
            value = value.Substring(index, length);
            Expression expression = Expression.Constant(value, typeof(string));
            return expression;
        }


        public override Expression VisitNumericExpression(DataPrivilegeParser.NumericExpressionContext context)
        {
            AddExceptionIfExists(context);
            string sign = "+";
            if (context.ChildCount == 2)
            {
                sign = context.GetChild(0).GetText();

            }
            var decimalContext = context.DECIMAL();
            if (decimalContext != null)
            {
                decimal value = decimal.Parse(decimalContext.GetText());
                if (sign == "-")
                {
                    value = value * -1;
                }
                return Expression.Constant(value, typeof(decimal));
            }
            var binayContext = context.BINARY();
            if (binayContext != null)
            {
                string binaryString= binayContext.GetText();
                binaryString = binaryString.Substring(2);
                byte[] data = new byte[binaryString.Length];
                for(int i=0;i<binaryString.Length;i++)
                {
                    if(binaryString[i]<65)
                    {
                        data[i] = (byte)(binaryString[i] - 48);
                    }
                    else if(binaryString[i]<97)
                    {
                        data[i] = (byte)(binaryString[i] - 55);
                    }else
                    {
                        data[i] = (byte)(binaryString[i] - 87);
                    }
                }
                return Expression.Constant(data, typeof(byte[]));
            }
            string valueStr = context.ChildCount == 2 ? context.GetChild(1).GetText() : context.GetChild(0).GetText();
            double _value = double.Parse(valueStr);
            if (sign == "-")
            {
                _value = _value * -1;
            }
            return Expression.Constant(_value, typeof(double));
        }

        public override Expression VisitNullExpression(DataPrivilegeParser.NullExpressionContext context)
        {
            AddExceptionIfExists(context);
            return Expression.Constant(null);
        }
        private Expression GetCustomFieldValueExpression(string customFieldName)
        {
            var parameters = Expression.PropertyOrField(Expression.Constant(this), "Parameters");
            var parameterValue = Expression.Call(parameters, typeof(IDictionary<string, object>).GetMethod("get_Item"), Expression.Constant(customFieldName));
            Type type = DataPrivilegeFieldProvider.GetFieldType(customFieldName);
            return Expression.ConvertChecked(parameterValue, type);
        }
        public override Expression VisitCustomField(DataPrivilegeParser.CustomFieldContext context)
        {
            AddExceptionIfExists(context);
            string customField = context.GetText();
            Expression expression = GetCustomFieldValueExpression(customField);
            if (!CustomFields.Contains(customField))
            {
                CustomFields.Add(customField);
            }
            return expression;
        }

        public override Expression VisitGetDateExpression(DataPrivilegeParser.GetDateExpressionContext context)
        {
            AddExceptionIfExists(context);
            return Expression.Constant(DateTime.Now);
        }

        public override Expression VisitBetweenAndExpression(DataPrivilegeParser.BetweenAndExpressionContext context)
        {
            AddExceptionIfExists(context);
            bool isNot = context.ChildCount == 6;
            var children = context.expression();
            var left = VisitExpression(children[0]);
            var right = VisitExpression(children[1]);
            Convert(ref left, ref right);
            Expression expression = isNot ? Expression.LessThan(left, right) : Expression.GreaterThanOrEqual(left, right);
            right = VisitExpression(children[2]);
            Expression second = isNot ? Expression.GreaterThan(left, right) : Expression.LessThanOrEqual(left, right);
            expression = isNot ? Expression.OrElse(expression, second) : Expression.AndAlso(expression, second);
            return expression;
        }

        public override Expression VisitInExpression(DataPrivilegeParser.InExpressionContext context)
        {
            AddExceptionIfExists(context);
            bool isNot = context.ChildCount == 6;
            Expression item = VisitExpression(context.expression());
            Expression set = null;
            var expressionListContext = context.expressionList();
            MethodInfo containsMethod;
            if (expressionListContext != null)
            {
                set = VisitExpressionList(expressionListContext);
                containsMethod = typeof(Enumerable).GetMethods().FirstOrDefault(m => m.Name == "Contains" && m.GetParameters()?.Length == 2);
                containsMethod = containsMethod.MakeGenericMethod(item.Type);
                NewArrayExpression newArrayExpression = set as NewArrayExpression;
                List<Expression> list = new List<Expression>();
                List<Expression> setList = new List<Expression>();
                foreach (Expression itemExpression in newArrayExpression.Expressions)
                {
                    if (itemExpression.Type != item.Type)
                    {
                        if (typeof(IEnumerable<>).MakeGenericType(item.Type).IsAssignableFrom(itemExpression.Type))
                        {

                            setList.Add(itemExpression);
                        }
                        else
                        {
                            list.Add(Expression.Convert(itemExpression, item.Type));
                        }
                    }
                    else
                    {
                        list.Add(itemExpression);
                    }
                }
                if (list.Count > 0)
                {
                    setList.Add(Expression.NewArrayInit(item.Type, list));
                }
                Expression expression = Expression.Call(containsMethod, setList[0], item);
                Expression right = null;
                if (setList.Count > 1)
                {
                    for (int i = 1; i < setList.Count; i++)
                    {
                        right = Expression.Call(containsMethod, setList[i], item);
                        expression = Expression.OrElse(expression, right);
                    }
                }
                if (isNot)
                {
                    expression = Expression.Not(expression);
                }
                return expression;
            }
            else
            {
                set = VisitSimpleSubquery(context.simpleSubquery());
                containsMethod = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "Contains" && m.GetParameters()?.Length == 2);
                containsMethod = containsMethod.MakeGenericMethod(item.Type);
                Expression expression= Expression.Call(containsMethod, set, item);
                if (isNot)
                {
                    expression = Expression.Not(expression);
                }
                return expression;
            }
        }

        public override Expression VisitExpressionList(DataPrivilegeParser.ExpressionListContext context)
        {
            AddExceptionIfExists(context);
            var contexts = context.expression();
            List<Expression> expressions = new List<Expression>();
            foreach (var child in contexts)
            {
                expressions.Add(VisitExpression(child));
            }
            return Expression.NewArrayInit(typeof(object), expressions);
        }

        public override Expression VisitLikeExpression(DataPrivilegeParser.LikeExpressionContext context)
        {
            AddExceptionIfExists(context);
            bool isNot = context.ChildCount == 4;
            Expression expression=VisitExpression(context.expression());
            if(expression.Type!=typeof(string))
            {
                Exceptions.Add(new Exception(" like 表达式只能作用于字符串类型"));
                return Expression.Constant(true);
            }
            MethodInfo containsMethod = typeof(string).GetMethod("Contains",new Type[] {typeof(string)});
            expression= Expression.Call(expression, containsMethod, Expression.Constant(context.STRING()));
            if (isNot)
            {
                expression = Expression.Not(expression);
            }
            return expression;
        }

        public override Expression VisitIsNullExpression(DataPrivilegeParser.IsNullExpressionContext context)
        {
            AddExceptionIfExists(context);
            bool isNot = context.ChildCount == 4;
            Expression expression = VisitExpression(context.expression());
            expression = isNot ? Expression.NotEqual(expression, Expression.Constant(null)) : Expression.Equal(expression, Expression.Constant(null));
            return expression;
        }

        public override Expression VisitSimpleSubquery(DataPrivilegeParser.SimpleSubqueryContext context)
        {
            AddExceptionIfExists(context);
            var tableNameContext = context.tableName();
            Expression tableExpression = VisitTableName(tableNameContext);
            var columnElemContext= context.columnElem();
            var columnExpression= VisitColumnElem(columnElemContext);
            var searchConditionContext= context.searchCondition();
            Type entityType = tableExpression.Type.GetGenericArguments()[0];
            ParameterExpression parameter = _tableContainer[_tableContainer.Count - 1].Parameter;
            if (searchConditionContext!=null)
            {
                Type expressionType = typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType,typeof(bool)));
                MethodInfo whereMethod =typeof(Queryable).GetMethods().FirstOrDefault(m => {
                      if(m.Name!="Where")
                    {
                        return false;
                    }
                    var parameters= m.GetParameters();
                    if (parameters.Length != 2)
                    {
                        return false;
                    }
                    var generocTypes=  parameters[1].ParameterType.GenericTypeArguments;
                    if(generocTypes?.Length!=1)
                    {
                        return false;
                    }
                    generocTypes = generocTypes[0].GenericTypeArguments;
                    if (generocTypes?.Length != 1)
                    {
                        return false;
                    }
                    if(generocTypes[0].Name!= "Func`2")
                    {
                        return false;
                    }
                    return true;
                });
                whereMethod = whereMethod.MakeGenericMethod(entityType);
                Expression expression = VisitSearchCondition(searchConditionContext);
                Type delegateType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
                Expression predicateExpession= Expression.Lambda(delegateType,expression,parameter);
                tableExpression=Expression.Call(whereMethod, tableExpression, predicateExpession);
            }
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
                     generocTypes = generocTypes[0].GenericTypeArguments;
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
            Expression selectLambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(entityType, columnExpression.Type), columnExpression, parameter);
           Expression dataExpression=  Expression.Call(selectMethod, tableExpression, selectLambda);
            _tableContainer.RemoveAt(_tableContainer.Count - 1);
            return dataExpression;
        }

        public override Expression VisitTableName( DataPrivilegeParser.TableNameContext context)
        {
            AddExceptionIfExists(context);
            var tableNames= context.ID();
            string tableName = tableNames[0].GetText();
            string aliasName = tableNames.Length == 2 ? tableNames[1].GetText() : tableName;
            Expression expression= GetTableExpression(tableName);
            Type entityType= expression.Type.GenericTypeArguments[0];
            ParameterExpression parameter= Expression.Parameter(entityType, $"_p{_tableContainer.Count}");
            _tableContainer.Add((aliasName,tableName, parameter));
            return expression;
        }

        private string GetTableName(Type entityType)
        {
            return DbContext.Model.FindEntityType(entityType)?.GetTableName();
        }

        public VisitResult<TEntity> Visit(string conditionExpression)
        {
            Expression<Func<TEntity, bool>> predicate = null;
            IList<Exception> exceptions = null;
            try
            {
                ICharStream charStream = new AntlrInputStream(conditionExpression);
                DataPrivilegeLexer dataPrivilegeLexer = new DataPrivilegeLexer(charStream);
                DataPrivilegeErrotListener errorListener = new DataPrivilegeErrotListener();
                dataPrivilegeLexer.RemoveErrorListeners();
                dataPrivilegeLexer.AddErrorListener(errorListener);
                CommonTokenStream commonTokenStream = new CommonTokenStream(dataPrivilegeLexer);
                DataPrivilegeParser dataPrivilegeParser = new DataPrivilegeParser(commonTokenStream);
                dataPrivilegeParser.RemoveErrorListeners();
                dataPrivilegeParser.AddErrorListener(errorListener);
                IParseTree tree = dataPrivilegeParser.searchCondition();
                if (errorListener.Exceptions.Count != 0)
                {
                    Exceptions.AddRange(errorListener.Exceptions);
                }
                ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "_p");
                string tableName = GetTableName(typeof(TEntity));
                _tableContainer.Add((tableName, tableName, parameter));
                Expression expression = Visit(tree);
                predicate = Expression.Lambda<Func<TEntity, bool>>(expression, parameter);
            }
            catch (Exception ex)
            {
                Exceptions.Add(ex);
            }
            finally
            {
                exceptions = Exceptions.ToList();
                _tableContainer.Clear();
                Exceptions.Clear();
                CustomFields.Clear();
            }

            return new VisitResult<TEntity>(predicate, exceptions, CustomFields);
        }
        private class DataPrivilegeErrotListener : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
        {
            public List<Exception> Exceptions { get; } = new List<Exception>();
            public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                //Exceptions.Add(e);
                Exceptions.Add(new Exception($"[行:{line}，{charPositionInLine}] {offendingSymbol}:{msg}",e));
            }

            public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                Exceptions.Add(new Exception($"[行:{line}，{charPositionInLine}] {offendingSymbol}:{msg}",e));
            }
        }

    }
}
