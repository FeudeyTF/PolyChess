using PolyChess.Components.Telegram.Callback;
using PolyChess.Configuration;
using PolyChess.Core.Telegram;
using PolyChess.Core.Telegram.Messages;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChess.Components.Telegram.Handlers
{
	internal class QuestionHandler : ITelegramUpdateHandler
	{
		public UpdateType Type => UpdateType.Message;

		private readonly IMainConfig _mainConfig;

		public QuestionHandler(IMainConfig config)
		{
			_mainConfig = config;
		}

		public async Task<bool> HandleUpdate(ITelegramBotClient client, Update update, CancellationToken token)
		{
			var message = update.Message!;
			var user = message.From;
			if (user == null)
				return false;

			if (message.Chat.Id == _mainConfig.QuestionChannelId && message.ReplyToMessage != null && message.ReplyToMessage.ReplyMarkup != null)
			{
				if (message.ReplyToMessage.ReplyMarkup.InlineKeyboard.Any())
				{
					var inlineKeyBoard = message.ReplyToMessage.ReplyMarkup.InlineKeyboard.First();
					if (inlineKeyBoard.Any())
					{
						var dataButton = inlineKeyBoard.First();
						if (!string.IsNullOrEmpty(dataButton.CallbackData))
						{
							var data = TelegramCallbackQueryData.Parse(dataButton.CallbackData);
							if (data != null)
							{
								var userId = data.GetLongNumber("ID");
								var questionChannelID = data.GetNumber("ChannelID");
								if (userId != default && questionChannelID != default)
								{
									var msg = new TelegramMessageBuilder($"❗️Получен **ответ** на ваш вопрос от {user.FirstName} {user.LastName}:\n{message.Text}", parseMode: ParseMode.MarkdownV2)
										.ReplyTo(questionChannelID);
									await client.SendMessageAsync(msg, userId, token);
									return true;
								}
							}
						}
					}
				}
			}
			return false;
		}
	}
}
