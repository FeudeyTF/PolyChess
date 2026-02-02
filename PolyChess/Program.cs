using Microsoft.EntityFrameworkCore;
using PolyChess.Cli;
using PolyChess.Cli.Commands;
using PolyChess.Components;
using PolyChess.Components.Data;
using PolyChess.Components.Telegram;
using PolyChess.Components.Telegram.CommandAggregators;
using PolyChess.Components.Telegram.Handlers;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using PolyChess.Core.Commands;
using PolyChess.Core.Commands.Parsers;
using PolyChess.Core.Configuration;
using PolyChess.Core.Logging;
using PolyChess.Core.Logging.Types;
using PolyChess.Core.Telegram.Providers;
using PolyChess.LichessAPI.Clients;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace PolyChess
{
	internal class Program
	{
		private static MainConfigFile? _configuration;

		private static InitializerComponent? _initializerComponent;

		private static readonly ConsoleLogger _logger;

		private static CommandManager<CliCommandExecutionContext>? _consoleCommandManager;

		private static readonly LichessClient _lichessClient;

		static Program()
		{
			_lichessClient = new();
			_logger = new();
		}

		/// <summary>
		/// TODO: Переписать код, используя библиотеки Microsoft
		/// </summary>
		private static async Task Main(string[] args)
		{
			_logger.Info($"Запущен бот PolyChess. Текущая дата до изменения культуры: {DateTime.Now:f}");
			Thread.CurrentThread.CurrentCulture = new("ru-RU");
			_logger.Info($"Текущая дата после изменения культуры: {DateTime.Now:f}");
			CancellationTokenSource tokenSource = new();
			_configuration = ConfigFile.Load<MainConfigFile>();
			_configuration.Save();

			_logger.Info("Конфигурационный файл загружен и сохранён");
			_logger.Info($"Текущий семестр: {_configuration.SemesterStartDate:D} - {_configuration.SemesterEndDate:D}");
			
			DbContextOptionsBuilder<PolyContext> contextBuilder = new();
			PolyContext polyContext = new(contextBuilder.UseSqlite(_configuration.DatabaseConnectionString).Options);
			PolyContextComponent polyContextComponent = new(polyContext, _logger);

			TournamentsComponent tournaments = new(_configuration, _logger, _lichessClient, polyContext);

			var telegramProvider = new PollingTelegramProvider(
				new TelegramBotClient(_configuration.TelegramToken),
				new ReceiverOptions(),
				tokenSource.Token
			);

			MeTelegramCommand telegramCommands = new(telegramProvider, _lichessClient, polyContext, tournaments, _configuration, new(telegramProvider), _logger);
			StudentCommands studentCommands = new(polyContext, _configuration, telegramProvider, _lichessClient);
			AdminPanel adminPanel = new(telegramProvider, tournaments, _configuration, polyContext, _logger, _lichessClient);

			AttendanceHandler attendanceHandler = new(polyContext);
			QuestionHandler questionHandler = new(_configuration);

			TelegramComponent telegramComponent = new(
				_logger,
				telegramProvider,
				_configuration,
				[
					telegramCommands,
					studentCommands,
					adminPanel
				],
				[
					telegramCommands,
					studentCommands,
					adminPanel
				],
				[
					attendanceHandler,
					questionHandler
				]
			);

			_initializerComponent = new(
				[
					polyContextComponent,
					tournaments,
					telegramComponent
				],
				_logger
			);

			await _initializerComponent.StartAsync();

			_consoleCommandManager = new([new DefaultCommands(polyContext, _lichessClient)], (ctx) => Task.CompletedTask);
			while (true)
			{
				// Флаг, показывающий что можно осуществить ввод через консоль
				if (!args.Contains("--enable-console"))
					continue;
				var text = Console.ReadLine();
				if (text == null)
					continue;
				SeparatorCommandArgumentsParser parser = new("", ' ', text);
				var (name, arguments) = parser.Parse();
				await _consoleCommandManager.ExecuteAsync(name, new CliCommandExecutionContext(arguments));
			}
		}
	}
}
