using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace HnSF
{
    [System.Serializable]
    public class QuantumEventReceiverHitstopShake
    {
        private List<IDisposable> _disposableCallbacks = new List<IDisposable>();
        public QuantumEntityViewUpdater viewUpdater;

        private Dictionary<EventKey, BattleActorViewHitstopShake> _unconfirmedShakes = new();

        public void Initialize()
        {
            _disposableCallbacks.Add(
                QuantumCallback.SubscribeManual((CallbackEventCanceled c) => WhenEventCanceled(c)));
            _disposableCallbacks.Add(
                QuantumCallback.SubscribeManual((CallbackEventConfirmed c) => WhenEventConfirmed(c)));
            _disposableCallbacks.Add(QuantumEvent.SubscribeManual((EventCauseHitstopShake e) =>
                DoHitstopShake(e)));
        }

        public void Breakdown()
        {
            for (int i = 0; i < _disposableCallbacks.Count; i++)
            {
                _disposableCallbacks[i].Dispose();
            }

            _disposableCallbacks.Clear();
        }

        private void WhenEventConfirmed(CallbackEventConfirmed callback)
        {
            if (_unconfirmedShakes.ContainsKey(callback.EventKey))
            {
                _unconfirmedShakes.Remove(callback.EventKey);
            }
        }

        private void WhenEventCanceled(CallbackEventCanceled callback)
        {
            if (_unconfirmedShakes.ContainsKey(callback.EventKey))
            {
                _unconfirmedShakes[callback.EventKey].StopShake();
                _unconfirmedShakes.Remove(callback.EventKey);
            }
        }

        private void DoHitstopShake(EventCauseHitstopShake callback)
        {
            if (viewUpdater == null) viewUpdater = GameObject.FindAnyObjectByType<QuantumEntityViewUpdater>();

            EventKey eventKey = (EventKey)callback;
            var g = callback.Game;

            var entity = viewUpdater.GetView(callback.fighterEntity);
            if (entity == null)
            {
                Debug.LogError($"Could not find view of {callback.fighterEntity}");
                return;
            }
            
            entity.GetComponent<BattleActorViewHitstopShake>()
                .Shake((float)callback.shakeFrames / (float)callback.Game.Frames.Predicted.UpdateRate);
            _unconfirmedShakes.Add(eventKey, entity.GetComponent<BattleActorViewHitstopShake>());
        }
    }
}

