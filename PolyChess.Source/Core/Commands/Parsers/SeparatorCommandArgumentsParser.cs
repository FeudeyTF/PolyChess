using System.Text;

namespace PolyChess.Core.Commands.Parsers
{
    internal class SeparatorCommandArgumentsParser : ICommandArgumentsParser
    {
        private readonly string _specifier;

        private readonly char _separator;

        private readonly string _text;

        public SeparatorCommandArgumentsParser(string specifier, char separator, string text)
        {
            _specifier = specifier;
            _separator = separator;
            _text = text;
        }

        public (string Name, List<string> Arguments) Parse()
        {
            var commandText = _text[_specifier.Length..];

            int index = -1;
            for (int i = 0; i < commandText.Length; i++)
            {
                if (commandText[i] == _separator)
                {
                    index = i;
                    break;
                }
            }

            if (index == 0)
                return default;

            var commandName = index < 0 ? commandText.ToLower() : commandText[..index].ToLower();
            var args = index >= 0 ? ParseParameters(commandText[index..]) : [];

            return (commandName, args);

        }
        private List<string> ParseParameters(string text)
        {
            List<string> result = [];
            StringBuilder sb = new();
            bool isInString = false;
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '\\' && ++i < text.Length)
                {
                    if (text[i] != '"' && text[i] != ' ' && text[i] != '\\')
                        sb.Append('\\');
                    sb.Append(text[i]);
                }
                else if (c == '"')
                {
                    isInString = !isInString;
                    if (!isInString)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (sb.Length > 0)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else if (c == _separator && !isInString)
                {
                    if (sb.Length > 0)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                    sb.Append(c);
            }
            if (sb.Length > 0)
                result.Add(sb.ToString());

            return result;
        }
    }
}
