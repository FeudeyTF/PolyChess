namespace PolyChess.Core.Commands.Parametrized.Parameters
{
    internal class StringCommandParameter : ICommandParameter
    {
        public bool TryParse(string value, out object? result, out string? errorFormat, out object[] args)
        {
            result = value;
            errorFormat = default;
            args = [];
            return true;
        }
    }
}
