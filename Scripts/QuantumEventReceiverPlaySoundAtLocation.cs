using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace HnSF
{
    [System.Serializable]
    public class QuantumEventReceiverPlaySoundAtLocation
    {
        private Dictionary<EventKey, (EntitySoundManager, GameAudioSource)> _unconfirmedSounds = new();
        private List<IDisposable> _disposableCallbacks = new List<IDisposable>();

        public EntitySoundManager globalManager;

        public QuantumEntityViewUpdater viewUpdater;

        public Dictionary<AudioSourceConfig, ObjectPool<GameAudioSource>> audioSourcePools = new();

        public void Initialize()
        {
            _disposableCallbacks.Add(
                QuantumCallback.SubscribeManual((CallbackEventCanceled c) => WhenEventCanceled(c)));
            _disposableCallbacks.Add(
                QuantumCallback.SubscribeManual((CallbackEventConfirmed c) => WhenEventConfirmed(c)));
            _disposableCallbacks.Add(QuantumEvent.SubscribeManual((EventPlaySoundAtLocation e) => PlaySoundEvent(e)));
        }

        public void Teardown()
        {
            for (int i = 0; i < _disposableCallbacks.Count; i++)
            {
                _disposableCallbacks[i].Dispose();
            }

            _disposableCallbacks.Clear();
        }

        private void WhenEventCanceled(CallbackEventCanceled callback)
        {
            if (!_unconfirmedSounds.ContainsKey(callback.EventKey)) return;
            _unconfirmedSounds[callback.EventKey].Item1.StopSound(_unconfirmedSounds[callback.EventKey].Item2);
            _unconfirmedSounds.Remove(callback.EventKey);
        }

        private void WhenEventConfirmed(CallbackEventConfirmed callback)
        {
            if (_unconfirmedSounds.ContainsKey(callback.EventKey))
            {
                _unconfirmedSounds.Remove(callback.EventKey);
            }
        }

        private void PlaySoundEvent(EventPlaySoundAtLocation callback)
        {
            if (viewUpdater == null)
                viewUpdater = GameObject.FindAnyObjectByType<QuantumEntityViewUpdater>();

            EventKey eventKey = (EventKey)callback;

            var g = callback.Game;

            var parentEntity = viewUpdater.GetView(callback.parentedTo);
            var soundPosition = callback.position.ToUnityVector3();

            var soundEntryAsset = QuantumUnityDB.GetGlobalAsset<SoundEntry>(callback.sound.Id);
            var audioSourceConfigAsset =
                QuantumUnityDB.GetGlobalAsset<AudioSourceConfig>(callback.audioSourceConfig.Id);

            EntitySoundManager ownerSoundManager;
            var aso = GetPooledAudioSource(audioSourceConfigAsset);
            if (aso == null) return;
            aso.transform.position = soundPosition;
            aso.config = audioSourceConfigAsset;

            if (callback.isGlobal)
            {
                ownerSoundManager = globalManager;
                globalManager.audioPool = audioSourcePools;
                globalManager.PlaySound(
                    aso,
                    soundEntryAsset,
                    parentEntity?.gameObject,
                    soundPosition,
                    (g.Frames.Predicted.Number - callback.Tick) * Time.fixedDeltaTime,
                    callback.volume.AsFloat,
                    Random.Range(callback.minPitch.AsFloat, callback.maxPitch.AsFloat),
                    callback.tag, audioSourceConfigAsset, eventKey,
                    callback.cancelOthersSoundEntry, callback.cancelOthersTag,
                    callback.ignoreIfSoundPlaying, callback.ignoreIfTagPlaying);
            }
            else
            {
                var entity = viewUpdater.GetView(callback.owner);
                if (!entity) return;
                ownerSoundManager = entity.gameObject.GetComponent<EntitySoundManager>();
                ownerSoundManager.audioPool = audioSourcePools;
                ownerSoundManager.PlaySound(
                    aso,
                    soundEntryAsset,
                    parentEntity?.gameObject,
                    soundPosition,
                    (g.Frames.Predicted.Number - callback.Tick) * Time.fixedDeltaTime,
                    callback.volume.AsFloat,
                    Random.Range(callback.minPitch.AsFloat, callback.maxPitch.AsFloat),
                    callback.tag, audioSourceConfigAsset, eventKey,
                    callback.cancelOthersSoundEntry, callback.cancelOthersTag,
                    callback.ignoreIfSoundPlaying, callback.ignoreIfTagPlaying);
            }

            if (aso) _unconfirmedSounds.Add(eventKey, (ownerSoundManager, aso));
        }

        private GameAudioSource GetPooledAudioSource(AudioSourceConfig sourceConfigAsset)
        {
            if (sourceConfigAsset == null) return null;

            if (!audioSourcePools.ContainsKey(sourceConfigAsset))
            {
                audioSourcePools.Add(sourceConfigAsset, new ObjectPool<GameAudioSource>(
                    createFunc: () => GameObject.Instantiate(sourceConfigAsset.prefab).GetComponent<GameAudioSource>(),
                    actionOnGet: (ve) => { ve.gameObject.SetActive(true); },
                    actionOnRelease:
                    (ve) =>
                    {
                        ve.audioSource.Stop();
                        ve.audioSource.clip = null;
                        ve.gameObject.SetActive(false);
                    },
                    actionOnDestroy: (ve) =>
                    {
                        if (ve == null) return;
                        GameObject.Destroy(ve.gameObject);
                    },
                    collectionCheck: false,
                    defaultCapacity: 10,
                    maxSize: 30
                ));
            }

            return audioSourcePools[sourceConfigAsset].Get();
        }
    }
}