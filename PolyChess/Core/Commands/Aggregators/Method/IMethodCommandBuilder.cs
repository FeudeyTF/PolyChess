using System.Reflection;

namespace PolyChess.Core.Commands.Aggregators.Method
{
    internal interface IMethodCommandBuilder<TContext> where TContext : ICommandExecutionContext
    {
        public Dictionary<Type, Type> TypeOverrides { get; }

        public ICommand<TContext>? Build(MethodInfo info, object? registrator);
    }
}
