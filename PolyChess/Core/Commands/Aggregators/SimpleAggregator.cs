
namespace PolyChess.Core.Commands.Aggregators
{
    internal class SimpleAggregator<TContext> : ICommandAggregator<TContext> where TContext : ICommandExecutionContext
    {
        public List<ICommand<TContext>> Commands { get; }

        public SimpleAggregator(List<ICommand<TContext>> commands)
        {
            Commands = commands;
        }
    }
}
