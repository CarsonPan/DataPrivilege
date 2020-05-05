using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DataPrivilege.Converters
{
    public interface IExpressionConvert
    {
        
        void Convert(ref Expression left,ref Expression right);

        bool IsEffective(Expression left, Expression right);

        int Priority { get; }
    }
}
