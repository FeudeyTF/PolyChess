using PolyChess.Components.Telegram.Buttons;
using PolyChess.Components.Telegram.Callback;
using PolyChess.Components.Telegram.Commands;
using PolyChess.Configuration;
using PolyChess.Core.Commands;
using PolyChess.Core.Commands.Parsers;
using PolyChess.Core.Logging;
using PolyChess.Core.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChess.Components.Telegram
{
    internal class TelegramComponent : IComponent
    {
        private const string _commandSpecifier = "/";

        private readonly CommandManager<TelegramCommandExecutionContext> _commandManager;

        private readonly CommandManager<TelegramButtonExecutionContext> _buttonsManager;

        private readonly ITelegramProvider _telegramProvider;

        private readonly ILogger _logger;

        private readonly IMainConfig _mainConfig;

        private readonly IEnumerable<ITelegramUpdateHandler> _handlers;

        public TelegramComponent(ILogger logger, ITelegramProvider provider, IMainConfig config, List<ICommandAggregator<TelegramCommandExecutionContext>> commandAggregators, List<ICommandAggregator<TelegramButtonExecutionContext>> buttonAggregators, IEnumerable<ITelegramUpdateHandler> handlers)
        {
            _commandManager = new(commandAggregators);
            _buttonsManager = new(buttonAggregators);
            _telegramProvider = provider;
            _logger = logger;
            _mainConfig = config;
            _handlers = handlers;
            _telegramProvider.OnUpdate += HandleTelegramUpdate;
            _telegramProvider.OnMessage += HandleTelegramMessage;
            _telegramProvider.OnCallback += HandleTelegramCallback;
            _telegramProvider.OnException += HandleTelegramException;
        }

        public async Task StartAsync()
        {
            await _telegramProvider.StartAsync();
            var user = await _telegramProvider.Client.GetMe();
            if (user == null)
                _logger.Warn("Телеграм бот не был успешно запущен!");
            else
                _logger.Info($"Телеграм бот '{user.FirstName} ({user.Username})' успешно запущен!", LogLevel.Info);

            List<BotCommand> commands = [];
            foreach (var aggregator in _commandManager.Aggregators)
            {
                foreach (var command in aggregator.Commands)
                {
                    _logger.Debug($"Зарегистрирована команда: {command.Name}");
                    if (command is TelegramCommand telegramCommand && !telegramCommand.IsHidden)
                    {
                        commands.Add(
                            new BotCommand
                            {
                                Command = telegramCommand.Name,
                                Description = telegramCommand.Description
                            }
                        );
                    }
                }
            }

            await _telegramProvider.Client.SetMyCommands(commands);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        private async Task HandleTelegramUpdate(ITelegramBotClient client, Update update, CancellationToken token)
        {
            foreach (var handler in _handlers)
            {
                if (handler.Type == update.Type)
                {
                    var isHandled = await handler.HandleUpdate(client, update, token);
                    if (isHandled)
                        return;
                }
            }
        }

        private async Task HandleTelegramMessage(ITelegramBotClient client, Message message, CancellationToken token)
        {
            if (message.Text != null && message.From != null)
            {
                _logger.Info($"Получено сообщение {message.Text} от пользователя {message.From.Username} (Id: {message.From.Id})");

                if (message.Text.StartsWith(_commandSpecifier))
                {
                    SeparatorCommandArgumentsParser parser = new(_commandSpecifier, ' ', message.Text);
                    var (name, args) = parser.Parse();
                    try
                    {
                        await _commandManager.ExecuteAsync(
                            name,
                            new TelegramCommandExecutionContext(message.From, args, message, _mainConfig.TelegramAdmins, _telegramProvider, token)
                        );
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                    }
                }
            }
        }

        private async Task HandleTelegramCallback(ITelegramBotClient client, CallbackQuery query, CancellationToken token)
        {
            _logger.Info($"Получен колбэк от пользователя {query.From.Username} (Id: {query.From.Id}), Data: {query.Data}");
            if (TelegramCallbackQueryData.TryParse(query.Data, default, out var result))
            {
                try
                {
                    await _buttonsManager.ExecuteAsync(
                        result.ButtonId,
                        new TelegramButtonExecutionContext(result.ButtonId, query, result, token, _telegramProvider, [])
                    );
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
                await client.AnswerCallbackQuery(query.Id, cancellationToken: token);
            }
        }

        private Task HandleTelegramException(ITelegramBotClient client, Exception exception, global::Telegram.Bot.Polling.HandleErrorSource source, CancellationToken token)
        {
            _logger.Error(exception);
            return Task.CompletedTask;
        }
    }
}
