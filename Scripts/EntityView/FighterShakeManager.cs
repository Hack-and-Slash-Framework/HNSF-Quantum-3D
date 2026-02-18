using Quantum;
using UnityEngine;

namespace HnSF
{
    public class FighterShakeManager : MonoBehaviour
    {
        protected DispatcherSubscription _updateViewDispatcher;
        public QuantumEntityView view;

        public Transform shaker;

        public float currentShakeLength = 0;
        protected float timer = 0;

        public float shakeSpeed = 1.0f;
        public float shakeAmt = 1.0f;
        public AnimationCurve shakeCurve;

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
            if (timer < currentShakeLength)
            {
                var t = timer / currentShakeLength;
                var shakeVal = Mathf.Sin(Time.time * shakeSpeed) * shakeAmt;

                var vector3 = shaker.transform.localPosition;
                vector3.x = Mathf.Lerp(shakeVal, 0.0f, shakeCurve.Evaluate(t));
                vector3.z = Mathf.Lerp(shakeVal, 0.0f, shakeCurve.Evaluate(t));
                shaker.transform.localPosition = vector3;

                timer += Time.deltaTime;

                if (timer >= currentShakeLength) shaker.transform.localPosition = Vector3.zero;
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