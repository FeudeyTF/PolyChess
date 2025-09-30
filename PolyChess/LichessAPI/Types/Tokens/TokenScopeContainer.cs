namespace LichessAPI.Types.Tokens
{
    public class TokenScopeContainer
    {
        public TokenScope Write;

        public TokenScope Read;

        public TokenScope Bulk;

        public TokenScope Play;

        public TokenScope Mod;

        internal TokenScopeContainer(TokenScopeType type)
        {
            Write = new(type, TokenScopeAccessLevel.Write);
            Read = new(type, TokenScopeAccessLevel.Read);
            Bulk = new(type, TokenScopeAccessLevel.Bulk);
            Play = new(type, TokenScopeAccessLevel.Play);
            Mod = new(type, TokenScopeAccessLevel.Mod);
        }
    }
}
