using PolyChess.Core.Commands.Aggregators.Method;

namespace PolyChess.ProgramConsole.Commands
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
