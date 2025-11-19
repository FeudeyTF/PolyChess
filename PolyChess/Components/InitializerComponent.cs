using PolyChess.Core.Logging;
using System.Diagnostics;

namespace PolyChess.Components
{
    internal class InitializerComponent : IComponent
    {
        private readonly IEnumerable<IComponent> _components;

        private readonly ILogger _logger;

        public InitializerComponent(IEnumerable<IComponent> components, ILogger logger)
        {
            _components = components;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            Stopwatch stopwatch = new();
            foreach (var component in _components)
            {
                var name = component.GetType().Name;
                _logger.Info($"Инициализация компонента: {name}...");
                try
                {
                    stopwatch.Start();
                    await component.StartAsync();
                    stopwatch.Stop();
                }
                catch (Exception e)
                {
                    _logger.Error($"При инициализация компонента: {name} произошла ошибка: {e}!");
                    continue;
                }

                _logger.Info($"Компонент {name} успешно инициализирован за {stopwatch.Elapsed.TotalSeconds} с!");
                stopwatch.Reset();
            }
        }

        public async Task DisposeAsync()
        {
            foreach (var component in _components)
                await component.DisposeAsync();
        }
    }
}
