using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot.Commands.Discrete
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class DiscreteCommandAttribute : Attribute
    {
        public string Name { get; }

        public string Description { get; }

        public BotCommandScopeType ScopeType { get; }

        public bool Visible { get; }

        public bool Admin { get; }

        public List<string> Questions { get; }

        public DiscreteCommandAttribute(string name, string description, string[] questions, bool visible = false, bool admin = false, BotCommandScopeType scopeType = BotCommandScopeType.Default)
        {
            Name = name;
            Description = description;
            ScopeType = scopeType;
            Admin = admin;
            Visible = visible;
            Questions = [.. questions];
        }
    }
}
