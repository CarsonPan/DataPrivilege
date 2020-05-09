using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using DataPrivilege.DataPrivilegeFields;
using DataPrivilege.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataPrivilege
{
    public class DataPrivilegeContext<TDbContext, TEntity,TRule>
        where TDbContext : DbContext
        where TEntity : class
        where TRule:DataPrivilegeRule
    {
        private static readonly ParameterExpression _parameterExpression = Expression.Parameter(typeof(TEntity), "_p");
        private static readonly ConcurrentDictionary<string, DataPrivilegeInfo<TEntity>> _cache = new ConcurrentDictionary<string, DataPrivilegeInfo<TEntity>>();

        public IList<Exception> Vertify(TRule rule)
        {
            if(_cache.TryGetValue(rule.ConditionExpression.ToUpper(),out DataPrivilegeInfo<TEntity> value))
            {
                return null;
            }

            var visitResult = DataPrivilegeVisitor.Visit(Rules[0].ConditionExpression);
            if(!visitResult.Success)
            {
                return visitResult.Exceptions;
            }
            ReplacementVisitor visitor = new ReplacementVisitor(Expression.Constant(this), visitResult.PredicateExpression.Parameters[0], _parameterExpression);
            var newExpression = visitor.Visit(visitResult.PredicateExpression);
            DataPrivilegeInfo<TEntity> result = new DataPrivilegeInfo<TEntity>(newExpression as Expression<Func<TEntity, bool>>, visitResult.CustomFields);
            if (!_cache.ContainsKey(rule.ConditionExpression.ToUpper()))
            {
                lock(_cache)
                {
                    _cache.TryAdd(rule.ConditionExpression.ToUpper(), result);
                }
            }
            return null;
        }
        private readonly DataPrivilegeVisitor<TDbContext, TEntity> DataPrivilegeVisitor;
        public List<TRule> Rules { get; }
        public TDbContext DbContext { get; }

        protected readonly IDataPrivilegeFieldProvider DataPrivilegeFieldProvider;
        public DataPrivilegeContext(List<TRule> rules,
                                    TDbContext dbContext,
                                    IDataPrivilegeFieldProvider dataPrivilegeFieldProvider,
                                    DataPrivilegeVisitor<TDbContext, TEntity> dataPrivilegeVisitor)
        {
            
            Rules = rules;
            var comparer = new CommonComparer<DataPrivilegeRule>((x, y) => {
                int left = x.ConditionExpression.GetHashCode();
                int right = y.ConditionExpression.GetHashCode();
                return left.CompareTo(right);
            });
            Rules.Sort(comparer);
            DbContext = dbContext;
            DataPrivilegeFieldProvider = dataPrivilegeFieldProvider;
            DataPrivilegeVisitor = dataPrivilegeVisitor;
        }

        private string GetCacheKey()
        {
            if (Rules.Count == 1)
            {
                return Rules[0].ConditionExpression.ToUpper();
            }
           
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Rules.Count; i++)
            {
                builder.Append("(").Append(Rules[i].ConditionExpression).Append(")");
                if (i != Rules.Count - 1)
                {
                    builder.Append(" OR ");
                }
            }
            return builder.ToString().ToUpper();
        }

        public bool CheckAndReduceRule(DataPrivilegeRule rule,out IList<Exception> exceptions)
        {
            string cacheKey = rule.ConditionExpression.ToUpper();
            if(_cache.ContainsKey(cacheKey))
            {
                exceptions = null;
                return true;
            }
            var visitResult = DataPrivilegeVisitor.Visit(rule.ConditionExpression);
            exceptions = visitResult.Exceptions;
            if (visitResult.Success)
            {
                ReplacementVisitor visitor = new ReplacementVisitor(Expression.Constant(this), visitResult.PredicateExpression.Parameters[0], _parameterExpression);
                var newExpression = visitor.Visit(visitResult.PredicateExpression);
                if(newExpression.CanReduce)
                {
                    newExpression = newExpression.Reduce();
                }
                DataPrivilegeInfo<TEntity> result = new DataPrivilegeInfo<TEntity>(newExpression as Expression<Func<TEntity, bool>>, visitResult.CustomFields);
               
                _cache.TryAdd(rule.ConditionExpression.ToUpper(), result); 
                  
            }
            return visitResult.Success;
        }
        private DataPrivilegeInfo<TEntity> GetDataPrevilegeInfoByRule(DataPrivilegeRule rule)
        {
            return _cache.GetOrAdd(rule.ConditionExpression.ToUpper(), _ =>
            {
                var visitResult = DataPrivilegeVisitor.Visit(rule.ConditionExpression);
                if (!visitResult.Success)
                {
                    throw new AggregateException(visitResult.Exceptions);
                }
                ReplacementVisitor visitor = new ReplacementVisitor(Expression.Constant(this), visitResult.PredicateExpression.Parameters[0], _parameterExpression);
                var newExpression = visitor.Visit(visitResult.PredicateExpression);
                DataPrivilegeInfo<TEntity> result = new DataPrivilegeInfo<TEntity>(newExpression as Expression<Func<TEntity, bool>>, visitResult.CustomFields);
                return result;
            });
        }
        private IDictionary<string, object> _parameters=null;
        protected IDictionary<string, object> Parameters
        {
            get {
                if(_parameters==null)
                {
                    _parameters = new Dictionary<string, object>();
                    SetParameters();
                }
                return _parameters;
            }
        }
        private DataPrivilegeInfo<TEntity> _dataPrevilegeInfo = null;
        public DataPrivilegeInfo<TEntity> DataPrivilegeInfo
        {
            get
            {
                if (_dataPrevilegeInfo == null)
                {
                    _dataPrevilegeInfo = GetDataPrevilegeInfo();
                }
                return _dataPrevilegeInfo;
            }
        }
        private DataPrivilegeInfo<TEntity> GetDataPrevilegeInfo()
        {
            if (Rules.Count == 1)
            {
                return GetDataPrevilegeInfoByRule(Rules[0]);
            }
            string key = GetCacheKey();
            return _cache.GetOrAdd(key, _ =>
            {

                List<DataPrivilegeInfo<TEntity>> list = new List<DataPrivilegeInfo<TEntity>>(Rules.Count);
                foreach (DataPrivilegeRule rule in Rules)
                {
                    list.Add(GetDataPrevilegeInfoByRule(rule));
                }
                Expression expression = null;
                List<string> customFields = new List<string>();
                foreach (var item in list)
                {
                    if (expression == null)
                    {
                        expression = item.PredicateExpression.Body;
                    }
                    else
                    {
                        expression = Expression.OrElse(expression, item.PredicateExpression.Body);
                    }
                    foreach (string field in item.CustomFields)
                    {
                        if (!customFields.Contains(field))
                        {
                            customFields.Add(field);
                        }
                    }
                }
                var predicate = Expression.Lambda<Func<TEntity, bool>>(expression, _parameterExpression);
                DataPrivilegeInfo<TEntity> dataPrevilegeInfo = new DataPrivilegeInfo<TEntity>(predicate, customFields);

                return dataPrevilegeInfo;
            });
        }


        private void SetParameters(string fieldName)
        {
            object value = DataPrivilegeFieldProvider.GetFieldValue(fieldName);
            Parameters.Add(fieldName, value);
        }
        private void SetParameters()
        {    
            foreach (string fieldName in DataPrivilegeInfo.CustomFields)
            {
                SetParameters(fieldName);
            }
        }

         
        public IQueryable<TEntity> Filter(IQueryable<TEntity> entities)
        {
            return entities.Where(DataPrivilegeInfo.PredicateExpression);
        }

        public IEnumerable<TEntity> Filter(IEnumerable<TEntity> entities)
        {
            return entities.Where(DataPrivilegeInfo.PredicateDelegate);
        }

        public bool CheckPermission(TEntity entity)
        {
            return DataPrivilegeInfo.PredicateDelegate(entity);
        }

        public bool CheckPermission(IQueryable<TEntity> entities)
        {
            return entities.All(DataPrivilegeInfo.PredicateExpression);
        }

        public bool CheckPermission(IEnumerable<TEntity> entities)
        {
            return entities.All(DataPrivilegeInfo.PredicateDelegate);
        }


        public IEnumerable<TEntity> GetNoAccessData(IEnumerable<TEntity> entities)
        {
            List<TEntity> list = new List<TEntity>();
            foreach(TEntity entity in entities)
            {
                if(!DataPrivilegeInfo.PredicateDelegate(entity))
                {
                    list.Add(entity);
                }
            }
            return list;
        }


        private class ReplacementVisitor : ExpressionVisitor
        {
            public ParameterExpression Source { get; }

            public ParameterExpression Destination { get; }
            public ReplacementVisitor(Expression instance, ParameterExpression source, ParameterExpression destination)
            {
                Instance = instance;
                Source = source;
                Destination = destination;
            }
            public Expression Instance { get; }
            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Member == typeof(DataPrivilegeVisitor<TDbContext, TEntity>).GetProperty("Parameters"))
                {
                    return Expression.PropertyOrField(Instance, "Parameters");
                }
                else
                    if (node.Member == typeof(DataPrivilegeVisitor<TDbContext, TEntity>).GetProperty("DbContext"))
                {
                    return Expression.PropertyOrField(Instance, "DbContext");
                }
                return base.VisitMember(node);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Name == Source.Name && node.Type == Source.Type)
                {
                    return Destination;
                }
                return base.VisitParameter(node);
            }
        }

    }
}
