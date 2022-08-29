using System.Collections.Concurrent;

namespace AetherCompass.Common
{
    public class ActionQueue
    {
        private readonly BlockingCollection<DrawAction> importantActions;
        private readonly BlockingCollection<DrawAction> normalActions;
        
        public int Threshold { get; }
        // count may be inaccurate 
        public int Count => importantActions.Count + normalActions.Count;


        public ActionQueue(int threshold)
        {
            normalActions = new(threshold);
            importantActions = new(threshold);
            Threshold = threshold > 0 ? threshold 
                : throw new ArgumentOutOfRangeException(nameof(threshold), "threshold should be positive");
        }


        public bool QueueAction(Action? a, bool important = false)
            => a != null && QueueAction(new(a, important));

        public bool QueueAction(DrawAction? a)
        {
            if (a == null) return false;
            if (a.Important) return EnqueueImportant(a);
            else return EnqueueNormal(a);
        }

        private bool EnqueueImportant(DrawAction a)
        {
            if (!importantActions.TryAdd(a)) return false;
            // Try make space by dequeueing normal
            // if important queue still has space but total bound reached
            // This is approximate tho
            if (Count >= Threshold && !TryDequeueNormal(out _)) return false;
            return importantActions.TryAdd(a);
        }

        private bool EnqueueNormal(DrawAction a) => normalActions.TryAdd(a);

        private bool TryDequeueImportant(out DrawAction? a) => importantActions.TryTake(out a);

        private bool TryDequeueNormal(out DrawAction? a) => normalActions.TryTake(out a);

        public void DoAll()
        {
            while (TryDequeueNormal(out var a))
                a?.Invoke();
            while (TryDequeueImportant(out var a))
                a?.Invoke();
        }

        public void Clear()
        {
            while (normalActions.TryTake(out var _)) ;
            while (importantActions.TryTake(out var _)) ;
        }
    }
}
