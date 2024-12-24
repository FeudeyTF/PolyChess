using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Commands.Basic;
using PolyChessTGBot.Bot.Commands.Discrete;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Bot.Messages.Discrete;
using PolyChessTGBot.Extensions;
using System.Diagnostics;
using System.Reflection;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        public static readonly string TempPath;

        static BotCommands()
        {
            TempPath = Path.Combine(Environment.CurrentDirectory, "Temp");
        }

        [Command("info", "Отправляет информацию о боте", true)]
        private async Task Version(CommandArgs args)
        {
            string exeFilePath = Path.Combine(
                Environment.CurrentDirectory,
                Assembly.GetExecutingAssembly().GetName().Name + ".exe");
            List<string> message =
            [
                "🛠<b>Информация о боте</b>🛠",
                $"👨🏻‍💻<b>Разработчики:</b> {Program.MainConfig.BotAuthor}",
                $"🔀<b>Версия бота:</b> v.{FileVersionInfo.GetVersionInfo(exeFilePath).FileVersion}",
                $"🕐<b>Дата последнего обновления:</b> {File.GetLastWriteTime(exeFilePath):g}",
                $"⏱<b>Время работы:</b> {DateTime.Now - Program.Started:%d' дн. '%h' ч. '%m' мин. '%s' сек.'}"
            ];
            await args.Reply(string.Join("\n", message));
        }

        [DiscreteCommand("task", "Отправляет выполненное творческое задание Павлу", ["Отправьте сообщение с файлом с выполненным заданием"])]
        private async Task SendCreativeTask(CommandArgs<Message> args)
        {
            if (args.Parameters.Count == 1)
            {
                var msg = args.Parameters[0];
                if(msg.Document != null)
                {
                    var user = Program.Data.GetUser(args.User.Id);
                    if(user != null)
                    {
                        if (!user.CreativeTaskCompleted)
                        {
                            List<string> text = ["Пришло выполненное творческое задание!"];
                            text.Add($"Студент: <b>{user.Name}</b>");
                            text.Add($"Курс: <b>{user.Year}</b>");
                            text.Add($"Сообщение от студента:");
                            text.Add($"<b>{msg.Text ?? msg.Caption ?? "Студент не отправлял текстового сообщения"}</b>");
                            TelegramMessageBuilder message = new(string.Join("\n", text));
                            message.WithFile(msg.Document.FileId);

                            InlineKeyboardButton approveButton = new("✅ Принять");
                            approveButton.SetData("CreativeTaskApprove", ("ID", user.TelegramID));
                            InlineKeyboardButton declineButton = new("❌ Отклонить");
                            declineButton.SetData("CreativeTaskDecline", ("ID", user.TelegramID));

                            message.AddKeyboard([approveButton, declineButton]);

                            await args.Bot.SendMessage(message, Program.MainConfig.CreativeTaskChannel);
                            await args.Reply("Творческое задание было успешно отправлено!");
                        }
                        else
                            await args.Reply("Вы уже выполнили творческое задание!");
                    }
                    else
                        await args.Reply("Вас нет в системе!");
                }
                else
                    await args.Reply("Вы не прикрепили файл к сообщению!");
            }
            else
                await args.Reply("Вы не отправили сообщение!");
        }

        [Button("CreativeTaskApprove")]
        private async Task CreativeTaskApprove(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
            {
                var telegramID = args.GetLongNumber("ID");
                var user = Program.Data.GetUser(telegramID);
                if (user != null)
                {
                    user.CreativeTaskCompleted = true;
                    Program.Data.Query($"UPDATE Users SET CreativeTaskCompleted='1' WHERE TelegramID='{telegramID}'");
                    await args.Bot.SendMessage($"Ваше творческое задание было <b>принято</b>! Поздравляю, вы - <b>молодец</b>", telegramID);
                  
                    TelegramMessageBuilder builder = new("[ПРИНЯТО]\n" + (args.Query.Message.Text ?? args.Query.Message.Caption));
                    if(args.Query.Message.Document != null)
                        builder.WithFile(args.Query.Message.Document.FileId);

                    await args.Bot.EditMessage(builder, args.Query.Message.Chat.Id, args.Query.Message);
                    await args.Reply($"Вы успешно <b>приняли</b> задание студента <b>{user.Name}</b>");
                }
                else
                    await args.Reply("Студент не был найден!");
            }
        }

        [Button("CreativeTaskDecline")]
        private async Task CreativeTaskDecline(ButtonInteractArgs buttonArgs)
        {
            if (buttonArgs.Query.Message != null)
                await buttonArgs.SendDiscreteMessage(
                    buttonArgs.Query.Message.Chat.Id,
                    ["Введите причину отклонения"],
                    OnCreativeTaskMessageEntered,
                    data: buttonArgs.GetLongNumber("ID")
                );

            async Task OnCreativeTaskMessageEntered(DiscreteMessageEnteredArgs args)
            {
                if(args.Responses.Length == 1 && buttonArgs.Query.Message != null)
                {
                    var telegramID = (long)args.Data[0];
                    var user = Program.Data.GetUser(telegramID);
                    if (user != null)
                    {
                        TelegramMessageBuilder builder = new("[ПРИНЯТО]\n" + (buttonArgs.Query.Message.Text ?? buttonArgs.Query.Message.Caption));
                        if (buttonArgs.Query.Message.Document != null)
                            builder.WithFile(buttonArgs.Query.Message.Document.FileId);
                        
                        await args.Bot.EditMessage(builder, buttonArgs.Query.Message.Chat.Id, buttonArgs.Query.Message);
                        await args.Bot.SendMessage($"Ваше творческое задание <b>было отклонено</b> по причине:\n{args.Responses[0].Text}", telegramID);
                        await args.Reply($"Вы успешно <b>отклонили</b> задание студента <b>{user.Name}</b>");
                    }
                    else
                        await args.Reply("Студент не был найден!");
                }
            }
        }
    }
}