using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DataPrivilege
{
    public class VisitResult<TEntity>
    {
        public Expression<Func<TEntity, bool>> PredicateExpression { get; private set; }
        public bool Success => Exceptions == null || Exceptions.Count == 0;

        public IList<Exception> Exceptions { get; }

        public IList<string> CustomFields { get; }
        public VisitResult(Expression<Func<TEntity, bool>> predicateExpression, IList<Exception> exceptions, IList<string> customFields)
        {
            PredicateExpression = predicateExpression;
            Exceptions = exceptions;
            CustomFields = customFields;
        }
    }
    public class DataPrivilegeInfo<TEntity>
    {
        public Expression<Func<TEntity, bool>> PredicateExpression { get; private set; }

        private Func<TEntity, bool> _predicateDelegate;
        public Func<TEntity, bool> PredicateDelegate
        {
            get
            {
                if (_predicateDelegate == null)
                {
                    _predicateDelegate = PredicateExpression.Compile();
                }
                return _predicateDelegate;
            }
        }

       

        public IList<string> CustomFields { get; }

        public DataPrivilegeInfo(Expression<Func<TEntity, bool>> predicateExpression, IList<string> customFields)
        {
            PredicateExpression = predicateExpression;
            CustomFields = customFields;
        }
        
    }
}
