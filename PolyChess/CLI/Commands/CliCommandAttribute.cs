using PolyChess.Core.Commands.Aggregators.Method;

namespace PolyChess.CLI.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class CliCommandAttribute : Attribute, ICommandAttribute
    {
        public string Name { get; }

        public CliCommandAttribute(string name)
        {
            Name = name;
        }
    }
}
