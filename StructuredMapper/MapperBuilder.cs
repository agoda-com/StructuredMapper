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

        /// <summary>
        /// Describes a mapping that will executed asynchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> toSelector,
            Func<TFrom, Task<TToProp>> mapper)
        {
            ThrowIfRegistered(toSelector.Body);
            var propertyMapper = new PropertyMapper<TFrom, TTo, TToProp>(toSelector, mapper);
            _mappers.Add(propertyMapper.Map);
            return this;
        }
        
        /// <summary>
        /// Describes a mapping that will executed synchronously.
        /// </summary>
        public MapperBuilder<TFrom, TTo> For<TToProp>(
            Expression<Func<TTo, TToProp>> toSelector,
            Func<TFrom, TToProp> mapper)
        {
            ThrowIfRegistered(toSelector.Body);
            var propertyMapper = new PropertyMapper<TFrom, TTo, TToProp>(toSelector, mapper);
            _mappers.Add(propertyMapper.Map);
            return this;
        }

        public Func<TFrom, Task<TTo>> Build()
        {
            if (!_mappers.Any())
            {
                var msg = $"Nothing to map. Call the {nameof(For)} method before calling {nameof(Build)}.";
                throw new InvalidOperationException(msg);
            }
            
            return async from =>
            {
                if (from == null)
                {
                    return default(TTo);
                }
                var to = new TTo();
                var tasks = _mappers.Select(mapper => mapper(from, to)).ToList();
                await Task.WhenAll(tasks);
                return await tasks.First();
            };
        }

        private void ThrowIfRegistered(Expression exp)
        {
            if (_expressionBodies.Contains(exp.ToString()))
            {
                throw new InvalidOperationException($"Multiple mappings given for property {exp}.");
            }

            _expressionBodies.Add(exp.ToString());
        }
    }
}