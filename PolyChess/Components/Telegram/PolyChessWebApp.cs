using Microsoft.AspNetCore.StaticFiles;

namespace PolyChess.Components.Telegram
{
	internal class PolyChessWebApp
	{
		private readonly WebApplication _app;

		public PolyChessWebApp(WebApplication app)
		{
			_app = app;
		}

		public void Start()
		{
			FileExtensionContentTypeProvider provider = new()
			{
				Mappings = { [".tgs"] = "application/x-tgsticker" }
			};
			_app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });

			_app.UseRouting();
			_app.MapRazorPages();
		}
	}
}
