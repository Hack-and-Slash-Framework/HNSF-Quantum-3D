using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Apply Slope Movement")]
    public unsafe partial class ApplySlopeMovement : HNSFStateAction
    {
        public AssetRef<AnimationCurveAsset> rampCurveRef;
        public LayerMask layerMask;
        public HNSFParamFP minimumAngle;
        public HNSFParamFP maximumAngle;
        public HNSFParamFP minimumSpeed;
        public HNSFParamFP maximumSpeed;
        public HNSFParamFP traction;
        public HNSFParamFP AdjustHorizontal;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            var physics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
            KCC* kcc = frame.Unsafe.GetPointer<KCC>(entity);
            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(entity);
            var bufferCam = frame.Unsafe.GetPointer<ActorInputCamera>(entity);
            var bufferMovement = frame.Unsafe.GetPointer<ActorInputBufferMovement>(entity);
            AnimationCurveAsset curve = frame.FindAsset<AnimationCurveAsset>(rampCurveRef.Id);

            var raycast = frame.Physics3D.Raycast(
                transform->Position + FPVector3.Up,
                FPVector3.Down,
                FP.FromRaw(78643),
                layerMask,
                QueryOptions.HitStatics | QueryOptions.ComputeDetailedInfo);

            if (!raycast.HasValue) return false;

            var left = FPVector3.Cross(raycast.Value.Normal, FPVector3.Up);
            var slope = FPVector3.Cross(raycast.Value.Normal, left);
            var rot = FPQuaternion.LookRotation(slope);

            var mLocal = bufferCam->GetMovementVector(0, bufferMovement->GetMovement(0), true)
                .InverseTransformDirection(rot);
            mLocal.Z = 0;
            mLocal.Y = 0;
            mLocal.X *= AdjustHorizontal.Resolve(frame, entity, ref stateContext);
            mLocal = mLocal.Normalized.TransformDirection(rot);
            
            
            var forward = slope + mLocal;
            forward.Y = 0;
            forward = forward.Normalized;

            var angle = FPVector3.Angle(forward, slope);

            var minAngle = minimumAngle.Resolve(frame, entity, ref stateContext);
            var maxAngle = maximumAngle.Resolve(frame, entity, ref stateContext);
            var tract = traction.Resolve(frame, entity, ref stateContext);
            var minSpeed = minimumSpeed.Resolve(frame, entity, ref stateContext);
            var maxSpeed = maximumSpeed.Resolve(frame, entity, ref stateContext);
            
            if (angle < minAngle)
            {
                kcc->SetKinematicVelocity(FPVector3.MoveTowards(kcc->Data.KinematicVelocity, FPVector3.Zero, tract * frame.DeltaTime));
                return false;
            }
            
            angle -= minAngle;
            maxAngle -= minAngle;

            var wantedForce = forward * FPMath.Lerp(minSpeed, maxSpeed, curve.animationCurve.Evaluate(angle / maxAngle));

            kcc->SetKinematicVelocity(FPVector3.MoveTowards(kcc->Data.KinematicVelocity, wantedForce, tract * frame.DeltaTime));
            return false;
        }
        
        public override HNSFStateAction Copy()
        {
            return CopyTo(new ApplySlopeMovement());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ApplySlopeMovement;
            t.rampCurveRef = rampCurveRef;
            t.layerMask = layerMask;
            t.minimumAngle = minimumAngle.Clone() as HNSFParamFP;
            t.maximumAngle = maximumAngle.Clone() as HNSFParamFP;
            t.minimumSpeed = minimumSpeed.Clone() as HNSFParamFP;
            t.maximumSpeed = maximumSpeed.Clone() as HNSFParamFP;
            t.traction = traction.Clone() as HNSFParamFP;
            t.AdjustHorizontal = AdjustHorizontal.Clone() as HNSFParamFP;
            return base.CopyTo(target);
        }
    }
}