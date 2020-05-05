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
    }
}
