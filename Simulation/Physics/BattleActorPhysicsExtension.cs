using Photon.Deterministic;

namespace Quantum
{
    public unsafe partial struct BattleActorPhysics
    {
        public void SetExternalImpulse(Frame f, EntityRef entity, FPVector3 impulse)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                kcc->Data.ExternalCollisionImpulse = impulse;
            }
        }
        
        public FPVector3 GetOverallVelocity(Frame f, EntityRef entity)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                return kcc->Data.KinematicVelocity + kcc->Data.DynamicVelocity;
            }
            return force;
        }
        
        public void SetOverallVelocity(Frame f, EntityRef entity, FPVector3 velocity)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                kcc->SetKinematicVelocity(velocity.XOZ);
                kcc->Data.DynamicVelocity.Y = velocity.Y;
            }
            force = velocity;
        }

        public void ForceUngrounded(Frame f, EntityRef entityRef)
        {
            if(f.Unsafe.TryGetPointer<KCC>(entityRef, out var kcc))
            {
                kcc->Data.IsGrounded = false;
            }
        }

        public FPVector3 GetCenter(Frame f, EntityRef entity)
        {
            if (!f.Unsafe.TryGetPointer<Transform3D>(entity, out var transform)) return FPVector3.Zero;
            
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                return kcc->Position + transform->TransformDirection(kcc->Data.PositionOffset) + new FPVector3(0, kcc->Data.Height / FP._2, 0);
            }else if (f.Unsafe.TryGetPointer<PhysicsCollider3D>(entity, out var pc))
            {
                return transform->Position + transform->TransformDirection(pc->Shape.Centroid);
            }
            return transform->Position;
        }

        // KINEMATIC VELOCITY //
        public FPVector3 GetKinematicVelocity(Frame f, EntityRef entity)
        {
            return force;
            //if (!f.Unsafe.TryGetPointer<KCC>(entity, out var kcc)) return FPVector3.Zero;
            //return kcc->Data.KinematicVelocity;
        }
        
        public void SetKinematicVelocity(Frame f, EntityRef entity, FPVector3 velocity)
        {
            force = velocity;
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                kcc->SetKinematicVelocity(velocity);
            }
        }
        
        public FPVector3 GetKinematicHorizontalSpeed(Frame f, EntityRef entity)
        {
            /*
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                return kcc->Data.KinematicVelocity.XOZ;
            }*/
            return force.XOZ;
        }
        
        public void SetKinematicHorizontalSpeed(Frame f, EntityRef entity, FPVector3 value)
        {
            force.X = value.X;
            force.Z = value.Z;
            
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                kcc->Data.KinematicVelocity.X = value.X;
                kcc->Data.KinematicVelocity.Z = value.Z;
            }
            /*
            else
            {
                force.X = value.X;
                force.Z = value.Z;
            }*/
        }
        
        public FPVector3 GetKinematicVerticalSpeed(Frame f, EntityRef entity)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                return new FPVector3(0, kcc->Data.KinematicVelocity.Y, 0);
            }
            return new FPVector3(0, force.Y, 0);
        }
        
        public FP GetKinematicVerticalSpeedFP(Frame f, EntityRef entity)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                return kcc->Data.KinematicVelocity.Y;
            }
            return force.Y;
        }

        public void SetKinematicVerticalSpeed(Frame f, EntityRef entity, FP value)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                kcc->Data.KinematicVelocity.Y = value;
            }
        }
        
        // DYNAMIC VELOCITY //
        public FPVector3 GetDynamicVelocity(Frame f, EntityRef entity)
        {
            if (!f.Unsafe.TryGetPointer<KCC>(entity, out var kcc)) return FPVector3.Zero;
            return kcc->Data.DynamicVelocity;
        }
        
        public void SetDynamicVelocty(Frame f, EntityRef entity, FPVector3 velocity)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                kcc->SetDynamicVelocity(velocity);
            }
        }
        
        public FPVector3 GetDynamicVelocityHorizontalSpeed(Frame f, EntityRef entity)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                return kcc->Data.DynamicVelocity.XOZ;
            }
            return force.XOZ;
        }
        
        public FPVector3 GetDynamicVelocityVerticalSpeed(Frame f, EntityRef entity)
        {
            if (!f.Unsafe.TryGetPointer<KCC>(entity, out var kcc)) return FPVector3.Zero;
            return new FPVector3(0, kcc->Data.DynamicVelocity.Y, 0);
        }
        
        public FP GetDynamicVelocityVerticalSpeedFP(Frame f, EntityRef entity)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                return kcc->Data.DynamicVelocity.Y;
            }
            return force.Y;
        }
        
        public void SetDynamicVelocityVerticalSpeed(Frame f, EntityRef entity, FP value, bool asJumpForce = false)
        {
            if (f.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                if(asJumpForce) kcc->Jump(new FPVector3(0, value, 0));
                else kcc->Data.DynamicVelocity.Y = value;
            }
            else
            {
                force.Y = value;
            }
        }
    }
}