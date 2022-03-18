using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;

namespace AetherCompass.Compasses.Objectives
{
    public unsafe class MobHunCachedCompassObjective : CachedCompassObjective
    {
        public NMRank Rank { get; private set; }
        public bool IsSSMinion { get; private set; }

        public MobHunCachedCompassObjective(GameObject* obj, NMRank rank, bool isHostile)
            : base (obj)
        {
            Init(rank, isHostile);
        }

        public MobHunCachedCompassObjective(ObjectInfo* info, NMRank rank, bool isHostile)
            : base(info)
        {
            Init(rank, isHostile);
        }

        private void Init(NMRank rank, bool isHostile)
        {
            Rank = rank;
            IsSSMinion = rank == NMRank.B && isHostile;
        }

        public string GetExtendedRank() =>
            IsSSMinion ? $"{Rank} - SS Minion" : $"{Rank}";
    }
}
