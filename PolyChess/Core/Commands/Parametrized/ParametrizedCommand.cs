namespace PolyChess.Core.Commands.Parametrized
{
    internal class ParametrizedCommand<TContext> : ICommand<TContext> where TContext : ICommandExecutionContext
    {
        public string Name { get; }

        private readonly object? _invoker;

        private readonly Func<object?, object?[], object?> _invokerMethod;

        private readonly List<ICommandParameter> _parameters;

        private readonly List<ICommandParameter> _optionalParameters;

        private readonly List<object?> _optionalParameterDefaults;

        public ParametrizedCommand(string name, object? invoker, Func<object?, object?[], object?> invokerMethod, List<ICommandParameter> parameters, List<ICommandParameter> optionalParameters, List<object?> optionalParametersDefaults)
        {
            Name = name;
            _invoker = invoker;
            _invokerMethod = invokerMethod;
            _parameters = parameters;
            _optionalParameters = optionalParameters;
            _optionalParameterDefaults = optionalParametersDefaults;
        }

        public Task<bool> ExecuteAsync(TContext ctx)
        {
            List<object?> parameters =
            [
                ctx
            ];

            for (int i = 0; i < _parameters.Count; i++)
            {
                if (_parameters[i].TryParse(ctx.Arguments[i], out var result, out var error, out var errorArgs) && result != default)
                    parameters.Add(result);
                else if (error != null)
                    throw new Exception(string.Format(error, errorArgs));
            }

            for (int i = 0; i < _optionalParameters.Count; i++)
            {
                string? error = default;
                if (_parameters.Count + i < ctx.Arguments.Count && _optionalParameters[i].TryParse(ctx.Arguments[_parameters.Count + i], out var result, out error, out _) && result != default)
                    parameters.Add(result);
                else if (error != null)
                    return Task.FromResult(false);
                else
                    parameters.Add(_optionalParameterDefaults[i]);
            }

            try
            {
                _invokerMethod(_invoker, [.. parameters]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return Task.FromResult(true);
        }

        public virtual Task<bool> IsCommandRunable(TContext ctx)
        {
            return Task.FromResult(true);
        }
    }
}
