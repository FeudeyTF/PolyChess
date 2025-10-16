using PolyChess.Core.Commands;
using PolyChess.Core.Commands.Aggregators.Method;
using PolyChess.Core.Commands.Parametrized;

namespace PolyChess.CLI.Commands
{
    internal class ConsoleCommandAggregator : ICommandAggregator<ConsoleCommandExecutionContext>
    {
        public List<ICommand<ConsoleCommandExecutionContext>> Commands => _commandsAggregator.Commands;

        private readonly MethodAggregator<ConsoleCommandExecutionContext> _commandsAggregator;

        public ConsoleCommandAggregator()
        {
            DefaultMethodCommandBuilder<ConsoleCommandExecutionContext> builder = new(BuildCommand);
            _commandsAggregator = new(this, builder);
        }

        private ParametrizedCommand<ConsoleCommandExecutionContext>? BuildCommand(ICommandAttribute attribute, object? invoker, Func<object?, object?[], object?> invokerMethod, List<ICommandParameter> parameters, List<ICommandParameter> optionalParameters, List<object?> optionalParametersDefaults)
        {
            return new ParametrizedCommand<ConsoleCommandExecutionContext>(attribute.Name, invoker, invokerMethod, parameters, optionalParameters, optionalParametersDefaults);
        }
    }
}
