using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DataPrivilege.Converters
{
   public class BooleanExpressionConverter : ExpressionConvertBase
    {
        public override Type[] Types => new Type[] { typeof(bool),typeof(decimal),typeof(long),typeof(int),typeof(short),typeof(byte),
                                                                                  typeof(ulong),typeof(uint),typeof(ushort),typeof(string) };

        private Expression ConvertToBoolean(Expression exp)
        {
            if (exp is ConstantExpression constantExpression)
            {
                if (constantExpression.Type == typeof(string))
                {
                    string value = constantExpression.Value.ToString().ToUpper();
                    return Expression.Constant(value == "TURE");
                }
                else
                {
                    return Expression.Constant(constantExpression.Value.Equals(1));
                }
            }
            else//目前不支持
            {
                if (exp.Type == typeof(string))
                {
                    MethodInfo methodInfo = typeof(string).GetMethod("ToUpper", new Type[0]);
                    return Expression.Equal(Expression.Call(exp,methodInfo), Expression.Constant("TRUE"));
                }
                return Expression.Equal(exp, Expression.ConvertChecked(Expression.Constant(1), exp.Type));
            }
        }
        public override void Convert(ref Expression left, ref Expression right)
        {
            if (LeftIndex == 0)
            {
                right = ConvertToBoolean(right);
            }
            else
            {
                left = ConvertToBoolean(left);
            }
        }

        public override bool IsEffective(Expression left, Expression right)
        {
            Type leftType = left.Type;
            Type rightType = right.Type;
            for (int i = 0; i < Types.Length; i++)
            {
                if (Types[i] == leftType)
                {
                    LeftIndex = i;
                }
                if (Types[i] == rightType)
                {
                    RightIndex = i;
                }
            }
            return LeftIndex > -1 && RightIndex > -1&&(LeftIndex==0||RightIndex==0);
        }
    }
}
