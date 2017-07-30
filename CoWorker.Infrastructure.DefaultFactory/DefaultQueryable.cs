
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace CoWorker.Infrastructure.DefaultFactory
{
    public class DefaultQueryable<T> : IQueryable<T> where T : new()
    {
        private IQueryable<T> queryengine;
        private IEnumerable<T> _ts;
        public DefaultQueryable()
        {
            _ts = Enumerable.Empty<T>().Append(new T());
            queryengine = new EnumerableQuery<T>(_ts);
        }

        internal DefaultQueryable(IQueryable<T> query)
        {
            this.queryengine = query;
        }

        public Type ElementType => this.queryengine.ElementType;

        public Expression Expression => this.queryengine.Expression;

        public IQueryProvider Provider => new DefaultQueryProvider(this.queryengine.Provider,q => new DefaultQueryable<T>(q as IQueryable<T>));

        public IEnumerator<T> GetEnumerator() => this.queryengine.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.queryengine.GetEnumerator();
    }
}
