using Telegram.Bot.Types;

namespace PolyChessTGBot.Bot.Commands
{
	internal delegate Task CommandDelegate(CommandArgs args);

    internal class Command
    {
        public string Name => Names.Length > 0 ? Names[0] : "None";

        public string[] Names;

        public string Description;

        public CommandDelegate Delegate;

		public Command(string[] names, string description, CommandDelegate commandDelegate)
		{
            Names = names;
            Description = description;
            Delegate = commandDelegate;
		}
    }
}