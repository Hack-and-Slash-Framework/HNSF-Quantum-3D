using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Modify Intertia")]
    public unsafe partial class ModifyInertia : HNSFStateAction
    {
        public enum ModifyType
        {
            Conserve,
            Release,
            Clear
        }

        public ModifyType modifyType;
        [DrawIf(nameof(modifyType), (int)ModifyType.Release)] public bool useYInertia;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            var physics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
            KCC* kcc = frame.Unsafe.GetPointer<KCC>(entity);
            var transform = frame.Unsafe.GetPointer<Transform3D>(entity);

            switch (modifyType)
            {
                case ModifyType.Conserve:
                    physics->conservedIntertia = transform->InverseTransformDirection(kcc->Data.KinematicVelocity + kcc->Data.DynamicVelocity);
                    break;
                case ModifyType.Release:
                    if (useYInertia)
                    {
                        var yTemp = physics->conservedIntertia.Y;
                        physics->conservedIntertia.Y = 0;
                        physics->SetKinematicVelocity(frame, entity, transform->TransformDirection(physics->conservedIntertia));
                        physics->SetDynamicVelocty(frame, entity, new FPVector3(0, yTemp, 0));
                    }
                    else
                    {
                        physics->conservedIntertia.Y = 0;
                        physics->SetKinematicVelocity(frame, entity, transform->TransformDirection(physics->conservedIntertia));
                    }
                    break;
                case ModifyType.Clear:
                    physics->conservedIntertia = FPVector3.Zero;
                    break;
            }
            return false;
        }
    }
}