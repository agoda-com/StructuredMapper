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

        private bool _hasAsync = false;

        /// <summary>
        /// Describes a mapping that will executed asynchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> toSelector,
            Func<TFrom, Task<TToProp>> mapper)
        {
            ThrowIfAlreadyRegistered(toSelector.Body);
            var propertyMapper = new PropertyMapper<TFrom, TTo, TToProp>(toSelector, mapper);
            _propertyMappers.Add(propertyMapper.Map);
            _hasAsync = true;
            return this;
        }
        
        /// <summary>
        /// Describes a property mapping that will executed synchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> toSelector,
            Func<TFrom, TToProp> mapFunc)
        {
            ThrowIfAlreadyRegistered(toSelector.Body);
            var propertyMapper = new PropertyMapper<TFrom, TTo, TToProp>(toSelector, mapFunc);
            _propertyMappers.Add(propertyMapper.Map);
            return this;
        }
        
        /// <summary>
        /// Describes a property mapping to a literal value retrieved asynchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> toSelector,
            Task<TToProp> value)
        {
            return For(toSelector, _ => value);
        }
        
        /// <summary>
        /// Describes a property mapping to a literal value retrieved synchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> toSelector,
            TToProp value)
        {
            return For(toSelector, _ => value);
        }

        /// <summary>
        /// Describes an object mapping to a literal value that will execute synchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> ForObject(
            Expression<Func<TTo, TTo>> objectExpression,
            TTo value)
        {
            return ForObject(objectExpression, _ => value);
        }
        
        /// <summary>
        /// Describes an object mapping to a literal value that will execute asynchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> ForObject(
            Expression<Func<TTo, TTo>> objectExpression,
            Task<TTo> value)
        {
            return ForObject(objectExpression, _ => value);
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
            _hasAsync = true;
            return this;
        }
        
        /// <summary>
        /// Builds the mapper.
        /// </summary>
        /// <remarks>
        /// This method builds an asynchronous version of the mapper.
        /// </remarks>>
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
        
        /// <summary>
        /// Builds the mapper.
        /// </summary>
        /// <remarks>
        /// This method builds a synchronous version of the mapper and can only be called when no asynchronous mappers
        /// have been defined. Prefer the Build method, which returns an asynchronous version.
        /// </remarks>>
        public Func<TFrom, TTo> BuildSync()
        {
            ThrowIfNoMappers();
            ThrowIfHasAsync();
            
            return from =>
            {
                if (from == null)
                {
                    return default(TTo);
                }
                
                var to = new TTo();
                var tasks = _propertyMappers.Select(mapper => mapper(from, to))
                    .Concat(_objectMappers.Select(mapper => mapper(from)))
                    .ToArray();
                Task.WaitAll(tasks);
                return tasks.First().Result;
            };
        }

        private void ThrowIfNoMappers()
        {
            if (_propertyMappers.Any() || _objectMappers.Any())
            {
                return;
            }
            
            var msg = $"Nothing to map. Call the {nameof(ForObject)} method before building.";
            throw new InvalidOperationException(msg);
        }

        private void ThrowIfAlreadyRegistered(Expression exp)
        {
            var expAsString = exp.ToString();
            if (_expressionBodies.Contains(expAsString))
            {
                var msg = $"Multiple mappings given expression {expAsString}.";
                throw new InvalidOperationException(msg);
            }

            _expressionBodies.Add(expAsString);
        }
        
        private void ThrowIfHasAsync()
        {
            if (!_hasAsync)
            {
                return;
            }

            var msg = $"Cannot build synchronous mapper when async mappers have been given." +
                      $"(Did you mean to call {nameof(Build)}()?)";
            throw new InvalidOperationException(msg);
        }
    }
}