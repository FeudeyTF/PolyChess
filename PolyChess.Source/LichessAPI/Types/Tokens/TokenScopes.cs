namespace LichessAPI.Types.Tokens
{
    public class TokenScopes
    {
        public static readonly TokenScopeContainer Preferences = new(TokenScopeType.Preference);

        public static readonly TokenScopeContainer Email = new(TokenScopeType.Email);

        public static readonly TokenScopeContainer Engine = new(TokenScopeType.Engine);

        public static readonly TokenScopeContainer Bot = new(TokenScopeType.Bot);

        public static readonly TokenScopeContainer Web = new(TokenScopeType.Web);

        public static readonly TokenScopeContainer Board = new(TokenScopeType.Board);

        public static readonly TokenScopeContainer Study = new(TokenScopeType.Study);

        public static readonly TokenScopeContainer Message = new(TokenScopeType.Msg);

        public static readonly TokenScopeContainer Follow = new(TokenScopeType.Follow);

        public static readonly TokenScopeContainer Puzzle = new(TokenScopeType.Puzzle);

        public static readonly TokenScopeContainer Racer = new(TokenScopeType.Racer);

        public static readonly TokenScopeContainer Challenge = new(TokenScopeType.Challenge);

        public static readonly TokenScopeContainer Tournament = new(TokenScopeType.Tournament);
    }
}
