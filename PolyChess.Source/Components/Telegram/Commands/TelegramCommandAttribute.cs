using PolyChess.Core.Commands.Aggregators.Method;

namespace PolyChess.Components.Telegram.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class TelegramCommandAttribute : Attribute, ICommandAttribute
    {
        public string Name { get; }

        public string Description { get; }

        public bool IsAdmin { get; set; }

        public bool IsHidden { get; set; }

        public TelegramCommandAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
