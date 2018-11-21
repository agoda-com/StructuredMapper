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
        private readonly List<Func<TFrom, TTo, Task<TTo>>> _mappers = new List<Func<TFrom, TTo, Task<TTo>>>();

        private bool _hasAsync = false;

        /// <summary>
        /// Describes a mapping that will executed asynchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> propertyExpression,
            Func<TFrom, Task<TToProp>> mappingFunc)
        {
            EnsurePropertyUnique(propertyExpression.Body);
            var mapper = new PropertyMapper<TFrom, TTo, TToProp>(propertyExpression, mappingFunc);
            _mappers.Add(mapper.Map);
            _hasAsync = true;
            return this;
        }
        
        /// <summary>
        /// Describes a property mapping that will executed synchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> propertyExpression,
            Func<TFrom, TToProp> mappingFunc)
        {
            EnsurePropertyUnique(propertyExpression.Body);
            var mapper = new PropertyMapper<TFrom, TTo, TToProp>(propertyExpression, mappingFunc);
            _mappers.Add(mapper.Map);
            return this;
        }
        
        /// <summary>
        /// Describes a property mapping to a literal value retrieved asynchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> propertyExpression,
            Task<TToProp> value)
        {
            return For(propertyExpression, _ => value);
        }
        
        /// <summary>
        /// Describes a property mapping to a literal value retrieved synchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> propertyExpression,
            TToProp value)
        {
            return For(propertyExpression, _ => value);
        }
      
        /// <summary>
        /// Builds the mapper.
        /// </summary>
        /// <remarks>
        /// This method builds an asynchronous version of the mapper.
        /// </remarks>>
        /// <returns>
        /// A mapping function that executes asynchronously.
        /// </returns>
        public Func<TFrom, Task<TTo>> Build()
        {
            EnsureMappersDeclared();
            
            return async from =>
            {
                if (from == null)
                {
                    return default(TTo);
                }
                
                var to = new TTo();
                var tasks = _mappers.Select(mapper => mapper(from, to));
                await Task.WhenAll(tasks);
                return to;
            };
        }
        
        /// <summary>
        /// Builds the mapper.
        /// </summary>
        /// <remarks>
        /// This method builds a synchronous version of the mapper and can only be called when no asynchronous mappers
        /// have been declared.
        /// </remarks>>
        /// <returns>
        /// A mapping function that executes synchronously.
        /// </returns>
        public Func<TFrom, TTo> BuildSync()
        {
            EnsureMappersDeclared();
            EnsureNoAsyncMappers();
            
            return from =>
            {
                if (from == null)
                {
                    return default(TTo);
                }
                
                var to = new TTo();
                var tasks = _mappers.Select(mapper => mapper(from, to)).ToArray();
                Task.WaitAll(tasks);
                return to;
            };
        }
        
        private void EnsureMappersDeclared()
        {
            if (_mappers.Any())
            {
                return;
            }
            
            var msg = $"Nothing to map. Call the {nameof(For)}() method before building.";
            throw new InvalidOperationException(msg);
        }

        private void EnsurePropertyUnique(Expression exp)
        {
            var expAsString = exp.ToString();
            if (_expressionBodies.Contains(expAsString))
            {
                var msg = $"Multiple mappings for property {expAsString} were declared.";
                throw new InvalidOperationException(msg);
            }

            _expressionBodies.Add(expAsString);
        }
        
        private void EnsureNoAsyncMappers()
        {
            if (!_hasAsync)
            {
                return;
            }

            var msg = $"Cannot build synchronous mapper when asynchronous mappers have been declared. " +
                      $"Try calling {nameof(Build)}() instead.";
            throw new InvalidOperationException(msg);
        }
    }
}