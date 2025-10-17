using PolyChess.Core.Commands;
using PolyChess.Core.Commands.Aggregators.Method;
using PolyChess.Core.Commands.Parametrized;

namespace PolyChess.CLI.Commands
{
    internal class CliCommandAggregator : ICommandAggregator<CliCommandExecutionContext>
    {
        public List<ICommand<CliCommandExecutionContext>> Commands => _commandsAggregator.Commands;

        private readonly MethodAggregator<CliCommandExecutionContext> _commandsAggregator;

        public CliCommandAggregator()
        {
            DefaultMethodCommandBuilder<CliCommandExecutionContext> builder = new(BuildCommand);
            _commandsAggregator = new(this, builder);
        }

        private ParametrizedCommand<CliCommandExecutionContext>? BuildCommand(ICommandAttribute attribute, object? invoker, Func<object?, object?[], object?> invokerMethod, List<ICommandParameter> parameters, List<ICommandParameter> optionalParameters, List<object?> optionalParametersDefaults)
        {
            return new ParametrizedCommand<CliCommandExecutionContext>(attribute.Name, invoker, invokerMethod, parameters, optionalParameters, optionalParametersDefaults);
        }
    }
}
