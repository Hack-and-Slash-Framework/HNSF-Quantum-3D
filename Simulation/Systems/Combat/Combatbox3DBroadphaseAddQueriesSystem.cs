using System.Collections.Generic;
using Quantum;

namespace HnSF.core.systems
{
    public unsafe partial class Combatbox3DBroadphaseAddQueriesSystem : SystemMainThread
    {
        public enum CombatboxType
        {
            hitbox,
            hurtbox,
            collisionbox,
            throwbox,
            telegraphbox
        }

        public override void OnInit(Frame f)
        {
            base.OnInit(f);
        }
        
        public override void Update(Frame f)
        {
            f.Context.ClearFrameCombatResolutionVariables();
            f.Signals.CombatboxResolvingPreBroadphase();
            EventReceiverHelper.CallEvent(f, (int)EventReceiverTyping.PreCombatboxBroadphase);
            
            {
                var hitboxFilter = f.Filter<Hitbox, Transform3D, PhysicsCollider3D>();
                var collisionboxFilter = f.Filter<Collisionbox, Transform3D, PhysicsCollider3D>();
                var throwboxFilter = f.Filter<Throwbox, Transform3D, PhysicsCollider3D>();
                var telegraphboxFilter = f.Filter<Telegraphbox, Transform3D, PhysicsCollider3D>();
                
                while (hitboxFilter.NextUnsafe(out var entityRef, out _, out var transform, out var physicsCollider3D))
                {
                    DoQuery(f, ref entityRef, transform, physicsCollider3D, CombatboxType.hitbox);
                }

                while (collisionboxFilter.NextUnsafe(out var entityRef, out _, out var transform,
                           out var physicsCollider3D))
                {
                    DoQuery(f, ref entityRef, transform, physicsCollider3D, CombatboxType.collisionbox);
                }
                
                while (throwboxFilter.NextUnsafe(out var entityRef, out _, out var transform,
                           out var physicsCollider3D))
                {
                    DoQuery(f, ref entityRef, transform, physicsCollider3D, CombatboxType.throwbox);
                }
                
                while (telegraphboxFilter.NextUnsafe(out var entityRef, out _, out var transform,
                           out var physicsCollider3D))
                {
                    DoQuery(f, ref entityRef, transform, physicsCollider3D, CombatboxType.telegraphbox);
                }
            }
        }
        protected virtual void DoQuery(Frame frame, ref EntityRef entityRef, Transform3D* boxTransform, PhysicsCollider3D* boxCollider, CombatboxType boxType)
        {
            var simConfig = frame.SimulationConfig;
            
            QueryOptions queryOptions;
            Quantum.LayerMask layerMask;
            List<FrameContextUser.EntityToPhysicsQuery> queryList;
            switch (boxType)
            {
                case CombatboxType.hitbox:
                    layerMask = (1 << simConfig.layerHitbox) | (1 << simConfig.layerHurtbox);
                    queryList = frame.Context.HitboxBroadphaseQueries;
                    break;
                case CombatboxType.hurtbox:
                    layerMask = 1 << simConfig.layerHitbox;
                    queryList = frame.Context.HurtboxBroadphaseQueries;
                    break;
                case CombatboxType.collisionbox:
                    layerMask = 1 << simConfig.layerCollisionbox;
                    queryList = frame.Context.CollisionboxBroadphaseQueries;
                    break;
                case CombatboxType.throwbox:
                    layerMask = 1 << simConfig.layerHurtbox;
                    queryList = frame.Context.ThrowboxBroadphaseQueries;
                    break;
                case CombatboxType.telegraphbox:
                    layerMask = 1 << simConfig.layerHurtbox;
                    queryList = frame.Context.TelegraphboxBroadphaseQueries;
                    break;
                default:
                    return;
            }

            queryOptions = QueryOptions.HitAll | QueryOptions.ComputeDetailedInfo;

            PhysicsQueryRef queryRef = frame.Physics3D.AddOverlapShapeQuery(boxTransform->Position, boxTransform->Rotation, boxCollider->Shape, layerMask,
                options: queryOptions);

            FrameContextUser.EntityToPhysicsQuery entityAndQuery = new FrameContextUser.EntityToPhysicsQuery()
            {
                entityRef = entityRef,
                queryRef = queryRef
            };
            
            queryList.Add(entityAndQuery);
        }
    }
}
