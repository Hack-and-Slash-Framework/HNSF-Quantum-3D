using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace HnSF
{
    public unsafe class EntityViewTrackECB : MonoBehaviour
    {
        public QuantumEntityView customEntityView;

        protected List<DispatcherSubscription> quantumSubscriptions = new();

        public float smoothTime = 0.3F;
        protected Vector3 velocity = Vector3.zero;


        public Vector3 offset;
        public Vector3 currentLocation;
        public Vector3 locationSmoothed;
        protected bool smoothedSet;
        protected bool teleport;

        protected virtual void OnEnable()
        {
            quantumSubscriptions.Add(QuantumCallback.Subscribe(this,
                (CallbackUpdateView callback) => UpdateView(callback)));
            quantumSubscriptions.Add(QuantumEvent.Subscribe<EventTeleportCamera>(this, handler: (a) =>
            {
                if (a.callerEntity != customEntityView.EntityRef) return;
                teleport = true;
            }));
        }

        protected virtual void OnDisable()
        {
            foreach (var s in quantumSubscriptions)
            {
                QuantumCallback.Unsubscribe(s);
            }

            quantumSubscriptions.Clear();
        }

        protected virtual void UpdateView(CallbackUpdateView callback)
        {
            var game = callback.Game;
            //UpdateLocation(game);
        }

        public virtual void UpdateLocation(QuantumGame game)
        {
            if (!game.Frames.Predicted.Unsafe.TryGetPointer<Transform3D>(customEntityView.EntityRef, out var t3d)
                || !game.Frames.Predicted.Unsafe.TryGetPointer<KCC>(customEntityView.EntityRef, out var ecb)) return;

            var targetPos = t3d->Position.ToUnityVector3() +
                            t3d->TransformDirection(ecb->Data.PositionOffset).ToUnityVector3() +
                            new Vector3(0, ecb->Data.Height.AsFloat / 2.0f, 0) + offset;
            var lastPos = targetPos;

            if (game.Frames.PredictedPrevious.Unsafe.TryGetPointer<Transform3D>(customEntityView.EntityRef,
                    out var pt3d)
                && game.Frames.PredictedPrevious.Unsafe.TryGetPointer<KCC>(customEntityView.EntityRef, out var pecb))
            {
                lastPos = pt3d->Position.ToUnityVector3() +
                          pt3d->TransformDirection(pecb->Data.PositionOffset).ToUnityVector3() +
                          new Vector3(0, pecb->Data.Height.AsFloat / 2.0f, 0) + offset;
            }

            currentLocation = Vector3.Lerp(lastPos, targetPos, game.InterpolationFactor);

            if (!smoothedSet)
            {
                locationSmoothed = currentLocation;
                smoothedSet = true;
                return;
            }

            if (teleport)
            {
                locationSmoothed = currentLocation;
                teleport = false;
                return;
            }

            locationSmoothed = Vector3.SmoothDamp(locationSmoothed, currentLocation, ref velocity, smoothTime);
        }
    }
}