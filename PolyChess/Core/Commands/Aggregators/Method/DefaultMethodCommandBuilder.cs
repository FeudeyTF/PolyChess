using PolyChess.Core.Commands.Aggregators.Method;
using PolyChess.Core.Commands.Parametrized;
using PolyChess.Core.Commands.Parametrized.Parameters;
using System.Reflection;
using System.Runtime.Serialization;

namespace PolyChess.Core.Commands.Aggregators.Typed
{
    internal delegate ICommand<TContext>? DefaultMethodCommandBuilderDelegate<TContext>(ICommandAttribute attribute, object? invoker, Func<object?, object?[], object?> invokerMethod, List<ICommandParameter> parameters, List<ICommandParameter> optionalParameters, List<object?> optionalParametersDefaults) where TContext : ICommandExecutionContext;

    internal class DefaultMethodCommandBuilder<TContext> : IMethodCommandBuilder<TContext> where TContext : ICommandExecutionContext
    {
        public Dictionary<Type, Type> TypeOverrides { get; } = new()
        {
            { typeof(string), typeof(StringCommandParameter) },
            { typeof(long), typeof(NumberCommandParameter<long>) },
            { typeof(int), typeof(NumberCommandParameter<int>) },
            { typeof(double), typeof(NumberCommandParameter<long>) },
            { typeof(float), typeof(NumberCommandParameter<int>) },
            { typeof(DateTime), typeof(DateTimeCommandParameter) },

            { typeof(long?), typeof(NumberCommandParameter<long>) },
            { typeof(int?), typeof(NumberCommandParameter<int>) },
            { typeof(double?), typeof(NumberCommandParameter<long>) },
            { typeof(float?), typeof(NumberCommandParameter<int>) },
            { typeof(DateTime?), typeof(DateTimeCommandParameter) }
        };

        private DefaultMethodCommandBuilderDelegate<TContext> _builder;

        public DefaultMethodCommandBuilder(DefaultMethodCommandBuilderDelegate<TContext> builder)
        {
            _builder = builder;
        }

        public ICommand<TContext>? Build(MethodInfo info, object? registrator)
        {
            var commandAttribute = (ICommandAttribute?)info.GetCustomAttributes().FirstOrDefault(a => a.GetType().IsAssignableTo(typeof(ICommandAttribute)));
            if (commandAttribute == null)
                return null;
            if (info.DeclaringType == null)
                return null;

            var name = commandAttribute.Name;

            var methodParameters = info.GetParameters();
            if (methodParameters.Length > 0 && methodParameters[0].ParameterType == typeof(TContext))
            {
                List<ICommandParameter> commandParameters = [];
                List<ICommandParameter> commandOptionalParameters = [];
                List<object?> commandOptionalParametersDefaultValues = [];

                if (methodParameters.Length > 1)
                {
                    var interfaceType = typeof(ICommandParameter);
                    for (int i = 1; i < methodParameters.Length; i++)
                    {
                        var methodParameter = methodParameters[i];
                        ICommandParameter? parameter = CreateCommandParameterInstance(methodParameter.ParameterType);
                        if (parameter == null && methodParameter.ParameterType.IsArray)
                        {
                            var elementType = methodParameter.ParameterType.GetElementType();
                            if (elementType != null)
                            {
                                var arrayType = CreateCommandParameterInstance(elementType);
                                if (arrayType != null)
                                    parameter = new ArrayCommandParameter(arrayType.GetType(), elementType);
                            }
                        }

                        if (parameter != null)
                        {
                            if (methodParameter.HasDefaultValue)
                            {
                                commandOptionalParametersDefaultValues.Add(methodParameter.DefaultValue);
                                commandOptionalParameters.Add(parameter);
                            }
                            else
                                commandParameters.Add(parameter);
                        }
                        else
                        {
                            throw new Exception($"Command '{name}' was ignored due to incorrect parameter type: '{methodParameter.Name}' of '{methodParameter.ParameterType.FullName}'");
                        }
                    }
                }

                return _builder
                (
                    commandAttribute,
                    registrator,
                    info.Invoke,
                    commandParameters,
                    commandOptionalParameters,
                    commandOptionalParametersDefaultValues
                );

#pragma warning disable SYSLIB0050
                ICommandParameter? CreateCommandParameterInstance(Type type)
                {
                    if (type.IsAssignableTo(typeof(ICommandParameter)))
                    {
                        return (ICommandParameter)FormatterServices.GetUninitializedObject(type);

                    }
                    else if (TypeOverrides.TryGetValue(type, out var defaultType))
                    {
                        return (ICommandParameter)FormatterServices.GetUninitializedObject(defaultType);
                    }
                    return null;
                }
#pragma warning restore SYSLIB0050
            }
            return null;
        }
    }
}
