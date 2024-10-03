using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class CommandAttribute : Attribute
    {
        public string Name { get; }

        public string Description { get; }

        public BotCommandScopeType ScopeType { get; }

        public bool Visible { get; }

        public CommandAttribute(string name, string description, bool visible = false, BotCommandScopeType scopeType = BotCommandScopeType.Default)
        {
            Name = name;
            Description = description;
            ScopeType = scopeType;
            Visible = visible;
        }
    }
}