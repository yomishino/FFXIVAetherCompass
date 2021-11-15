using System;

namespace AetherCompass.Common
{
    public class DrawAction
    {
        private readonly Action action;
        public bool Important { get; }

        public DrawAction(Action action, bool important = false)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            Important = important;
        }

        public void Invoke() => action.Invoke();

        public static implicit operator Action(DrawAction a) => a.action;
    }
}
