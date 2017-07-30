
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace CoWorker.Infrastructure.DefaultFactory
{

    public class DefaultFactory
    {
        IDictionary<(string name, Type type), object> map = new Dictionary<(string, Type), object>();

        public void Init<T>(Func<IQueryable<T>, IQueryable<T>> query) where T : new()
            => Init(string.Empty, query);
        public void Init<T>(string name, Func<IQueryable<T>, IQueryable<T>> query) where T : new()
            => map.Add((name, typeof(T)), query(new DefaultQueryable<T>()).First());

        public T Get<T>(string name) where T : new()
            => map.FirstOrDefault(
                x => x.Key.name == (name ?? string.Empty) && x.Key.type == typeof(T))
                .Value is T t
                    ? t
                    : GetAutoInit<T>(name);

        private T GetAutoInit<T>(string name) where T : new()
        {
            Init<T>(name,x => InitAllProperties(x,name));
            return Get<T>(name);
        }

        private IQueryable<T> InitAllProperties<T>(IQueryable<T> query,string name) where T : new()
        {
            var param = typeof(T).ToParameter();
            return typeof(T).GetProperties().Select(
                    x => Expression.Lambda<Func<T,bool>>(InitProperty(Expression.Property(param, x.Name), name), param))
                .Aggregate(query,(seed,next) => seed.Where(next));
        }

        private Expression InitProperty(MemberExpression target,string name)
            => target.Type.IsValueType
                ? target.EqualWith(Expression.Default(target.Type))
                : target.Type == typeof(string)
                    ? target.EqualWith(Expression.Constant(String.Empty))
                    : target.EqualWith(Expression.Call(this.ToConstant(), "Get", new Type[] { target.Type },name.ToConstant()));

        public T Get<T>() where T : new()
            => Get<T>(string.Empty);
    }
}
