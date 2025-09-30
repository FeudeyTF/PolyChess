namespace PolyChess.Components
{
    internal interface IComponent
    {
        public Task StartAsync();

        public Task DisposeAsync();
    }
}
