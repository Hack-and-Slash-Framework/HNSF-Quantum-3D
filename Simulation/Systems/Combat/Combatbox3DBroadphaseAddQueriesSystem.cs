using System.Collections.Generic;
using Quantum;
using Quantum.Profiling;

namespace HnSF.core.systems
{
    public unsafe partial class Combatbox3DBroadphaseAddQueriesSystem : SystemMainThread
    {
        public enum CombatboxType
        {
            hitbox,
            hurtbox,
            collisionbox,
            throwbox
        }

        public override void OnInit(Frame f)
        {
            base.OnInit(f);
            f.Context.GetMasks(f);
        }
        
        public override void Update(Frame f)
        {
            f.Context.ClearFrameCombatResolutionVariables();
            f.Signals.CombatboxResolvingPreBroadphase();
            EventReceiverHelper.CallEvent(f, (int)EventReceiverTyping.PreCombatboxBroadphase);
            
            HostProfiler.Start("Combatbox_PARTITION_BROADPHASE_QUERIES");
            {
                HostProfiler.Start("BUILD FILTERS");
                var hitboxFilter = f.Filter<Hitbox, Transform3D, PhysicsCollider3D>();
                var collisionboxFilter = f.Filter<Collisionbox, Transform3D, PhysicsCollider3D>();
                var throwboxFilter = f.Filter<Throwbox, Transform3D, PhysicsCollider3D>();
                HostProfiler.End();

                HostProfiler.Start("Do Filters");
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
                HostProfiler.End();
            }
            HostProfiler.End();
        }

        private void DoQuery(Frame frame, ref EntityRef entityRef, Transform3D* boxTransform, PhysicsCollider3D* boxCollider, CombatboxType boxType)
        {
            QueryOptions queryOptions;
            Quantum.LayerMask layerMask;
            List<FrameContextUser.EntityToPhysicsQuery> queryList;
            switch (boxType)
            {
                case CombatboxType.hitbox:
                    layerMask = frame.Context.hitboxLayerMask;
                    queryList = frame.Context.HitboxBroadphaseQueries;
                    break;
                case CombatboxType.hurtbox:
                    layerMask = frame.Context.hurtboxLayerMask;
                    queryList = frame.Context.HurtboxBroadphaseQueries;
                    break;
                case CombatboxType.collisionbox:
                    layerMask = frame.Context.collisionboxLayerMask;
                    queryList = frame.Context.CollisionboxBroadphaseQueries;
                    break;
                case CombatboxType.throwbox:
                    layerMask = frame.Context.throwboxLayerMask;
                    queryList = frame.Context.ThrowboxBroadphaseQueries;
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
