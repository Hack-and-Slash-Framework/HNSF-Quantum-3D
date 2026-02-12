using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Physics/ECB/Set ECB")]
    public unsafe partial class SetECB : HNSFStateAction
    {
        public HNSFParamFP radiusParam;
        public HNSFParamFP heightParam;
        public HNSFParamFPVector3 offsetParam;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                var radius = radiusParam.Resolve(frame, entity, ref stateContext);
                var height = heightParam.Resolve(frame, entity, ref stateContext);
                var offset = offsetParam.Resolve(frame, entity, ref stateContext);

                //kcc->Data.Radius
                kcc->Data.Height = height;
                kcc->Data.PositionOffset = offset;
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SetECB());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SetECB;
            t.radiusParam = radiusParam.Clone() as HNSFParamFP;
            t.heightParam = heightParam.Clone() as HNSFParamFP;
            t.offsetParam = offsetParam.Clone() as HNSFParamFPVector3;
            return base.CopyTo(target);
        }
    }
}