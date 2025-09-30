namespace PolyChess.Core.Commands
{
    internal class CommandManager<TContext> where TContext : ICommandExecutionContext
    {
        public List<ICommandAggregator<TContext>> Aggregators { get; private set; }

        public CommandManager(List<ICommandAggregator<TContext>> aggregators)
        {
            Aggregators = aggregators;
        }

        public async Task ExecuteAsync(string name, TContext context)
        {
            foreach (var aggregator in Aggregators)
            {
                var command = aggregator.Commands.FirstOrDefault(c => c.Name == name);
                if (command == null || !(await command.IsCommandRunable(context)))
                    continue;
                await command.ExecuteAsync(context);
            }
        }
    }
}
