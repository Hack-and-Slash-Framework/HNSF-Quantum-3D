using System;
using HnSF.core.state;
using Quantum;

namespace HnSF
{
    [Serializable]
    public unsafe partial struct DirectHitInfo
    {
        public EntityRef attackerEntityRef;
        public EntityRef defenderEntityRef;
        public AssetRef<HitInfoBase> hitInfoRef;
        public int hitboxId;
        public int hitHurtboxId;
        public bool checkForStateChange;
        public AssetRef<HNSFState> hitByState;
        public uint hitByStateIdentifier;
        public bool releaseDefenderFromThrow;
    }
}