using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot
{
    public static partial class Utils
    {
        public static BotCommandScope GetScopeByType(BotCommandScopeType type)
        {
            return type switch
            {
                BotCommandScopeType.AllChatAdministrators => BotCommandScope.AllChatAdministrators(),
                BotCommandScopeType.AllGroupChats => BotCommandScope.AllGroupChats(),
                BotCommandScopeType.AllPrivateChats => BotCommandScope.AllPrivateChats(),
                _ => BotCommandScope.Default()
            };
        }
    }
}
