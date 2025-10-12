using PolyChess.Components.Telegram.Buttons;
using PolyChess.Components.Telegram.Commands;
using PolyChess.Core.Commands;
using PolyChess.Core.Commands.Aggregators.Method;
using PolyChess.Core.Commands.Parametrized;
using System.Reflection;

namespace PolyChess.Components.Telegram
{
    internal class TelegramCommandAggregator : ICommandAggregator<TelegramCommandExecutionContext>, ICommandAggregator<TelegramButtonExecutionContext>
    {
        List<ICommand<TelegramCommandExecutionContext>> ICommandAggregator<TelegramCommandExecutionContext>.Commands => _commandAggregator.Commands;

        List<ICommand<TelegramButtonExecutionContext>> ICommandAggregator<TelegramButtonExecutionContext>.Commands => _buttonsAggregator.Commands;

        private readonly MethodAggregator<TelegramCommandExecutionContext> _commandAggregator;

        private readonly MethodAggregator<TelegramButtonExecutionContext> _buttonsAggregator;

        public TelegramCommandAggregator(BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            DefaultMethodCommandBuilder<TelegramCommandExecutionContext> commandBuilder = new(BuildCommand);
            DefaultMethodCommandBuilder<TelegramButtonExecutionContext> buttonBuilder = new(BuildButton);
            _commandAggregator = new(this, commandBuilder, flags);
            _buttonsAggregator = new(this, buttonBuilder, flags);
        }

        private TelegramCommand? BuildCommand(ICommandAttribute attribute, object? invoker, Func<object?, object?[], object?> invokerMethod, List<ICommandParameter> parameters, List<ICommandParameter> optionalParameters, List<object?> optionalParametersDefaults)
        {
            if (attribute is TelegramCommandAttribute telegramAttribute)
                return new TelegramCommand(telegramAttribute.Name, telegramAttribute.Description, telegramAttribute.IsAdmin, telegramAttribute.IsHidden, invoker, invokerMethod, parameters, optionalParameters, optionalParametersDefaults);
            return null;
        }

        private ParametrizedCommand<TelegramButtonExecutionContext>? BuildButton(ICommandAttribute attribute, object? invoker, Func<object?, object?[], object?> invokerMethod, List<ICommandParameter> parameters, List<ICommandParameter> optionalParameters, List<object?> optionalParametersDefaults)
        {
            return new ParametrizedCommand<TelegramButtonExecutionContext>(attribute.Name, invoker, invokerMethod, parameters, optionalParameters, optionalParametersDefaults);
        }
    }
}
