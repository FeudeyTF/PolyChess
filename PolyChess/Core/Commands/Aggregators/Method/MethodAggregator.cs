using System.Reflection;

namespace PolyChess.Core.Commands.Aggregators.Method
{
    internal class MethodAggregator<TContext> : ICommandAggregator<TContext> where TContext : ICommandExecutionContext
    {
        public List<ICommand<TContext>> Commands { get; }

        public MethodAggregator(Type type, IMethodCommandBuilder<TContext> builder, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : this(type, default, builder, flags)
        {
        }

        public MethodAggregator(object obj, IMethodCommandBuilder<TContext> builder, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : this(obj.GetType(), obj, builder, flags)
        {
        }

        private MethodAggregator(Type type, object? obj, IMethodCommandBuilder<TContext> builder, BindingFlags flags)
        {
            Commands = [];

            foreach (var method in type.GetMethods(flags))
            {
                var command = builder.Build(method, obj);

                if (command != null)
                {
                    var equals = Commands.Where(c => c.Name == command.Name);
                    if (equals.Any())
                        Commands.Remove(equals.First());
                    Commands.Add(command);
                }
            }
        }
    }
}
