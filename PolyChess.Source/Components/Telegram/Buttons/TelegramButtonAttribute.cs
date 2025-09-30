using PolyChess.Core.Commands.Aggregators.Method;

namespace PolyChess.Components.Telegram.Buttons
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class TelegramButtonAttribute : Attribute, ICommandAttribute
    {
        public string Name { get; set; }

        public TelegramButtonAttribute(string name)
        {
            Name = name;
        }
    }
}
