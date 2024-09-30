namespace PolyChessTGBot.Bot.Buttons
{
    internal delegate Task ButtonDelegate(ButtonInteractArgs args);
    
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
