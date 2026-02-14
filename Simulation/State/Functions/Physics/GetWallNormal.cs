using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.functions
{
    [System.Serializable]
    public unsafe partial class GetWallNormal : StateFunctionFPVector3
    {
        public bool removeY;

        public override FPVector3 Execute(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            var gotWallInfo = frame.Unsafe.GetPointer<GotWallInfo>(entity);
            var nor = gotWallInfo->wallNormal;
            if (removeY)
            {
                nor.Y = 0;
                nor = nor.Normalized;
            }
            return nor;
        }

        public override HNSFStateFunction Copy()
        {
            return CopyTo(new GetWallNormal());
        }

        public override HNSFStateFunction CopyTo(HNSFStateFunction target)
        {
            var t = target as GetWallNormal;
            t.removeY = removeY;
            return base.CopyTo(target);
        }
    }
}