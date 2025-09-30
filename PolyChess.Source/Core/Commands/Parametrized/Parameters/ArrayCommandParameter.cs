namespace PolyChess.Core.Commands.Parametrized.Parameters
{
    internal class ArrayCommandParameter<TArray> : ICommandParameter where TArray : ICommandParameter, new()
    {
        public const string Separator = " ";

        public List<TArray> Value;

        public ArrayCommandParameter(List<TArray> value)
        {
            Value = value;
        }

        public bool TryParse(string value, out object? result, out string? errorFormat, out object[] args)
        {
            List<TArray> values = [];
            TArray defaultParameter = new();
            var splitted = value.Split(Separator);
            for (int i = 0; i < splitted.Length; i++)
            {
                if (defaultParameter.TryParse(splitted[i], out var parameter, out errorFormat, out args))
                {
                    if (parameter is TArray arrayParameter)
                        values.Add(arrayParameter);
                }
                else
                {
                    result = default;
                    return false;
                }
            }
            errorFormat = default;
            args = [];
            result = new ArrayCommandParameter<TArray>(values);
            return true;
        }
    }

    internal class ArrayCommandParameter : ICommandParameter
    {
        public const string Separator = " ";

        public ICommandParameter ArrayParameter;

        public Type RealType;

        public ArrayCommandParameter(Type arrayType, Type realType)
        {
            RealType = realType;
            var constructor = arrayType.GetConstructor([]);
            if (constructor != null)
                ArrayParameter = (ICommandParameter)constructor.Invoke(null);
            else
                throw new Exception("ICommandParameter constructor need more, than one element!");
        }

        public bool TryParse(string value, out object? result, out string? errorFormat, out object[] args)
        {
            var splitted = value.Split(Separator);
            var array = Array.CreateInstance(RealType, splitted.Length);
            for (int i = 0; i < splitted.Length; i++)
            {
                if (ArrayParameter.TryParse(splitted[i], out var parameter, out errorFormat, out args) && parameter != null)
                {
                    array.SetValue(parameter, i);
                }
                else
                {
                    result = default;
                    return false;
                }
            }
            errorFormat = default;
            args = [];
            result = array;
            return true;
        }
    }
}
