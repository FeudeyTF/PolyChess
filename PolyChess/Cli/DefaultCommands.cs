using Microsoft.EntityFrameworkCore.Internal;
using PolyChess.Cli.Commands;
using PolyChess.Components.Data;
using PolyChess.LichessAPI.Clients;

namespace PolyChess.Cli
{
#pragma warning disable CS1998, CA1822
	internal class DefaultCommands : CliCommandAggregator
	{
		private readonly PolyContext _polyContext;

		private readonly LichessClient _lichessClient;

		public DefaultCommands(PolyContext db, LichessClient lichessClient)
		{
			_polyContext = db;
			_lichessClient = lichessClient;
		}

		[CliCommand("exit")]
		public async Task Exit(CliCommandExecutionContext ctx)
		{
			ctx.SendMessage("Программа закрывается...");
			Environment.Exit(0);
		}

		[CliCommand("fixlichessid")]
		public async Task FixLichessId(CliCommandExecutionContext ctx)
		{
			ctx.SendMessage("Выполняется исправление LichessId в записях базы данных");

			foreach (var student in _polyContext.Students)
			{
				if(student.LichessId == null)
					continue;
				var user = await _lichessClient.GetUserAsync(student.LichessId);
				if(user == null)
					continue;
				if (user.Username != student.LichessId)
					student.LichessId = user.Username;
				ctx.SendMessage($"Исправлен LichessId студента: {student.Name} {student.Surname}. Новый Id: {student.LichessId}");
			}

			await _polyContext.SaveChangesAsync();

			ctx.SendMessage("Все Id успешно исправлены и обновлены");
		}

		[CliCommand("getlessons")]
		public async Task GetLessons(CliCommandExecutionContext ctx)
		{
			ctx.SendMessage($"Всего занятий: {_polyContext.Lessons.Count()}");
			foreach(var lesson in _polyContext.Lessons)
			{
				ctx.SendMessage($"Занятие: #{lesson.Id}: с {lesson.StartDate} до {lesson.EndDate}. Обязательное: {(lesson.IsRequired ? "да" : "нет")}. Место: {lesson.Latitude}, {lesson.Longitude}");
			}
		}
	}
#pragma warning restore CS1998, CA1822
}
