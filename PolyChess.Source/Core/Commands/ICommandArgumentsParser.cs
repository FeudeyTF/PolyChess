namespace PolyChess.Core.Commands
{
    internal interface ICommandArgumentsParser
    {
        public (string Name, List<string> Arguments) Parse();
    }
}
