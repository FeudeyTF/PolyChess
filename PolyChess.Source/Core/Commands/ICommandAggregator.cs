namespace PolyChess.Core.Commands
{
    internal interface ICommandAggregator<TContext> where TContext : ICommandExecutionContext
    {
        public List<ICommand<TContext>> Commands { get; }
    }
}
