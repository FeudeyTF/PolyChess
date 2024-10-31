using PolyChessTGBot.Bot.Commands.Discrete;
using PolyChessTGBot.Extensions;
using PolyChessTGBot.Logs;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot.Commands.Basic
{
    internal class DiscreteCommandRegistrator
    {
        public List<DiscreteCommand> Commands;

        public DiscreteCommandRegistrator()
        {
            Commands = [];
        }

        public void RegisterCommands(object obj, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            foreach (var method in obj.GetType().GetMethods(flags))
            {
                var commandAttribute = method.GetCustomAttribute<DiscreteCommandAttribute>();
                if (commandAttribute == null)
                    continue;
                DiscreteCommandDelegate? commandDelegate = null;
                commandDelegate = (DiscreteCommandDelegate)Delegate.CreateDelegate(typeof(DiscreteCommandDelegate), obj, method);
                if (commandDelegate != null)
                {
                    var command = new DiscreteCommand(commandAttribute.Name, commandAttribute.Description, commandAttribute.Visible, commandAttribute.Admin, commandAttribute.ScopeType, commandAttribute.Questions, commandDelegate);

                    var equals = Commands.Where(c => c.Name == command.Name);
                    if (equals.Any())
                        Commands.Remove(equals.First());
                    Commands.Add(command);
                }
            }
        }

        public async Task<bool> ExecuteCommand(string text, Message message, User user, CancellationToken token)
        {
            string commandText = text.Remove(0, 1);
            string commandName = commandText.Split(' ')[0];

            foreach (var command in Commands)
                if (command.Name == commandName)
                {
                    Program.Logger.Write($"Получена команда: '{message.Text}'.", LogType.Info);

                    if (!command.AdminCommand || Program.MainConfig.Admins.Contains(user.Id))
                    {
                        #if !DEBUG
                        try
                        {
                        #endif
                            await command.Message.Send(message.Chat.Id);
                        #if !DEBUG
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Write(e.ToString(), LogType.Error);
                            await args.Reply("Произошла ошибка при выполнении команды! Обратитесь к вашему системному администратору");
                        }
                        #endif
                    }
                    else
                        await Program.Bot.Telegram.SendMessage(new Messages.TelegramMessageBuilder("Эта команда доступна только админам!").ReplyTo(message.MessageId), message.Chat.Id);
                    return true;
                }
            return false;
        }

        public Dictionary<BotCommandScopeType, List<BotCommand>> GetCommandsInTelegram()
        {
            Dictionary<BotCommandScopeType, List<BotCommand>> commands = [];
            foreach (var command in Commands)
                if (command.Visible)
                {
                    if (commands.TryGetValue(command.ScopeType, out var list))
                        list.Add(command.ToTelegramCommand());
                    else
                        commands.Add(command.ScopeType, [command.ToTelegramCommand()]);
                }
            return commands;
        }
    }
}