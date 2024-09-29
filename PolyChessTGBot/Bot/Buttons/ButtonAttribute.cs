namespace PolyChessTGBot.Bot.Buttons
{
    internal delegate Task ButtonDelegate(ButtonArgs args);
    
    [AttributeUsage(AttributeTargets.Method)]
    internal class ButtonAttribute : Attribute
    {
        public string ID;

        public ButtonAttribute(string id)
        {
            ID = id;
        }
    }
}
