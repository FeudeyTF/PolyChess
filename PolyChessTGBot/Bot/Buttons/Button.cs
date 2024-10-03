namespace PolyChessTGBot.Bot.Buttons
{
    internal class Button(string id, ButtonDelegate handler)
    {
        public string ID = id;

        public ButtonDelegate Delegate = handler;
    }
}
