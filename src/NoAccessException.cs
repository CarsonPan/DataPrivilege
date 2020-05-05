using DataPrivilege.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataPrivilege
{
    public class NoAccessException : Exception
    {
        public object Datas { get; }
        public object User { get; }

        public DataOperation DataOperation { get; }

        public NoAccessException(object data, DataOperation dataOperation)
        {
            Datas = data;
            DataOperation = dataOperation;
        }

        public NoAccessException(object data, DataOperation dataOperation, string message)
            : base(message)
        {
            Datas = data;
            DataOperation = dataOperation;
        }


        public NoAccessException(object data, DataOperation dataOperation,object user, string message)
            :this(data,dataOperation,message)
        {
            this.User = user;
        }
    }
}
