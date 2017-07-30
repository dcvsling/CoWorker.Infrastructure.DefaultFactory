using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace CoWorker.Infrastructure.DefaultFactory
{

    public class DefaultQueryProvider : IQueryProvider
    {
        private readonly IQueryProvider _provider;
        private readonly Func<IQueryable,IQueryable> _ctor;
        private object _config;
        internal DefaultQueryProvider(IQueryProvider provider,Func<IQueryable,IQueryable> ctor)
        {
            _provider = provider;
            _ctor = ctor;
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
            => _ctor(this.CreateQuery(expression, _provider.CreateQuery));
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
            => _ctor(this.CreateQuery(expression,_provider.CreateQuery<TElement>)) as IQueryable<TElement>;
        Object IQueryProvider.Execute(Expression expression)
            => this.Execute(expression);
        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            this._config = this._config ?? this.Execute<TResult>(expression);
            var result = Activator.CreateInstance<TResult>();
            (this._config is Action<TResult> config ? config : x => { })(result);
            return result;
        }
        public virtual IQueryable CreateQuery(Expression expressions, Func<Expression, IQueryable> next)
            => next(expressions);
        public virtual IQueryable<TElement> CreateQuery<TElement>(Expression expressions,Func<Expression,IQueryable<TElement>> next)
            => next(expressions);
        public Action<object> Execute(Expression expression)
        {
            var exp = EmptyVisitor(expression,expression.Type);
            return (exp is Expression<Action<object>> getter ? getter : Expression.Lambda<Action<object>>(exp)).Compile();
        }
        public Action<TResult> Execute<TResult>(Expression expression)
        {
            var exp = EmptyVisitor(expression,typeof(TResult));
            var action = (exp is Expression<Action<object>> getter ? getter : Expression.Lambda<Action<object>>(exp)).Compile();
            return x => action(x);
        }
        private Expression<Action<object>> EmptyVisitor(Expression exp,Type type)
        {
            IList<BinaryExpression> exps = new List<BinaryExpression>();
            exp.Visit(v =>
              {
                  v.Visit = e => ExpressionType.Equal == e.NodeType 
                    ? v.VisitBinary(e as BinaryExpression) 
                    : ExpressionType.Call == e.NodeType
                        ? v.VisitMethodCall(e as MethodCallExpression)
                        : e;
                  v.VisitBinary = eq
                  =>
                  {
                      if (ExpressionType.Equal == eq.NodeType && ExpressionType.MemberAccess == eq.Left.NodeType)
                          exps.Add(eq);
                      return eq;
                  };
                  v.VisitMethodCall = c =>
                  {
                      if (c.Method.Name == nameof(Equals))
                      {
                          exps.Add(Expression.MakeBinary(ExpressionType.Assign,c.Object, c.Arguments.FirstOrDefault()));
                      }
                      return c;
                  };
              });
            var param = Expression.Parameter(typeof(object));
            var astype = param.AsTypeTo(type);
            var block = exps.Select(next=>
                {
                    return Expression.Assign(
                        Expression.Property(astype, (next.Left as MemberExpression).Member.Name),
                        next.Right) as Expression;
                }).GroupWithBlock();
            return Expression.Lambda<Action<object>>(block, param);
        }
    }
}
