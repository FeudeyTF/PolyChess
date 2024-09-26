using System.Reflection;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Logs;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot
{
    internal class CommandRegistrator
    {
        public List<Command> Commands;

        public CommandRegistrator()
        {
            Commands = new();
        }

        public void RegisterCommands<TCommandClass>(BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            foreach (var method in typeof(TCommandClass).GetMethods(flags))
            {
                var commandAttribute = method.GetCustomAttribute<CommandAttribute>();
                if (commandAttribute == null)
                    continue;
                CommandDelegate? commandDelegate = null;
                commandDelegate = (CommandDelegate)Delegate.CreateDelegate(typeof(CommandDelegate), null, method);
                if (commandDelegate != null)
                {
                    var command = new Command(new[] { commandAttribute.Name }, commandAttribute.Description, commandAttribute.ScopeType, commandAttribute.Visible, commandDelegate);

                    var equals = Commands.Where(c => c.Names.Contains(command.Name));
                    if (equals.Any())
                        Commands.Remove(equals.First());
                    Commands.Add(command);
                }
            }
        }

        public async Task ExecuteCommand(string text, Message message, User user)
        {
            string commandText = text.Remove(0, 1);

            int index = -1;
            for (int i = 0; i < commandText.Length; i++)
            {
                if (commandText[i] == ' ')
                {
                    index = i;
                    break;
                }
            }
            string commandName = index < 0 ? commandText.ToLower() : commandText[..index].ToLower();
            List<string> commandArgs = index < 0 ? new() : Utils.ParseParameters(commandText[index..]);

            CommandArgs args = new(message, Program.Bot.Telegram, user, commandArgs);
            foreach (var command in Commands)
                if (command.Names.Contains(commandName))
                {
                    Program.Logger.Write($"Получена команда: `{message.Text}`. Аргументы: {string.Join(", ", args.Parameters)}", LogType.Info);
                    try
                    {
                        await command.Delegate(args);
                    }
                    catch (Exception e)
                    {
                        Program.Logger.Write(e.ToString(), LogType.Error);
                        await args.Reply("Произошла ошибка при выполнении команды! Обратитесь к вашему системному администратору");
                    }
                }
        }

        public async Task RegisterCommandsInTelegram()
        {
            Dictionary<BotCommandScopeType, List<BotCommand>> commands = new();
            foreach (var command in Commands)
                if(command.Visible)
                { 
                    if (commands.TryGetValue(command.ScopeType, out var list))
                        list.Add(command.ToTelegramCommand());
                    else
                        commands.Add(command.ScopeType, new() { command.ToTelegramCommand() });
                }
            foreach (var commandList in commands)
            await Program.Bot.Telegram.SetMyCommandsAsync(commandList.Value, Utils.GetScopeByType(commandList.Key));
        }
    }
}