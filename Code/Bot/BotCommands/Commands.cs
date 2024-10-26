using PolyChessTGBot.Bot.Commands;
using System.Diagnostics;
using System.Reflection;
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

        [Command("version", "Отправляет информацию о боте", true)]
        private async Task Version(CommandArgs args)
        {
            string exeFilePath = Path.Combine(
                Environment.CurrentDirectory,
                Assembly.GetExecutingAssembly().GetName().Name + ".exe");
            List<string> message =
            [
                "🛠<b>Информация о боте</b>🛠",
                $"👨🏻‍💻<b>Разработчик:</b> {Program.MainConfig.BotAuthor}",
                $"🔀<b>Версия бота:</b> v.{FileVersionInfo.GetVersionInfo(exeFilePath).FileVersion}",
                $"🕐<b>Дата последнего обновления:</b> {File.GetLastWriteTime(exeFilePath):g}",
                $"⏱<b>Время работы:</b> {DateTime.Now - Program.Started:%d' дн. '%h' ч. '%m' мин. '%s' сек.'}"
            ];
            await args.Reply(string.Join("\n", message));
        }
    }
}