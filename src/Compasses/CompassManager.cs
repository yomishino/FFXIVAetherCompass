using System.Collections.Generic;

namespace AetherCompass.Compasses
{
    public class CompassManager
    {
        private readonly HashSet<Compass> compasses = new();

        public bool AddCompass(Compass c)
            => compasses.Add(c);

        public void LoopAndCheckObjects()
        {
            foreach (var o in Plugin.ObjectTable)
            {
                foreach (var compass in compasses)
                {
                    if (compass.IsObjective(o))
                    {
                        // TODO: LoopAndCheckObjects
                        ;
                    }
                }

            }
        }
    }
}
