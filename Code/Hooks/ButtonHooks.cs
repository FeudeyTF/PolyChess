using PolyChessTGBot.Bot.Buttons;

namespace PolyChessTGBot.Hooks
{
    internal static class ButtonHooks
    {
        public delegate Task ButtonInteractD(ButtonInteractArgs args);

        public static event ButtonInteractD OnButtonInteract;

        static ButtonHooks()
        {
            OnButtonInteract = (args) => Task.CompletedTask;
        }

        internal static void InvokeButtonInteract(ButtonInteractArgs args)
        {
            OnButtonInteract(args);
        }
    }
}
