using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AetherCompass.Compasses.Objectives
{
    public unsafe class MobHunCachedCompassObjective : CachedCompassObjective
    {
        public readonly NMRank Rank;
        public readonly bool IsSSMinion;

        public MobHunCachedCompassObjective(GameObject* obj, NMRank rank, bool isHostile)
            : base (obj)
        {
            Rank = rank;
            IsSSMinion = rank == NMRank.B && isHostile;
        }

        public string GetExtendedRank() =>
            IsSSMinion ? $"{Rank} - SS Minion" : $"{Rank}";
    }
}
