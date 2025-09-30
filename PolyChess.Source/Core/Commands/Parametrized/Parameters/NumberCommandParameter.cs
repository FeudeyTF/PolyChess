namespace PolyChess.Core.Commands.Parametrized.Parameters
{
    internal class NumberCommandParameter<TValue> : ICommandParameter where TValue : struct
    {
        public bool TryParse(string value, out object? result, out string? errorFormat, out object[] args)
        {
            result = default;
            errorFormat = default;
            args = [];
            long maxValue;
            switch (typeof(TValue).Name)
            {
                case "Int32":
                    if (int.TryParse(value, out var number))
                    {
                        result = number;
                        return true;
                    }
                    maxValue = int.MaxValue;
                    break;
                case "Byte":
                    if (byte.TryParse(value, out var byt))
                    {
                        result = byt;
                        return true;
                    }
                    maxValue = byte.MaxValue;
                    break;
                case "Int64":
                    if (long.TryParse(value, out var longNumber))
                    {
                        result = longNumber;
                        return true;
                    }
                    maxValue = long.MaxValue;
                    break;
                default:
                    return false;

            }

            errorFormat = "Value is not a number. Or bigger than {0}";
            args = [maxValue];
            return false;
        }
    }
}
