using DataPrivilege.Converters;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace DataPrivilegeTest
{
    public class BooleanExpressionConverterTests
    {
        [Fact]
        public void IsEffective_ReturnsFalse_WhenTypeIsNotIncludedInTheTypes()
        {
            BooleanExpressionConverter converter = new BooleanExpressionConverter();
            Expression left=Expression.Constant(true);
            Expression right=Expression.Constant(new byte[0],typeof(byte[]));
            converter.IsEffective(left, right).ShouldBeFalse();
             right = Expression.Constant(new string[0], typeof(string[]));
            converter.IsEffective(left, right).ShouldBeFalse();
            left = Expression.Constant(null,typeof(object));
            converter.IsEffective(left, right).ShouldBeFalse();
        }

        [Fact]
        public void IsEffective_ReturnsFalse_WhenBooleanTypeIsNotIncluded()
        {
           
            Dictionary<Type, object> datas = new Dictionary<Type, object>() { 
                { typeof(decimal) ,default(decimal)},{ typeof(long),default(long) },
                { typeof(int),default(int)},{typeof(short),default(short)},
                {typeof(byte),default(byte) },{typeof(ulong),default(ulong)},
                {typeof(uint),default(uint) },{typeof(ushort),default(ushort) },
                {typeof(string),"TRUE" }
                                                                              };
            BooleanExpressionConverter converter = new BooleanExpressionConverter();
            Expression left;
            Expression right;
            for (int i=0;i<datas.Count;i++)
            {
                for (int j = i; j < datas.Count; j++)
                {

                    left = Expression.Constant(datas.ElementAt(i).Value, datas.ElementAt(i).Key);
                    right = Expression.Constant(datas.ElementAt(j).Value, datas.ElementAt(j).Key);
                    converter.IsEffective(left,right).ShouldBeFalse();
                }
            }
        }

        [Fact]
        public void IsEffective_ReturnsTrue_WhenBooleanTypeIsIncluded()
        {
            Dictionary<Type, object> datas = new Dictionary<Type, object>() 
            {
                { typeof(decimal) ,default(decimal)},{ typeof(long),default(long) },
                { typeof(int),default(int)},{typeof(short),default(short)},
                {typeof(byte),default(byte) },{typeof(ulong),default(ulong)},
                {typeof(uint),default(uint) },{typeof(ushort),default(ushort) },
                {typeof(string),"TRUE" }
            };
            BooleanExpressionConverter converter = new BooleanExpressionConverter();
            Expression left=Expression.Constant(true,typeof(bool));
            Expression right;
            for (int i = 0; i < datas.Count; i++)
            {
                    right = Expression.Constant(datas.ElementAt(i).Value, datas.ElementAt(i).Key);
                    converter.IsEffective(left, right).ShouldBeTrue();
                
            }
        }

        [Fact]
        private void ConvertTest()
        {
            Dictionary<Type, object> datas = new Dictionary<Type, object>()
            {
                { typeof(decimal) ,default(decimal)},{ typeof(long),default(long) },
                { typeof(int),default(int)},{typeof(short),default(short)},
                {typeof(byte),default(byte) },{typeof(ulong),default(ulong)},
                {typeof(uint),default(uint) },{typeof(ushort),default(ushort) },
                {typeof(string),"FALSE" }
            };

            BooleanExpressionConverter converter = new BooleanExpressionConverter();
            Expression left = Expression.Constant(true, typeof(bool));
            Expression right;
            for (int i = 0; i < datas.Count; i++)
            {
                right = Expression.Constant(datas.ElementAt(i).Value, datas.ElementAt(i).Key);
                converter.IsEffective(left, right).ShouldBeTrue();
                converter.Convert(ref left,ref right);
                right.Type.ShouldBe(typeof(bool));
            }
        }


    }
}
