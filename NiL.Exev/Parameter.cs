using System;
using System.Linq.Expressions;

namespace NiL.Exev
{
    public sealed class Parameter
    {
        public readonly ParameterExpression ParameterExpression;
        public object Value;

        public Parameter(ParameterExpression parameterExpression)
        {
            ParameterExpression = parameterExpression ?? throw new ArgumentNullException(nameof(parameterExpression));
        }

        public Parameter(ParameterExpression parameterExpression, object value)
            : this(parameterExpression)
        {
            if (!parameterExpression.Type.IsAssignableFrom(value.GetType()))
                throw new ArgumentException("Invalid value type");

            Value = value;
        }
    }
}
