using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot.Commands
{
    internal delegate Task CommandDelegate(CommandArgs args);

    internal class Command
    {
        public string Name => Names.Length > 0 ? Names[0] : "None";

        public string[] Names;

        public string Description;

        public CommandDelegate Delegate;

        public BotCommandScopeType ScopeType;

        public bool Visible;

        public bool AdminCommand;

        public Command(string[] names, string description, BotCommandScopeType scopeType, bool visible, bool admin, CommandDelegate commandDelegate)
        {
            Names = names;
            Description = description;
            Delegate = commandDelegate;
            ScopeType = scopeType;
            Visible = visible;
            AdminCommand = admin;
        }

        public BotCommand ToTelegramCommand()
        {
            var result = new BotCommand();
            result.Command = Name;
            result.Description = Description;
            return result;
        }
    }
}