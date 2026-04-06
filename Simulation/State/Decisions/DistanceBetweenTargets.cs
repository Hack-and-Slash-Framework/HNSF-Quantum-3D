using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.decisions
{
    [Serializable]
    public unsafe partial class DistanceBetweenTargets : HNSFStateDecision
    {
        public HNSFParamEntityRef targetAParam;
        public HNSFParamEntityRef targetBParam;
        public HNSFParamFP minDistanceParam;
        
        public override bool Decide(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            var targetAEntityRef = targetAParam.Resolve(frame, entity, ref stateContext);
            if (!frame.Exists(targetAEntityRef)) return false;
            var targetBEntityRef = targetBParam.Resolve(frame, entity, ref stateContext);
            if (!frame.Exists(targetBEntityRef)) return false;
            
            var minDistance = minDistanceParam.Resolve(frame, entity, ref stateContext);
            return FPVector3.DistanceSquared(TransformHelpers.GetCenterOfMass(frame, targetAEntityRef), TransformHelpers.GetCenterOfMass(frame, targetBEntityRef)) <= (minDistance * minDistance);
        }
        
        public override HNSFStateDecision Copy()
        {
            return CopyTo(new DistanceBetweenTargets());
        }

        public override HNSFStateDecision CopyTo(HNSFStateDecision target)
        {
            var t = target as DistanceBetweenTargets;
            t.targetAParam = targetAParam.Clone() as HNSFParamEntityRef;
            t.targetBParam = targetBParam.Clone() as HNSFParamEntityRef;
            t.minDistanceParam = minDistanceParam.Clone() as HNSFParamFP;
            return base.CopyTo(target);
        }
    }
}