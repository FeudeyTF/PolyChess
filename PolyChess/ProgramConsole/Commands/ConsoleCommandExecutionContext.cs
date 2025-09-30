using PolyChess.Core.Commands;

namespace PolyChess.ProgramConsole.Commands
{
    internal class ConsoleCommandExecutionContext : ICommandExecutionContext
    {
        public List<string> Arguments { get; }

        public ConsoleCommandExecutionContext(List<string> arguments)
        {
            Arguments = arguments;
        }

        public void SendMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
