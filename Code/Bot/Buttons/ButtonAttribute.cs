namespace PolyChessTGBot.Bot.Buttons
{
    internal delegate Task ButtonDelegate(ButtonInteractArgs args);
    
    [AttributeUsage(AttributeTargets.Method)]
    internal class ButtonAttribute(string id) : Attribute
    {
        public string ID = id;
    }
}
