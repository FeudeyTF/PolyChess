using System.Reflection;
using System.Text;
using PolyChessTGBot.Bot.Commands;
using Telegram.Bot.Types;

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
                    var command = new Command(new[] { commandAttribute.Name }, commandAttribute.Description, commandDelegate);

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
            Console.WriteLine($"Received command: {message.Text}. Arguments: {string.Join(", ", args.Parameters)}");
            foreach (var command in Commands)
                if (command.Names.Contains(commandName))
                    await command.Delegate(args);
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
    }
}