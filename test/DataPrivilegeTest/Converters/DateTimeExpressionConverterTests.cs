using DataPrivilege.Converters;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace DataPrivilegeTest.Converters
{
    public class DateTimeExpressionConverterTests
    {
        [Fact]
        public void IsEffectiveTest()
        {
            Dictionary<Type, object> dic = new Dictionary<Type, object>() 
            {
                {typeof(DateTimeOffset?),null },{typeof(DateTimeOffset),default(DateTimeOffset)},
                {typeof(DateTime?),null },{typeof(DateTime),default(DateTime)},
                {typeof(string),"2020-5-9" }
            };
            Expression left;
            Expression right;
            DateTimeExpressionConverter converter = new DateTimeExpressionConverter();
            left = Expression.Constant(default(DateTimeOffset), typeof(DateTimeOffset));
            right = Expression.Constant(null, typeof(DateTime?));
            converter.IsEffective(left, right).ShouldBeFalse();

            left = Expression.Constant(DateTime.Now.ToShortDateString(), typeof(string));
            for(int i=0;i<dic.Count-1;i++)
            {
                right = Expression.Constant(dic.ElementAt(i).Value, dic.ElementAt(i).Key);
                converter.IsEffective(left, right).ShouldBeTrue();
            }

            left = Expression.Constant(DateTime.Now, typeof(DateTime));
            for (int i = 0; i < dic.Count - 2; i++)
            {
                right = Expression.Constant(dic.ElementAt(i).Value, dic.ElementAt(i).Key);
                converter.IsEffective(left, right).ShouldBeTrue();
            }

            left = Expression.Constant(null, typeof(DateTime?));
            right = Expression.Constant(null, typeof(DateTimeOffset?));
            converter.IsEffective(left, right).ShouldBeTrue();

            left = Expression.Constant(default(DateTimeOffset), typeof(DateTimeOffset));
            right = Expression.Constant(null, typeof(DateTimeOffset?));
            converter.IsEffective(left, right).ShouldBeTrue();
        }

        [Fact]
        public void ConvertTest()
        {
            Dictionary<Type, object> dic = new Dictionary<Type, object>()
            {
                {typeof(DateTimeOffset?),null },{typeof(DateTimeOffset),default(DateTimeOffset)},
                {typeof(DateTime?),null },{typeof(DateTime),default(DateTime)},
                {typeof(string),"2020-5-9" }
            };
            Expression left;
            Expression right;
            DateTimeExpressionConverter converter = new DateTimeExpressionConverter();
            for (int i = 0; i < dic.Count - 1; i++)
            {
                left = Expression.Constant("2020-5-9", typeof(string));
                right = Expression.Constant(dic.ElementAt(i).Value, dic.ElementAt(i).Key);
                converter.IsEffective(left, right).ShouldBeTrue();
                converter.Convert(ref left,ref right);
                left.Type.ShouldBe(dic.ElementAt(i).Key);
            }

            
            for (int i = 0; i < dic.Count - 2; i++)
            {
                left = Expression.Constant(DateTime.Now, typeof(DateTime));
                right = Expression.Constant(dic.ElementAt(i).Value, dic.ElementAt(i).Key);
                converter.IsEffective(left, right).ShouldBeTrue();
                converter.Convert(ref left, ref right);
                left.Type.ShouldBe(dic.ElementAt(i).Key);
            }


            left = Expression.Constant(null, typeof(DateTime?));
            right = Expression.Constant(null, typeof(DateTimeOffset?));
            converter.IsEffective(left, right).ShouldBeTrue();
            converter.Convert(ref left, ref right);
            left.Type.ShouldBe(typeof(DateTimeOffset?));
            left = Expression.Constant(default(DateTimeOffset), typeof(DateTimeOffset));
            right = Expression.Constant(null, typeof(DateTimeOffset?));
            converter.IsEffective(left, right).ShouldBeTrue();
            converter.Convert(ref left, ref right);
            left.Type.ShouldBe(typeof(DateTimeOffset?));
        }
    }
}
