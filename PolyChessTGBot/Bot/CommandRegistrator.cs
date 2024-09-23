using System.Reflection;
using System.Text;
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
                    var command = new Command(new[] { commandAttribute.Name }, commandAttribute.Description, commandAttribute.ScopeType, commandDelegate);

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
                if (IsWhiteSpace(commandText[i]))
                {
                    index = i;
                    break;
                }
            }
            string commandName = index < 0 ? commandText.ToLower() : commandText[..index].ToLower();
            List<string> commandArgs = index < 0 ? new() : ParseParameters(commandText[index..]);

            CommandArgs args = new(message, Program.BotClient, user, commandArgs);
            foreach (var command in Commands)
                if (command.Names.Contains(commandName))
                {
                    Program.Logger.Write($"Received command: {message.Text}. Arguments: {string.Join(", ", args.Parameters)}", LogType.Info);
                    try
                    {
                        await command.Delegate(args);
                    }
                    catch (Exception)
                    {
                        await Program.BotClient.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка при выполнении команды! Обратитесь к вашему системному администратору");
                    }
                }
        }

        private static List<string> ParseParameters(string message)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool instr = false;
            for (int i = 0; i < message.Length; i++)
            {
                char c = message[i];

                if (c == '\\' && ++i < message.Length)
                {
                    if (message[i] != '"' && message[i] != ' ' && message[i] != '\\')
                        sb.Append('\\');
                    sb.Append(message[i]);
                }
                else if (c == '"')
                {
                    instr = !instr;
                    if (!instr)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (sb.Length > 0)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else if (IsWhiteSpace(c) && !instr)
                {
                    if (sb.Length > 0)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                    sb.Append(c);
            }
            if (sb.Length > 0)
                result.Add(sb.ToString());

            return result;
        }

        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n';
        }

        public async Task RegisterCommandsInTelegram()
        {
            Dictionary<BotCommandScopeType, List<BotCommand>> commands = new();
            foreach (var command in Commands)
                if (commands.TryGetValue(command.ScopeType, out var list))
                    list.Add(command.ToTelegramCommand());
                else
                    commands.Add(command.ScopeType, new() { command.ToTelegramCommand() });
            foreach(var commandList in commands)
            await Program.BotClient.SetMyCommandsAsync(commandList.Value, GetScopeByType(commandList.Key));
        }

        private BotCommandScope GetScopeByType(BotCommandScopeType type)
        {
            return type switch{
                BotCommandScopeType.AllChatAdministrators => BotCommandScope.AllChatAdministrators(),
                BotCommandScopeType.AllGroupChats => BotCommandScope.AllGroupChats(),
                BotCommandScopeType.AllPrivateChats => BotCommandScope.AllPrivateChats(),
                _=> BotCommandScope.Default()
            };
        }
    }
}