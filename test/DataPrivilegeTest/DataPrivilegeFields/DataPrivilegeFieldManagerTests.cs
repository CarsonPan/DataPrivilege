using DataPrivilege.DataPrivilegeFields;
using DataPrivilege.DataPrivilegeFields.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace DataPrivilegeTest.DataPrivilegeFields
{
    public class DataPrivilegeFieldManagerTests
    {
        
        [Fact]
        public void LoadDataPrivilegeFieldTest()
        {
            ServiceCollection services = new ServiceCollection();
            services.LoadDataPrivilegeField();
            services.ShouldBeEmpty();

            string fileFullName = typeof(DataPrivilegeFieldManagerTests).Assembly.Location;
            string path = fileFullName.Substring(0, fileFullName.LastIndexOf("\\"));
            services.LoadDataPrivilegeField(path);
            services.Count.ShouldBe(2+1);//多余一个为DataPrivilegeFieldTypeContainer
            services.ShouldContain(sd => sd.ServiceType == typeof(TestField0) 
                                      && sd.ImplementationType == typeof(TestField0) 
                                      && sd.Lifetime == ServiceLifetime.Scoped);
            services.ShouldContain(sd => sd.ServiceType == typeof(TestField1) 
                                      && sd.ImplementationType == typeof(TestField1) 
                                      && sd.Lifetime == ServiceLifetime.Singleton);
            Exception ex = Should.Throw<Exception>(() => services.LoadDataPrivilegeField(path));
            ex.Message.ShouldBe("已经存在同名自定义字段：TestField0!");
        }

        [Fact]
        public void GetField_Success_WhenExistsFieldName()
        {
            ServiceCollection services = new ServiceCollection();
            string fileFullName = typeof(DataPrivilegeFieldManagerTests).Assembly.Location;
            string path = fileFullName.Substring(0, fileFullName.LastIndexOf("\\"));
            services.LoadDataPrivilegeField(path);
            IServiceProvider sp = services.BuildServiceProvider();
            using var scope= sp.CreateScope();
            var field= scope.ServiceProvider.GetField("TestField0");
            field.ShouldNotBeNull();
            field.ShouldBeAssignableTo<TestField0>();

            field = scope.ServiceProvider.GetField("TestField1");
            field.ShouldNotBeNull();
            field.ShouldBeAssignableTo<TestField1>();
        }

        [Fact]
        public void GetField_Throw_WhenNotExistsFieldName()
        {
            ServiceCollection services = new ServiceCollection();
            string fileFullName = typeof(DataPrivilegeFieldManagerTests).Assembly.Location;
            string path = fileFullName.Substring(0, fileFullName.LastIndexOf("\\"));
            services.LoadDataPrivilegeField(path);
            IServiceProvider sp = services.BuildServiceProvider();
            Exception ex= Should.Throw<Exception>(()=>sp.GetField("xxxx"));
            ex.Message.ShouldBe("字段xxxx不存在！");
        }
    }
    [DataPrivilegeField("TestField0")]
    public class TestField0 : DataPrivilegeFieldBase<string>
    {
        protected override string GetValueCore()
        {
            return "hello world";
        }
    }

    [DataPrivilegeField("TestField1",ServiceLifetime =ServiceLifetime.Singleton)]
    public class TestField1 : DataPrivilegeFieldBase<int>
    {
        protected override int GetValueCore()
        {
            return 1;
        }
    }
}
