using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Commands.Basic;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Bot.Messages.Discrete;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;
using File = System.IO.File;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        private readonly List<TelegramMessageBuilder> _testMessages = [];

        private readonly List<long> _graduatedUsers = []; 

        private bool _isTestRunning = false;

        private readonly List<string> _testTable = [];

        [Command("start_test", "Включает возможность проходить тест", admin: true)]
        private async Task StartTest(CommandArgs args)
        {
            _isTestRunning = !_isTestRunning;
            _graduatedUsers.Clear();
            if (_isTestRunning)
            {
                _testTable.Clear();
                _testTable.Add(string.Join(',', ["ID", "Ник", "Имя", .. Program.MainConfig.TestFiles.Select(t => Program.MainConfig.TestFiles.IndexOf(t) + 1), "Всего"]));
            }
            else
            {
                TelegramMessageBuilder message = new("Результаты теста");
                if (!Directory.Exists(TempPath))
                    Directory.CreateDirectory(TempPath);
                var csvFilePath = Path.Combine(TempPath, "test_result.csv");
                if (File.Exists(csvFilePath))
                    File.Delete(csvFilePath);
                using (var streamWriter = new StreamWriter(File.Create(csvFilePath), Encoding.UTF8))
                {
                    foreach (var entry in _testTable)
                        streamWriter.WriteLine(entry);
                    streamWriter.Close();
                }
                var stream = File.Open(csvFilePath, FileMode.Open);
                message.WithFile(stream, "Table.csv");
                await args.Reply(message);
                _testTable.Clear();
            }

            await args.Reply($"Вы {(_isTestRunning ? "включили" : "выключили")} возможность проходить тест!");
        }

        [Command("test", "Запустит тест", true)]
        private async Task TestCommand(CommandArgs args)
        {
            if(!_isTestRunning)
            {
                await args.Reply("В данный момент нельзя начать проходить тест!");
                return;
            }

            if(_testMessages.Count == 0)
            {
                await args.Reply("Вопросы для теста отсутсвуют. Обратитесь в организатору");
                return;
            }

            if (_graduatedUsers.Contains(args.User.Id))
            {
                await args.Reply("Вы уже прошли тест!");
                return;
            }

            await args.Reply("Вы начали проходить входной тест! Отвечайте тестовым сообщением, содержащим один вариант ответа, например, f4f5");
            await DiscreteMessage.Send(args.Message.Chat.Id, _testMessages, HandleTestEntered, args.Token);
        }

        private async Task HandleTestEntered(DiscreteMessageEnteredArgs args)
        {
            if(_graduatedUsers.Contains(args.User.Id))
            {
                await args.Reply("Вы уже прошли тест!");
                return;
            }

            List<string> csv = [args.User.Id.ToString(), args.User.Username, args.User.FirstName + " " + args.User.LastName];

            var correctAnswersCount = 0;
            for (int i = 0; i < args.Responses.Length; i++)
            {
                var answer = args.Responses[i].Text;
                if(answer == null)
                {
                    await args.Reply($"Ваш ответ #{i + 1} не содержал текстового ответа! Перепройдите тест");
                    return;
                }

                var option = Program.MainConfig.TestFiles[i];
                var correctAnswer = option.Options.FirstOrDefault(o => o.IsCorrect);
                if(correctAnswer == null)
                {
                    await args.Reply($"Вопрос #{i + 1} неправильно создан! Пожалуйста, обратитесь к организатору");
                    return;
                }

                if (correctAnswer.Value.Equals(answer, StringComparison.CurrentCultureIgnoreCase))
                {
                    csv.Add("1");
                    correctAnswersCount++;
                }
                else
                    csv.Add("0");
            }
            csv.Add(correctAnswersCount.ToString());

            _graduatedUsers.Add(args.User.Id);
            _testTable.Add(string.Join(',', csv));
            await args.Reply("Ваш результат был успешно учтён! Ожидайте конца теста");
        }
    }
}
