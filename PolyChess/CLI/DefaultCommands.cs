using PolyChess.Cli.Commands;

namespace PolyChess.Cli
{
#pragma warning disable CS1998, CA1822
    internal class DefaultCommands : CliCommandAggregator
    {
        [CliCommand("exit")]
        public async Task Exit(CliCommandExecutionContext ctx)
        {
            ctx.SendMessage("Программа закрывается...");
            Environment.Exit(0);
        }
    }
#pragma warning restore CS1998, CA1822
}
