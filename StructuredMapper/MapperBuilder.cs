using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StructuredMapper
{
    public class MapperBuilder<TFrom, TTo> 
        where TTo : new()
    {
        private readonly HashSet<string> _expressionBodies = new HashSet<string>();
        private readonly List<Func<TFrom, TTo, Task<TTo>>> _propertyMappers = new List<Func<TFrom, TTo, Task<TTo>>>();
        private readonly List<Func<TFrom, Task<TTo>>> _objectMappers = new List<Func<TFrom, Task<TTo>>>();

        /// <summary>
        /// Describes a mapping that will executed asynchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> ForProperty<TToProp>(
            Expression<Func<TTo, TToProp>> toSelector,
            Func<TFrom, Task<TToProp>> mapper)
        {
            ThrowIfAlreadyRegistered(toSelector.Body);
            var propertyMapper = new PropertyMapper<TFrom, TTo, TToProp>(toSelector, mapper);
            _propertyMappers.Add(propertyMapper.Map);
            return this;
        }
        
        /// <summary>
        /// Describes a property mapping that will executed synchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> ForProperty<TToProp>(
            Expression<Func<TTo, TToProp>> toSelector,
            Func<TFrom, TToProp> mapFunc)
        {
            ThrowIfAlreadyRegistered(toSelector.Body);
            var propertyMapper = new PropertyMapper<TFrom, TTo, TToProp>(toSelector, mapFunc);
            _propertyMappers.Add(propertyMapper.Map);
            return this;
        }

        /// <summary>
        /// Describes an object mapping that will execute synchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> ForObject(
            Expression<Func<TTo, TTo>> objectExpression,
            Func<TFrom, TTo> mapFunc)
        {
            ThrowIfAlreadyRegistered(objectExpression.Body);
            var mapper = new ObjectMapper<TFrom, TTo>(objectExpression, mapFunc);
            _objectMappers.Add(mapper.Map);            
            return this;
        }
        
        /// <summary>
        /// Describes an object mapping that will execute asynchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> ForObject(
            Expression<Func<TTo, TTo>> objectExpression,
            Func<TFrom, Task<TTo>> mapFunc)
        {
            ThrowIfAlreadyRegistered(objectExpression.Body);
            var mapper = new ObjectMapper<TFrom, TTo>(objectExpression, mapFunc);
            _objectMappers.Add(mapper.Map);            
            return this;
        }
        
        public Func<TFrom, Task<TTo>> Build()
        {
            ThrowIfNoMappers();
            
            return async from =>
            {
                if (from == null)
                {
                    return default(TTo);
                }
                
                var to = new TTo();
                var tasks = _propertyMappers.Select(mapper => mapper(from, to))
                    .Concat(_objectMappers.Select(mapper => mapper(from)))
                    .ToList();
                await Task.WhenAll(tasks);
                return await tasks.First();
            };
        }
        
        private void ThrowIfNoMappers()
        {
            if (_propertyMappers.Any() || _objectMappers.Any())
            {
                return;
            }
            
            var msg = $"Nothing to map. Call the {nameof(ForObject)} method before calling {nameof(Build)}.";
            throw new InvalidOperationException(msg);
        }

        private void ThrowIfAlreadyRegistered(Expression exp)
        {
            if (_expressionBodies.Contains(exp.ToString()))
            {
                throw new InvalidOperationException($"Multiple mappings given for property {exp}.");
            }

            _expressionBodies.Add(exp.ToString());
        }
    }
}