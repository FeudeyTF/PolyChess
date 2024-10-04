using PolyChessTGBot.Hooks;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Bot.Buttons
{
    internal class ButtonsRegistrator
    {
        public List<Button> Buttons;

        public ButtonsRegistrator()
        {
            Buttons = [];
            ButtonHooks.OnButtonInteract += HandleButtonInteract;
        }

        private async Task HandleButtonInteract(ButtonInteractArgs args)
        {
            foreach (var button in Buttons)
                if (args.ButtonID == button.ID)
                    await button.Delegate(args);
        }

        public void RegisterButtons(BindingFlags flags = BindingFlags.Public | BindingFlags.Static)
        => RegisterButtons(null, flags);

        public void RegisterButtons(object? obj, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            Type[] searchingTypes = [];
            if (obj == null)
                searchingTypes = Assembly.GetExecutingAssembly().GetTypes();
            else
                searchingTypes = [obj.GetType()];
            foreach (var type in searchingTypes)
                if (type != null)
                    foreach (var method in type.GetMethods(flags))
                    {
                        var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                        if (buttonAttribute == null)
                            continue;
                        ButtonDelegate? buttonDelegate = null;
                        buttonDelegate = (ButtonDelegate)Delegate.CreateDelegate(typeof(ButtonDelegate), obj, method);
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
