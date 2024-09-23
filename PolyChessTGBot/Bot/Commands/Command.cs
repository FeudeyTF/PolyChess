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

		public Command(string[] names, string description, BotCommandScopeType scopeType, CommandDelegate commandDelegate)
		{
            Names = names;
            Description = description;
            Delegate = commandDelegate;
            ScopeType = scopeType;
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