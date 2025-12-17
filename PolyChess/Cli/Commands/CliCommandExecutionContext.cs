using PolyChess.Core.Commands;

namespace PolyChess.Cli.Commands
{
    internal class CliCommandExecutionContext : ICommandExecutionContext
    {
        public List<string> Arguments { get; }

        public CliCommandExecutionContext(List<string> arguments)
        {
            Arguments = arguments;
        }

        public void SendMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
