using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StructuredMapper
{
    internal class PropertyMapper<TFrom, TTo, TToProp>
    {
        private readonly Func<TFrom, TToProp> _syncMapper;
        private readonly Func<TFrom, Task<TToProp>> _asyncMapper;
        private readonly Action<TTo, TToProp> _setProperty;

        private PropertyMapper(Expression<Func<TTo, TToProp>> toExpression)
        {
            if (toExpression.Body.NodeType != ExpressionType.MemberAccess)
            {
                var msg =
                    $"The supplied {nameof(ExpressionType)} should be of type {nameof(ExpressionType.MemberAccess)}. " +
                    $"Instead, an {nameof(ExpressionType)} of {toExpression.Body.NodeType} was supplied. " +
                    $"Expression should be something like to => to.TargetProperty.";
                throw new ArgumentException(msg, nameof(toExpression)); 
            }
            _setProperty = CompileSetter(toExpression); 
        }
        
        /// <summary>
        /// Creates a mapper for a single property that will run asynchronously.
        /// </summary>
        public PropertyMapper(
            Expression<Func<TTo, TToProp>> toExpression,
            Func<TFrom, Task<TToProp>> asyncMapper)
            : this(toExpression)
        {    
            _asyncMapper = asyncMapper;            
        }

        /// <summary>
        /// Creates a mapper for a single property that will run synchronously.
        /// </summary>
        public PropertyMapper(
            Expression<Func<TTo, TToProp>> toExpression,
            Func<TFrom, TToProp> syncMapper)
            : this(toExpression)
        {
            _syncMapper = syncMapper;
        }
        
        /// <summary>
        /// Sets the previously specified property to the mapped value.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<TTo> Map(TFrom from, TTo to)
        {
            if (_syncMapper != null)
            {
                _setProperty(to, _syncMapper(from));
            }
            else
            {
                _setProperty(to, await _asyncMapper(from));
            }

            return to;
        }

        private static Action<TTo, TToProp> CompileSetter(Expression<Func<TTo, TToProp>> toExpression) 
        { 
            // Input model 
            var toProp = toExpression.Parameters[0]; 
            // Input value to set 
            var value = Expression.Variable(typeof(TToProp), "toProperty"); 
            // Member access 
            var member = toExpression.Body; 
            // We turn the access into an assignation to the input value 
            var assignation = Expression.Assign(member, value); 
            // We wrap the action into a lambda expression with parameters 
            var assignLambda = Expression.Lambda<Action<TTo, TToProp>>(assignation, toProp, value);

            return assignLambda.Compile();
        }
    }
}