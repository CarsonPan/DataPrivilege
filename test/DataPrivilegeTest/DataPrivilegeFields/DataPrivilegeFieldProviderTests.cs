using DataPrivilege.DataPrivilegeFields;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;

namespace DataPrivilegeTest.DataPrivilegeFields
{
    public class DataPrivilegeFieldProviderTests
    {
        public DataPrivilegeFieldProviderTests()
        {
            
        }

        [Fact]
        public void GetFieldTypeTest()
        {
            ServiceCollection services = new ServiceCollection();
            string fileFullName = typeof(DataPrivilegeFieldManagerTests).Assembly.Location;
            string path = fileFullName.Substring(0, fileFullName.LastIndexOf("\\"));
            services.LoadDataPrivilegeField(path);
            var serviceProvider = services.BuildServiceProvider();
            DataPrivilegeFieldProvider provider = new DataPrivilegeFieldProvider(serviceProvider);
            provider.GetFieldType("TestField0").ShouldBe(typeof(string));
            provider.GetFieldType("TestField1").ShouldBe(typeof(int));
        }
        [Fact]
        public void GetFieldValueTest()
        {
            ServiceCollection services = new ServiceCollection();
            string fileFullName = typeof(DataPrivilegeFieldManagerTests).Assembly.Location;
            string path = fileFullName.Substring(0, fileFullName.LastIndexOf("\\"));
            services.LoadDataPrivilegeField(path);
            var serviceProvider = services.BuildServiceProvider();
            DataPrivilegeFieldProvider provider = new DataPrivilegeFieldProvider(serviceProvider);
            provider.GetFieldValue("TestField0").ShouldBe("hello world");
            provider.GetFieldValue("TestField1").ShouldBe(1);
        }
    }
}
