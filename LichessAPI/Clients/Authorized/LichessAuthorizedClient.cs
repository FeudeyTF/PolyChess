namespace LichessAPI.Clients.Authorized
{
    public class LichessAuthorizedClient : LichessApiClient
    {
        public readonly string QAuthToken;

        public LichessAuthorizedClient(string token)
        {
            QAuthToken = token;
        }
    }
}
