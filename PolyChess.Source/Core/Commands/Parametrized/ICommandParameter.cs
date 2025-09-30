namespace PolyChess.Core.Commands.Parametrized
{
    internal interface ICommandParameter
    {
        public bool TryParse(string value, out object? result, out string? errorFormat, out object[] args);
    }
}
