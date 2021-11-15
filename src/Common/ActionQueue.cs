using System;
using System.Collections.Generic;

namespace AetherCompass.Common
{
    public class ActionQueue
    {
        private readonly Queue<Action> actions;
        
        public int Threshold { get; }


        public ActionQueue(int threshold)
        {
            actions = new Queue<Action>(threshold);
            Threshold = threshold;
        }


        public bool QueueAction(Action? a, bool dequeueOldIfFull = false)
        {
            if (a == null) return false;
            if (EnqueueWithinThreshold(a)) return true;
            else if (dequeueOldIfFull && actions.TryDequeue(out _))
                return EnqueueWithinThreshold(a);
            return false;
        }

        private bool EnqueueWithinThreshold(Action? a)
        {
            if (a == null) return false;
            if (actions.Count >= Threshold) return false;
            actions.Enqueue(a);
            return true;
        }

        public void DoAll()
        {
            while (actions.TryDequeue(out var a ))
                a?.Invoke();
        }

        public void Clear() => actions.Clear();
    }
}
