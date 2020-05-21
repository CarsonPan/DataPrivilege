using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DataPrivilege.Converters
{
    public class ExpressionConverter
    {
        protected readonly IEnumerable<IExpressionConvert> Converters;
        public ExpressionConverter(IEnumerable<IExpressionConvert> converters)
        {
            Converters = converters.Sort((x,y) => x.Priority.CompareTo(y.Priority));
        }
        public void Convert(ref Expression left, ref Expression right)
        {
            foreach(IExpressionConvert converter in Converters)
            {
                if(converter.IsEffective(left,right))
                {
                    converter.Convert(ref left,ref right);
                    break;
                }
            }
        }
    }
}
