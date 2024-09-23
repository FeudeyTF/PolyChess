namespace PolyChessTGBot.Bot.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class CommandAttribute : Attribute
    {
        public string Name { get; }

        public string Description { get; }

        public CommandAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}