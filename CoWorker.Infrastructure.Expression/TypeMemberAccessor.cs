using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
namespace CoWorker.Expressions
{
    public class TypeMemberAccessor : IDisposable
    {
        public static TypeMemberAccessor GetAccessor(Type type) => new TypeMemberAccessor(type);

        private IDictionary<string, object> accessors;

        public TypeMemberAccessor Clone()
        {
            var result = new TypeMemberAccessor(this.Type);
            result.accessors = accessors;
            return result;
        }
        public Type Type { get; }
        private TypeMemberAccessor(Type type)
        {
            this.Type = type;
        }

        public T GetValue<T>(Object obj, string name)
            => (accessors.ContainsKey(name)
                ? accessors[name] as Func<object, object>
                : CreateAccessor(Type, name))(obj)
                    is T result
                        ? result
                        : default(T);
        

        private Func<object,object> CreateAccessor(Type type,string name)
        {
            var info = type.GetTypeInfo().DeclaredProperties.FirstOrDefault() ?? throw new NullReferenceException(name);
            accessors.Add(name, typeof(object).ToParameter()
                .MakeLambda<Func<object, object>>(arg => arg.AsTypeTo(type).GetPropertyOrField(name)).Compile());
            return accessors[name] as Func<object,object>;
        }

        public void Dispose()
        {
            accessors = null;
            GC.SuppressFinalize(this);
        }
    }
}
