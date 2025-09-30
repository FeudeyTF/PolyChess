namespace PolyChess.Core.Commands
{
    internal interface ICommandExecutionContext
    {
        public List<string> Arguments { get; }
    }
}
