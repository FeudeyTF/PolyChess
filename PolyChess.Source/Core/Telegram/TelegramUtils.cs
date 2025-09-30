using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChess.Core.Telegram
{
    internal static class TelegramUtils
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
