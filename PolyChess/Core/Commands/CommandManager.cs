namespace PolyChess.Core.Commands
{
    internal class CommandManager<TContext> where TContext : ICommandExecutionContext
    {
        public List<ICommandAggregator<TContext>> Aggregators { get; private set; }

        private readonly Dictionary<string, ICommand<TContext>> _cashedCommands;

        public CommandManager(List<ICommandAggregator<TContext>> aggregators)
        {
            Aggregators = aggregators;

            _cashedCommands = [];
            foreach (var aggregator in Aggregators)
                foreach (var command in aggregator.Commands)
                    _cashedCommands.Add(command.Name, command);
        }

        public async Task ExecuteAsync(string name, TContext context)
        {
            if(_cashedCommands.TryGetValue(name, out var command))
            {
                if (command == null || !(await command.IsCommandRunable(context)))
                    return;
                await command.ExecuteAsync(context);
            }
        }
    }
}
