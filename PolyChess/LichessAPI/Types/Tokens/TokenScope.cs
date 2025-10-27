namespace PolyChess.LichessAPI.Types.Tokens
{
    public struct TokenScope
    {
        public const string Separator = ":";

        public TokenScopeType Type { get; internal set; }

        public TokenScopeAccessLevel AccessLevel { get; internal set; }

        internal TokenScope(TokenScopeType type, TokenScopeAccessLevel accessLevel)
        {
            Type = type;
            AccessLevel = accessLevel;
        }

        public static TokenScope? Parse(string value)
        {
            var slicedToken = value.Split(Separator);
            if (slicedToken.Length == 2)
            {
                var type = Enum.Parse<TokenScopeType>(slicedToken[0], true);
                var accessLevel = Enum.Parse<TokenScopeAccessLevel>(slicedToken[1], true);
                return new(type, accessLevel);
            }
            return null;
        }

        public static bool TryParse(string value, out TokenScope tokenScope)
        {
            var slicedToken = value.Split(Separator);
            if (slicedToken.Length == 2)
            {
                if (Enum.TryParse<TokenScopeType>(slicedToken[0], true, out var type) && Enum.TryParse<TokenScopeAccessLevel>(slicedToken[1], true, out var accessLevel))
                {
                    tokenScope = new(type, accessLevel);
                    return true;
                }
            }
            tokenScope = default;
            return false;
        }

        public override string ToString()
        {
            return (Type + Separator + AccessLevel).ToLower();
        }
    }
}
