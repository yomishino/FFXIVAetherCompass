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

        public static DrawAction? Combine(bool important, params DrawAction?[] drawActions)
        {
            if (drawActions.Length == 0) return null;
            return Delegate.Combine(Array.ConvertAll(drawActions, drawAction => drawAction == null ? null : (Action)drawAction)) 
                is not Action combined ? null : new(combined, important);
        }

        public static DrawAction? Combine(params DrawAction?[] drawActions)
            => Combine(false, drawActions);
    }
}
