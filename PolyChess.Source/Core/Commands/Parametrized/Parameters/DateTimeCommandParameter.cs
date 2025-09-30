
namespace PolyChess.Core.Commands.Parametrized.Parameters
{
    internal class DateTimeCommandParameter : ICommandParameter
    {
        public bool TryParse(string value, out object? result, out string? errorFormat, out object[] args)
        {
            if (DateTime.TryParse(value, out var date))
            {
                result = date;
                errorFormat = default;
                args = [];
                return true;
            }
            else
            {
                result = default;
                errorFormat = "{0} is incorrect date time format!";
                args = [value];
                return false;
            }
        }
    }
}
