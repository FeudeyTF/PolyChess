using PolyChess.Core.Commands.Aggregators.Method;

namespace PolyChess.CLI.Commands
{
    internal class ConsoleCommandAttribute : Attribute, ICommandAttribute
    {
        public string Name { get; }

        public ConsoleCommandAttribute(string name)
        {
            Name = name;
        }
    }
}
