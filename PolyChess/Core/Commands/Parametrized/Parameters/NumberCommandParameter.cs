namespace PolyChess.Core.Commands.Parametrized.Parameters
{
    internal class NumberCommandParameter<TValue> : ICommandParameter where TValue : struct
    {
        public bool TryParse(string value, out object? result, out string? errorFormat, out object[] args)
        {
            result = default;
            errorFormat = default;
            args = [];
            switch (typeof(TValue).Name)
            {
                case "Int32":
                    if (int.TryParse(value, out var number))
                    {
                        result = number;
                        return true;
                    }
                    args = [int.MaxValue];
                    break;
                case "Byte":
                    if (byte.TryParse(value, out var byt))
                    {
                        result = byt;
                        return true;
                    }
                    args = [byte.MaxValue];
                    break;
                case "Int64":
                    if (long.TryParse(value, out var longNumber))
                    {
                        result = longNumber;
                        return true;
                    }
                    args = [long.MaxValue];
                    break;
                case "Single":
                    if (float.TryParse(value, out var floatNumber))
                    {
                        result = floatNumber;
                        return true;
                    }
                    args = [float.MaxValue];
                    break;
                case "Double":
                    if (double.TryParse(value, out var doubleNumber))
                    {
                        result = doubleNumber;
                        return true;
                    }
                    args = [double.MaxValue];
                    break;
                default:
                    return false;

            }

            errorFormat = "Value is not a number. Or bigger than {0}";
            return false;
        }
    }
}
