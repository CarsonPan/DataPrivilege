using DataPrivilege.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataPrivilege
{
    public interface IDataPrivilegeManager<TEntity>
        where TEntity:class
    {
        IEnumerable<TEntity> Filter(IEnumerable<TEntity> entities);
        IQueryable<TEntity> Filter(IQueryable<TEntity> entities);
        IQueryable<TEntity> GetAll();
        void CheckPermission(IEnumerable<TEntity> entities, DataOperation dataOperation);
        void CheckPermission(TEntity entity, DataOperation dataOperation);
    }
}
