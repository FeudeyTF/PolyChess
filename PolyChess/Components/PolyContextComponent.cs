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
                _logger.Info("Соединение с базой данной прошло успешно!");
            }
            else
            {
                _logger.Info("База данных не существует. Создание базы данных...");
                await _context.Database.EnsureCreatedAsync();
                _logger.Info("База данных успешно создана.");
            }
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            _logger.Info("Данные базы данных успешно освобождены");
        }
    }
}
