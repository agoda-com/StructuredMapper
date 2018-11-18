using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StructuredMapper
{
    internal class ObjectMapper<TFrom, TTo> where TTo : new()
    {
        private readonly Func<TFrom, TTo> _syncMapper;
        private readonly Func<TFrom, Task<TTo>> _asyncMapper;
        private readonly Lazy<Action<TTo, TTo>> _setObject;

        private ObjectMapper(Expression<Func<TTo, TTo>> objectExpression)
        {
            var nodeType = objectExpression.Body.NodeType;
            if (nodeType != ExpressionType.Parameter)
            {
                var msg =
                    $"The supplied {nameof(ExpressionType)} should be of type {nameof(ExpressionType.Parameter)}. " +
                    $"Instead, an {nameof(ExpressionType)} of {nodeType} was supplied. " +
                    $"Expression should be something like to => to.TargetProperty.";
                if (nodeType == ExpressionType.MemberAccess)
                {
                    msg += $" (Did you mean to use the {nameof(MapperBuilder<TFrom, TTo>.ForObject)} method instead?)";
                }
                throw new ArgumentException(msg, nameof(objectExpression)); 
            }

            _setObject = new Lazy<Action<TTo, TTo>>(() => CompileSetter(objectExpression));
        }
        
        /// <summary>
        /// Creates a mapper for an object that will run asynchronously.
        /// </summary>
        public ObjectMapper(
            Expression<Func<TTo, TTo>> objectExpression,
            Func<TFrom, Task<TTo>> asyncMapper)
            : this(objectExpression)
        {    
            _asyncMapper = asyncMapper;            
        }

        /// <summary>
        /// Creates a mapper for an object that will run synchronously.
        /// </summary>
        public ObjectMapper(
            Expression<Func<TTo, TTo>> objectExpression,
            Func<TFrom, TTo> syncMapper)
            : this(objectExpression)
        {
            _syncMapper = syncMapper;
        }
        
        /// <summary>
        /// Sets the previously specified property to the mapped value.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<TTo> Map(TFrom from)
        {
            var to = _asyncMapper != null 
                ? await _asyncMapper(@from) 
                : _syncMapper(@from);
            _setObject.Value(to, to);
            return to;
        }

        private static Action<TTo, TTo> CompileSetter(Expression<Func<TTo, TTo>> objectExpression) 
        { 
            // Input model 
            var @object = objectExpression.Parameters[0]; 
            // Input value to set 
            var value = Expression.Parameter(typeof(TTo), "object"); 
            // Member access 
            var member = objectExpression.Body; 
            // We turn the access into an assignation to the input value 
            var assignation = Expression.Assign(member, value); 
            // We wrap the action into a lambda expression with parameters 
            var assignLambda = Expression.Lambda<Action<TTo, TTo>>(assignation, @object, value);

            return assignLambda.Compile();
        }
    }
}