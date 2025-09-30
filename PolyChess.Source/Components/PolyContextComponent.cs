using PolyChess.Components.Data;
using PolyChess.Core.Logging;

namespace PolyChess.Components
{
    internal class PolyContextComponent : IComponent
    {
        private readonly PolyContext _context;

        private readonly ILogger _logger;

        public PolyContextComponent(PolyContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            if (await _context.Database.CanConnectAsync())
            {
                _logger.Write("Соединение с базой данной прошло успешно!", LogLevel.Info);
            }
            else
            {
                _logger.Write("База данных не существует. Создание базы данных...", LogLevel.Info);
                await _context.Database.EnsureCreatedAsync();
                _logger.Write("База данных успешно создана.", LogLevel.Info);
            }
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            _logger.Write("Данные базы данных успешно освобождены", LogLevel.Info);
        }
    }
}
