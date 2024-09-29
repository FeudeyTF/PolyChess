namespace PolyChessTGBot.Bot.Buttons
{
    internal class Button
    {
        public string ID;

        public ButtonDelegate Delegate;

        public Button(string id, ButtonDelegate handler)
        {
            ID = id;
            Delegate = handler;
        }
    }
}
