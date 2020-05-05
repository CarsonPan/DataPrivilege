using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DataPrivilege.Converters
{
    public abstract class ExpressionConvertBase : IExpressionConvert
    {
        public abstract Type[] Types { get; }

        public virtual void Convert(ref Expression left, ref Expression right)
        {
            if(LeftIndex<RightIndex)
            {
                right = Expression.Convert(right, left.Type);
            }
            else
            {
                left = Expression.Convert(left, right.Type);
            }
        }

        public virtual bool IsEffective(Expression left, Expression right)
        {
            Type leftType = left.Type;
            Type rightType = right.Type;
            for(int i=0;i<Types.Length;i++)
            {
                if(Types[i]==leftType)
                {
                    LeftIndex = i;
                }
                if(Types[i]==rightType)
                {
                    RightIndex = i;
                }    
            }
            return LeftIndex > -1 && RightIndex > -1;
        }
        protected int LeftIndex { get; set; } = -1;
        protected int RightIndex { get; set; } = -1;

        public virtual int Priority => 0;
    }
}
