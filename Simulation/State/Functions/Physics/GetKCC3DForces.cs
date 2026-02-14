using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.functions
{
    [System.Serializable]
    public unsafe partial class GetKCC3DForces : StateFunctionFPVector3
    {
        public enum ForceType
        {
            All,
            KinematicVelocity,
            DynamicVelocity
        }

        public enum SplitType
        {
            All,
            XOnly,
            YOnly
        }

        public ForceType forceType;
        public SplitType splitType;
        public bool normalize;
        
        public override FPVector3 Execute(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            if (frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc3d))
            {
                var returnVal = FPVector3.Zero;
                switch (forceType)
                {
                    case ForceType.All:
                        returnVal = kcc3d->Data.DynamicVelocity + kcc3d->Data.KinematicVelocity;
                        break;
                    case ForceType.KinematicVelocity:
                        returnVal = kcc3d->Data.KinematicVelocity;
                        break;
                    case ForceType.DynamicVelocity:
                        returnVal = kcc3d->Data.DynamicVelocity;
                        break;
                }

                switch (splitType)
                {
                    case SplitType.All:
                        break;
                    case SplitType.XOnly:
                        returnVal.Y = 0;
                        break;
                    case SplitType.YOnly:
                        returnVal.X = 0;
                        break;
                }

                if (normalize && returnVal != FPVector3.Zero) returnVal = returnVal.Normalized;
                return returnVal;
            }
            return FPVector3.Zero;
        }

        public override HNSFStateFunction Copy()
        {
            return CopyTo(new GetKCC3DForces());
        }

        public override HNSFStateFunction CopyTo(HNSFStateFunction target)
        {
            var t = target as GetKCC3DForces;
            t.forceType = forceType;
            t.splitType = splitType;
            t.normalize = normalize;
            return base.CopyTo(target);
        }
    }
}

