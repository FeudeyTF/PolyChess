using System.Reflection;
using PolyChessTGBot.Bot.Buttons;

namespace PolyChessTGBot.Bot
{
    internal class ButtonsRegistrator
    {
        public List<Button> Buttons;

        public ButtonsRegistrator()
        {
            Buttons = new();
        }

        public void RegisterButtons(BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            foreach(var type in Assembly.GetExecutingAssembly().GetTypes())
                if(type != null)
                    foreach (var method in type.GetMethods(flags))
                    {
                        var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                        if (buttonAttribute == null)
                            continue;
                        ButtonDelegate? buttonDelegate = null;
                        buttonDelegate = (ButtonDelegate)Delegate.CreateDelegate(typeof(ButtonDelegate), null, method);
                        if (buttonDelegate != null)
                        {
                            var button = new Button(buttonAttribute.ID, buttonDelegate);
                            var equals = Buttons.Where(c => c.ID == button.ID);
                            if (equals.Any())
                                Buttons.Remove(equals.First());
                            Buttons.Add(button);
                        }
                    }
        }
    }
}
