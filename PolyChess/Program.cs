﻿using LichessAPI.Clients;
using Microsoft.EntityFrameworkCore;
using PolyChess.CLI;
using PolyChess.CLI.Commands;
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
using PolyChess.Core.Logging.Types;
using PolyChess.Core.Telegram.Providers;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace PolyChess
{
    internal class Program
    {
        private static MainConfigFile? _configuration;

        private static InitializerComponent? _initializerComponent;

        private static readonly ConsoleLogger _logger;

        private static readonly CommandManager<CliCommandExecutionContext> _consoleCommandManager;

        static Program()
        {
            _logger = new();
            _consoleCommandManager = new([new DefaultCommands()]);
        }

        /// <summary>
        /// TODO: Переписать код, используя библиотеки Microsoft
        /// </summary>
        private static async Task Main(string[] args)
        {
            CancellationTokenSource tokenSource = new();
            _configuration = ConfigFile.Load<MainConfigFile>();
            _configuration.Save();

            DbContextOptionsBuilder<PolyContext> contextBuilder = new();
            PolyContext polyContext = new(contextBuilder.UseSqlite(_configuration.DatabaseConnectionString).Options);
            PolyContextComponent polyContextComponent = new(polyContext, _logger);

            LichessClient lichessClient = new();
            TournamentsComponent tournaments = new(_configuration, _logger, lichessClient, polyContext);

            var telegramProvider = new PollingTelegramProvider(
                new TelegramBotClient(_configuration.TelegramToken),
                new ReceiverOptions(),
                tokenSource.Token
            );

            MeTelegramCommand telegramCommands = new(telegramProvider, lichessClient, polyContext, tournaments, _configuration, new(telegramProvider));
            AdminCommands adminCommands = new(polyContext, telegramProvider, tournaments, _configuration);
            StudentCommands studentCommands = new(polyContext, _configuration, telegramProvider, lichessClient);

            AttendanceHandler attendanceHandler = new(polyContext);
            QuestionHandler questionHandler = new(_configuration);

            TelegramComponent telegramComponent = new(
                _logger,
                telegramProvider,
                _configuration,
                [
                    telegramCommands,
                    adminCommands,
                    studentCommands
                ],
                [
                    telegramCommands,
                    adminCommands,
                    studentCommands
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

            while (true)
            {
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
