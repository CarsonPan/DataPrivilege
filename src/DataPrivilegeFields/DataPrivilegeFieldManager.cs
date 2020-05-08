using DataPrivilege.DataPrivilegeFields.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DataPrivilege.DataPrivilegeFields
{
    public static class DataPrivilegeFieldManager
    {
        private static readonly IDictionary<string, Type> _fieldServices = new Dictionary<string, Type>();

        /// <summary>
        /// 获取字段服务实例
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="fieldName">字段名</param>
        /// <returns></returns>
        public static IDataPrivilegeField GetField(this IServiceProvider serviceProvider, string fieldName)
        {
            if(_fieldServices.TryGetValue(fieldName,out Type type))
            {
                return serviceProvider.GetService(type) as IDataPrivilegeField;
            }
            throw new Exception($"字段{fieldName}不存在！");
        }

        /// <summary>
        /// 加载字段插件
        /// </summary>
        /// <param name="services"></param>
        /// <param name="pluginPath"></param>
        public static void LoadDataPrivilegeField(this IServiceCollection services, string pluginPath=null)
        {
            LoadDataPrivilegeFieldPlugin(services, typeof(DataPrivilegeFieldManager).Assembly);
            if (string.IsNullOrWhiteSpace(pluginPath)||!Directory.Exists(pluginPath))
            {
                return;
            }
            var files = Directory.GetFiles(pluginPath, "*.dll");
            if (files == null || files.Length == 0)
            {
                return;
            }
            Assembly assembly;
            foreach (string assemblyFile in files)
            {
                assembly = Assembly.LoadFrom(assemblyFile);
                LoadDataPrivilegeFieldPlugin(services,  assembly);
            }

        }

        private static void LoadDataPrivilegeFieldPlugin(IServiceCollection services, Assembly assembly)
        {
            
            IEnumerable<Type> types;
            DataPrivilegeFieldAttribute attribute;
            types = assembly.GetExportedTypes().Where(t => t.IsClass&&!t.IsAbstract&& typeof(IDataPrivilegeField).IsAssignableFrom(t) && t.IsDefined(typeof(DataPrivilegeFieldAttribute)));
            if (types.Any())
            {
                foreach (Type type in types)
                {

                    attribute = type.GetCustomAttribute<DataPrivilegeFieldAttribute>();
                    if (!_fieldServices.ContainsKey(attribute.FieldName))
                    {
                        services.Add(new ServiceDescriptor(type, type, attribute.ServiceLifetime));
                        _fieldServices.Add(attribute.FieldName, type);
                    }
                    else
                    {
                        throw new Exception($"已经存在同名自定义字段：{attribute.FieldName}!");
                    }
                }
            }
        }
    }
}
