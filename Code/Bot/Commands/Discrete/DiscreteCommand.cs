using PolyChessTGBot.Bot.Messages.Discrete;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot.Commands.Discrete
{
    internal delegate Task DiscreteCommandDelegate(CommandArgs<Message> args);

    internal class DiscreteCommand
    {
        public DiscreteMessage Message;

        public string Name;

        public string Description;

        public bool Visible;

        public BotCommandScopeType ScopeType;

        public bool AdminCommand;

        private DiscreteCommandDelegate Delegate;

        public DiscreteCommand(string name, string description, bool visible, bool admin, BotCommandScopeType scope, List<string> questions, DiscreteCommandDelegate onEntered)
        {
            Delegate = onEntered;
            Name = name;
            Description = description;
            Visible = visible;
            AdminCommand = admin;
            ScopeType = scope;
            Message = new(questions, OnEntered);
        }

        private async Task OnEntered(DiscreteMessageEnteredArgs args)
        {
            await Delegate(new CommandArgs<Message>(args.Responses.Last(), args.Bot, args.User, [.. args.Responses], default));
        }

        public BotCommand ToTelegramCommand()
        {
            var result = new BotCommand
            {
                Command = Name,
                Description = Description
            };
            return result;
        }
    }
}
