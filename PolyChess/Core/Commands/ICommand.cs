namespace PolyChess.Core.Commands
{
    internal interface ICommand<TContext> where TContext : ICommandExecutionContext
    {
        public string Name { get; }

        public Task<bool> ExecuteAsync(TContext ctx);

        public Task<bool> IsCommandRunable(TContext ctx);
    }
}
