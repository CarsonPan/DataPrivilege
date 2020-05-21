using DataPrivilege.Converters;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace DataPrivilegeTest.Converters
{
    public class NumericExpressionConverterTests
    {
        [Fact]
        public void ConvertTest()
        {
            Type[] types = new Type[] { typeof(decimal?), typeof(decimal), typeof(float?), typeof(float), typeof(int?), typeof(int), typeof(short?), typeof(short), typeof(byte?), typeof(byte) };
            Expression left;
            Expression right;
            NumericExpressionConverter converter = new NumericExpressionConverter();
            for (int i = 0; i < types.Length; i++)
            {
                
                for (int j = i + 1; j < types.Length; j++)
                {
                    left = Expression.ConvertChecked( Expression.Constant(0), types[i]);
                    right = Expression.ConvertChecked(Expression.Constant(0), types[j]);
                    if (left.Type.IsGenericType)
                    {
                        converter.IsEffective(left, right).ShouldBeTrue();
                        converter.Convert(ref left, ref right);
                        right.Type.ShouldBe(left.Type);
                    }
                    else
                    {
                        if (right.Type.IsGenericType)
                        {
                            converter.IsEffective(left, right).ShouldBeFalse();
                        }
                        else
                        {
                            converter.IsEffective(left, right).ShouldBeTrue();
                            converter.Convert(ref left, ref right);
                            right.Type.ShouldBe(left.Type);
                        }
                    }
                }
            }
        }
    }
}
