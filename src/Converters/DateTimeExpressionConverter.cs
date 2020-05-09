using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DataPrivilege.Converters
{
    public class DateTimeExpressionConverter : ExpressionConvertBase
    {
        public override Type[] Types => new Type[] { typeof(DateTimeOffset?), typeof(DateTimeOffset), typeof(DateTime?),typeof(DateTime), typeof(string) };

        public override bool IsEffective(Expression left, Expression right)
        {
            if(base.IsEffective(left, right))
            {
                if(left.Type.IsGenericType&&right.Type.IsGenericType||!left.Type.IsGenericType && !right.Type.IsGenericType)
                {
                    return true;
                }
                if(LeftIndex>RightIndex)
                {
                    if(left.Type.IsGenericType)
                    {
                        return true;
                    } 
                }
                else
                {
                    if (right.Type.IsGenericType)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        private Expression ConvertExpression(Expression expression,Type type)
        {
            if (expression is ConstantExpression constant)
            {
                if (constant.Type == typeof(string))
                {
                    DateTime dateTime = DateTime.Parse(constant.Value.ToString());
                    if (type == typeof(DateTime))
                    {
                        return Expression.Constant(dateTime, typeof(DateTime));
                    }

                    if (type == typeof(DateTime?))
                    {
                        return Expression.Constant((DateTime?)dateTime, typeof(DateTime?));
                    }
                    if(type==typeof(DateTimeOffset))
                    {
                        return Expression.Constant((DateTimeOffset)dateTime, typeof(DateTimeOffset));
                    }
                    if (type == typeof(DateTimeOffset?))
                    {
                        return Expression.Constant((DateTimeOffset?)dateTime, typeof(DateTimeOffset?));
                    }
                }
            }
            return Expression.ConvertChecked(expression, type);
        }
        public override void Convert(ref Expression left, ref Expression right)
        {
            if (LeftIndex < RightIndex)
            {
                right = ConvertExpression(right, left.Type);
            }
            else
            {
                left = ConvertExpression(left, right.Type);
            }
        }
    }
}
