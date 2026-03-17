using Quantum;
using UnityEngine;

namespace HnSF
{
    public unsafe class BattleActorViewHitstopShake : MonoBehaviour
    {
        protected DispatcherSubscription _updateViewDispatcher;
        public QuantumEntityView view;

        public Transform shaker;

        public float currentShakeLength = 0;
        protected float timer = 0;

        public float shakeSpeed = 1.0f;
        public float shakeAmt = 1.0f;
        public AnimationCurve shakeCurve;

        protected int lastShakeFrame = 0;

        protected virtual void OnEnable()
        {
            _updateViewDispatcher =
                QuantumCallback.Subscribe(this, (CallbackUpdateView callback) => UpdateView(callback));
        }

        protected virtual void OnDisable()
        {
            QuantumCallback.Unsubscribe(_updateViewDispatcher);
        }

        protected virtual void UpdateView(CallbackUpdateView callback)
        {
            bool shouldYShake = false;

            if (callback.Game.Frames.Predicted.Unsafe.TryGetPointer<BattleActorPhysics>(view.EntityRef,
                    out var charaPhysics))
            {
                shouldYShake = charaPhysics->currentGroundedState == StateGroundedType.AERIAL;
            }

            if (callback.Game.Frames.Predicted.Unsafe.TryGetPointer<LastHitByInfo>(view.EntityRef,
                    out var lastHitByInfo)
                && lastHitByInfo->lastHitOnFrame == callback.Game.Frames.Predicted.Number
                && lastShakeFrame != lastHitByInfo->lastHitOnFrame)
            {
                lastShakeFrame = lastHitByInfo->lastHitOnFrame;
                Shake(lastHitByInfo->lastHitstopAmount * (1.0f / 60.0f));
            }

            if (timer < currentShakeLength)
            {
                var t = timer / currentShakeLength;
                var shakeVal = Mathf.Sin(Time.time * shakeSpeed) * shakeAmt;
                var shakezVal = Mathf.Sin((Time.time + 0.5f) * shakeSpeed) * shakeAmt;
                var shakeYVal = shouldYShake ? Mathf.Cos(Time.time * shakeSpeed) * shakeAmt : shakeVal;

                var vector3 = shaker.transform.localPosition;
                vector3.x = Mathf.Lerp(shakeVal, 0.0f, shakeCurve.Evaluate(t));
                vector3.z = Mathf.Lerp(shakezVal, 0.0f, shakeCurve.Evaluate(t));
                if (shouldYShake) vector3.y = Mathf.Lerp(shakeYVal, 0.0f, shakeCurve.Evaluate(t));
                shaker.transform.localPosition = vector3;

                timer += Time.deltaTime;

                if (timer >= currentShakeLength) StopShake();
            }
        }

        public virtual void Shake(float shakeLength)
        {
            currentShakeLength = shakeLength;
            timer = 0;
        }

        public virtual void StopShake()
        {
            currentShakeLength = 0;
            timer = currentShakeLength;
            shaker.transform.localPosition = Vector3.zero;
        }
    }
}