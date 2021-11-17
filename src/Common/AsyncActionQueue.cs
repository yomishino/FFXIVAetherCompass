using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AetherCompass.Common
{
    // TODO: try this for looping objtects?
    // However, if we simply await separately, then except for the timeout thing
    // i dont think it adds much value because we need to wait for that long anyway
    // in total, and worse still it adds complexity to the implementation
    public class AsyncActionQueue
    {
        private readonly Queue<Action> actions;
        
        public int Threshold { get; }
        public int Count { get; private set; } = 0;

        public string temp = "";

        public AsyncActionQueue(int threshold)
        {
            Threshold = threshold;
            actions = new(threshold);
        }

        public bool QueueAction(Action? a, bool dequeueOldIfFull = false)
        {
            if (a == null) return false;
            if (Enqueue(a)) return true;
            else if (dequeueOldIfFull && TryDequeue(out _))
                return Enqueue(a);
            return false;
        }

        private bool Enqueue(Action? a)
        {
            if (a == null) return false;
            if (Count >= Threshold) return false;
            actions.Enqueue(a);
            Count++;
            return true;
        }

        private bool TryDequeue(out Action? a)
        {
            if (actions.TryDequeue(out a))
            {
                Count--;
                return true;
            }
            else return false;
        }

        // Be careful with the behaviour tho, things inside Action/delegate
        // will not get executed until invoked in some way, just like normal methods.
        // (Call to Task.Run counts as an invocation since it does invoke the method.)
        // So if we has some variable whose value will be used in the delegate,
        // it makes a difference whether we do the pre-computation inside or outside the delegate.
        // For example, if we want to queue the actions of printing 0 to 9 in Log, we have:
        //
        //  for (int i = 0; i < 10; i++)
        //  {
        //      var str = $"{i}";
        //      q1.QueueAction(() => Plugin.LogDebug(str));
        //      q2.QueueAction(() => Plugin.LogDebug($"{i}");
        //  }
        //
        // Because we use `str` inside the delegate which has the value of `i` evaluated before
        // passing in, so if we call `q1.DoAll`, will print 0 to 9, 
        // and will be in order if we set `inOrder` to true
        // and may be not if we set that to false.
        // However `q2` will most likely print 10 lines of "10",
        // because by the time the delegate gets invoked, the loop is already finished
        // and `i` already has the value 10.
        //
        // Unrelated notes; if you use Linq, e.g. var tasks = actions.Select(a=>Task.Run(a)),
        // remember that Linq is lazy, so everytime you use `tasks`, 
        // you risk reevaluating it and may generate a lot of redundant tasks.
        // One way to force evaluation is instead of using the Linq method returned values,
        // use ToList etc. to force everything to be evaluated, e.g.,
        // var tasks = actions.Select(a=>Task.Run(a)).ToList();
        //
        // Also, when "async void" method called, altho tasks inside the methods are awaited,
        // the "async void" method itself is not "waited", so in
        //  DoSomething1();
        //  DoAsync();
        //  DoSomething2();
        // part or all of things in DoAsync may get "skipped" and control would advanced to
        // DoSomething2 and execution continued, until later DoAsync eventually catch up.
        // In other words, we may go to DoSomething2 before DoAsync actually finished.
        // This is especially dangerous when using with ImGui.
        // One workaround is to make this method into Task to get the outside context
        // await it instead.
        // But also remember that the task can be dispatched to different threads,
        // but ImGui is not thread-safe and should not access ImGui thru multiple threads;
        // Even dispatch complete window drawing procedure to another thread will
        // make the window not rendered or watever.
        // So even if multithreading, dont do it with imgui itself.
    public async void DoAll(int timeoutInMs = -1, bool inOrder = false)
        {
            if (inOrder)
            {
                while (TryDequeue(out var a))
                    a?.Invoke();
            }
            else
            {
                // ToList because Linq is lazy; if we dont force evaluation everytime we get new things meh
                var tasks = actions.Select(a => Task.Run(a)).ToList();
                //var rt = await Task.WhenAny(Task.WhenAll(tasks), Task.Delay( timeoutInMs));
                var timeOutTask = Task.Delay(timeoutInMs);
                //var rt = await Task.WhenAny(Task.WhenAll(tasks), timeOutTask);
                var rt = await Task.WhenAny(Task.WhenAll(tasks), timeOutTask);
                Plugin.LogDebug($"rt={rt.Id}, {rt.Status}; isTimeoutTask?={rt.Id==timeOutTask.Id}");
                foreach(var t in tasks)
                {
                    Plugin.LogDebug($"{t.Id}, {t.Status}");
                }
                Clear();
                Plugin.LogDebug($"DoAll Done");
            }
        }

        public void Clear()
        {
            actions.Clear();
            Count = 0;
        }
    }
}
