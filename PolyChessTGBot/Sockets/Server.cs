using Fleck;
using PolyChessTGBot.Logs;

namespace PolyChessTGBot.Sockets
{
    internal class SocketServer
    {
        private readonly WebSocketServer Server;

        private readonly Dictionary<string, IWebSocketConnection> Clients;

        private readonly ILog Logger;

        public SocketServer(int port, ILog logger)
        {
            Server = new($"ws://0.0.0.0:{port}");
            Logger = logger;
            Clients = new();
        }

        public void StartListening()
        {
            Server.Start(socket =>
            {
                socket.OnOpen = () => OnSocketConnect(socket);

                socket.OnClose = () => OnSocketDisconnect(socket);
            });
            Logger.Write("WebSocket сервер был запущен!", LogType.Info);
        }

        private void OnSocketConnect(IWebSocketConnection connection)
        {
            Logger.Write($"Клиент '{connection.ConnectionInfo.Host}' присоединился к серверу!", LogType.Info);
            Clients.Add(connection.ConnectionInfo.Id.ToString(), connection);
        }

        private void OnSocketDisconnect(IWebSocketConnection connection)
        {
            Logger.Write($"Клиент '{connection.ConnectionInfo.Host}' отсоединился от сервера!", LogType.Info);
            Clients.Remove(connection.ConnectionInfo.Id.ToString());
        }

        public async Task SendMessage(string message)
        {
            foreach (var client in Clients)
                try
                {
                    await client.Value.Send(message);
                }
                catch
                {
                    Logger.Write($"Client {client.Value.ConnectionInfo.Host} disconnected due error", LogType.Error);
                    client.Value.Close();
                }
        }
    }
}
