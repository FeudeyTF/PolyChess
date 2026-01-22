using PolyChess.Core.Commands.Parametrized;

namespace PolyChess.Components.Telegram.Commands
{
	internal class TelegramCommand : ParametrizedCommand<TelegramCommandExecutionContext>
	{
		public string Description { get; set; } = string.Empty;

		public bool IsAdmin { get; set; } = false;

		public bool IsHidden { get; set; } = false;

		public TelegramCommand(string name, string description, bool isAdmin, bool isHidden, object? invoker, Func<object?, object?[], object?> invokerMethod, List<ICommandParameter> parameters, List<ICommandParameter> optionalParameters, List<object?> optionalParametersDefaults) : base(name, invoker, invokerMethod, parameters, optionalParameters, optionalParametersDefaults)
		{
			Description = description;
			IsAdmin = isAdmin;
			IsHidden = isHidden;
		}

		public async override Task<bool> IsCommandRunable(TelegramCommandExecutionContext ctx)
		{
			if (IsAdmin && !ctx.Admins.Contains(ctx.User.Id))
			{
				await ctx.ReplyAsync("У вас нет прав на выполнение данной команды!");
				return false;
			}
			return true;
		}
	}
}
