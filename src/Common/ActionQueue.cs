using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AetherCompass.Common
{
    public class ActionQueue
    {
        private readonly ConcurrentQueue<DrawAction> importantActions;
        private readonly ConcurrentQueue<DrawAction> normalActions;
        
        public int Threshold { get; }
        public int Count { get; private set; } = 0;


        public ActionQueue(int threshold)
        {
            normalActions = new();
            importantActions = new();
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
            if (Count < Threshold || TryDequeueNormal(out _))
            {
                importantActions.Enqueue(a);
                Count++;
                AssertCount();
                return true;
            }
            return false;
        }

        private bool EnqueueNormal(DrawAction a)
        {
            if (Count < Threshold || TryDequeueNormal(out _))
            {
                normalActions.Enqueue(a);
                Count++;
                AssertCount();
                return true;
            }
            return false;
        }

        private bool TryDequeueImportant(out DrawAction? a)
        {
            if (importantActions.TryDequeue(out a))
            {
                Count--;
                AssertCount();
                return true;
            }
            return false;
        }

        private bool TryDequeueNormal(out DrawAction? a)
        {
            if (normalActions.TryDequeue(out a))
            {
                Count--;
                AssertCount();
                return true;
            }
            return false;
        }

        [Conditional("DEBUG")]
        private void AssertCount()
            => Debug.Assert(Count == normalActions.Count + importantActions.Count && Count <= Threshold);

        public void DoAll()
        {
            while (TryDequeueNormal(out var a))
                a?.Invoke();
            while (TryDequeueImportant(out var a))
                a?.Invoke();
        }

        public void Clear()
        {
            normalActions.Clear();
            importantActions.Clear();
            Count = 0;
        }
    }
}
