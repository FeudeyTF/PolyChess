using PolyChess.ProgramConsole.Commands;

namespace PolyChess.ProgramConsole
{
#pragma warning disable CS1998, CA1822
    internal class ConsoleCommands : ConsoleCommandAggregator
    {
        [ConsoleCommand("exit")]
        public async Task Exit(ConsoleCommandExecutionContext ctx)
        {
            ctx.SendMessage("Программа закрывается...");
            Environment.Exit(0);
        }
    }
#pragma warning restore CS1998, CA1822
}
