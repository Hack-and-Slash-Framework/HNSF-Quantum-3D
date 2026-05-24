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
        [Serializable]
        public class SliceGrouping
        {
            public List<GameAudioSource> sounds = new();
            
            public Dictionary<SoundEntry, List<GameAudioSource>> soundsByEntry = new();
            public Dictionary<AssetRef<Tag>, List<GameAudioSource>> soundsByTag = new();

            public bool TryGetTagSoundRecentlyPlayed(AssetRef<Tag> tag, out GameAudioSource audioSource)
            {
                audioSource = null;

                if (!soundsByTag.TryGetValue(tag, out var gameAudioSources))
                    return false;

                foreach (var gameAudioSource in gameAudioSources)
                {
                    if (gameAudioSource.audioSource.time <= 0.02f)
                    {
                        gameAudioSource.audioSource.Stop();
                        audioSource = gameAudioSource;
                        return true;
                    }
                }

                return false;
            }

            public bool TryGetTagSoundHittingLimit(AssetRef<Tag> tag, int limit, out GameAudioSource audioSource)
            {
                var lowestTime = float.MaxValue;
                audioSource = null;
                
                if (!soundsByTag.TryGetValue(tag, out var gameAudioSources))
                    return false;

                if (gameAudioSources.Count < limit)
                    return false;
                
                foreach (var gameAudioSource in gameAudioSources)
                {
                    if(gameAudioSource.audioSource.time >= lowestTime)
                        continue;
                    audioSource = gameAudioSource;
                    lowestTime = gameAudioSource.audioSource.time;
                }

                return audioSource != null;
            }

            public void AddAudioSource(GameAudioSource audioSource)
            {
                var soundEntry = audioSource.soundEntry;
                var tag = audioSource.soundEntry.tag;
                
                if(!soundsByEntry.ContainsKey(soundEntry))
                    soundsByEntry.Add(soundEntry, new List<GameAudioSource>());
                
                if(!soundsByTag.ContainsKey(tag))
                    soundsByTag.Add(tag, new List<GameAudioSource>());
                
                sounds.Add(audioSource);
                soundsByEntry[soundEntry].Add(audioSource);
                soundsByTag[tag].Add(audioSource);
            }

            public void RemoveAudioSource(GameAudioSource audioSource)
            {
                var soundEntry = audioSource.soundEntry;
                var tag = audioSource.soundEntry.tag;
                
                soundsByEntry[soundEntry].Remove(audioSource);
                soundsByTag[tag].Remove(audioSource);
                sounds.Remove(audioSource);
            }
        }
        
        private Dictionary<EventKey, (EntitySoundManager, GameAudioSource)> _unconfirmedSounds = new();
        private List<IDisposable> _disposableCallbacks = new List<IDisposable>();

        public EntitySoundManager globalManager;

        public QuantumEntityViewUpdater viewUpdater;

        public Dictionary<AudioSourceConfig, ObjectPool<GameAudioSource>> audioSourcePools = new();

        public Dictionary<Vector3Int, SliceGrouping> groups = new();
        public float sliceValue = 10;
        public int defaultLimitPerTag = 3;
        
        public virtual void Initialize()
        {
            _disposableCallbacks.Add(
                QuantumCallback.SubscribeManual((CallbackEventCanceled c) => WhenEventCanceled(c)));
            _disposableCallbacks.Add(
                QuantumCallback.SubscribeManual((CallbackEventConfirmed c) => WhenEventConfirmed(c)));
            _disposableCallbacks.Add(QuantumEvent.SubscribeManual((EventPlaySoundAtLocation e) => PlaySoundEvent(e)));
        }

        public virtual void Teardown()
        {
            for (int i = 0; i < _disposableCallbacks.Count; i++)
            {
                _disposableCallbacks[i].Dispose();
            }

            _disposableCallbacks.Clear();
        }

        protected virtual void WhenEventCanceled(CallbackEventCanceled callback)
        {
            if (!_unconfirmedSounds.ContainsKey(callback.EventKey)) return;
            // TODO: Cancel sound.
            //_unconfirmedSounds[callback.EventKey].Item1.StopSound(_unconfirmedSounds[callback.EventKey].Item2);
            //_unconfirmedSounds.Remove(callback.EventKey);
        }

        protected virtual void WhenEventConfirmed(CallbackEventConfirmed callback)
        {
            if (_unconfirmedSounds.ContainsKey(callback.EventKey))
            {
                _unconfirmedSounds.Remove(callback.EventKey);
            }
        }
        
        protected virtual Vector3Int ConvertToSlice(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / sliceValue),
                Mathf.FloorToInt(position.y / sliceValue),
                Mathf.FloorToInt(position.z / sliceValue)
            );
        }

        protected virtual List<Vector3Int> ConvertToSlices(Vector3 position, float radius)
        {
            int minX = Mathf.FloorToInt((position.x - radius) / sliceValue);
            int maxX = Mathf.FloorToInt((position.x + radius) / sliceValue);
            int minY = Mathf.FloorToInt((position.y - radius) / sliceValue);
            int maxY = Mathf.FloorToInt((position.y + radius) / sliceValue);
            int minZ = Mathf.FloorToInt((position.z - radius) / sliceValue);
            int maxZ = Mathf.FloorToInt((position.z + radius) / sliceValue);

            List<Vector3Int> slices = new List<Vector3Int>();

            for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            for (int z = minZ; z <= maxZ; z++)
                slices.Add(new Vector3Int(x, y, z));

            return slices;
        }
        
        protected virtual void UnregisterGameAudioSource(GameAudioSource audioSource)
        {
            foreach (var slice in audioSource.inSlices)
            {
                if (!groups.TryGetValue(slice, out var group))
                    continue;

                group.RemoveAudioSource(audioSource);
            }
            audioSource.inSlices.Clear();
        }

        protected virtual void RegisterGameAudioSource(GameAudioSource audioSource)
        {
            var slices = ConvertToSlices(audioSource.transform.position, audioSource.audioSource.minDistance);
            audioSource.inSlices = slices;

            foreach (var slice in slices)
            {
                if(!groups.ContainsKey(slice))
                    groups.Add(slice, new SliceGrouping());
                
                groups[slice].AddAudioSource(audioSource);
            }
        }
        
        protected virtual GameAudioSource RegisterAndGetBestAudioSource(Vector3 soundStartPosition, float soundRadius, SoundEntry soundEntry, AudioSourceConfig sourceConfig)
        {
            Vector3Int mainSlice = ConvertToSlice(soundStartPosition);

            if(!groups.ContainsKey(mainSlice))
                groups.Add(mainSlice, new SliceGrouping());
            
            var group = groups[mainSlice];

            GameAudioSource gotAudioSource = null;
            
            if(group.TryGetTagSoundRecentlyPlayed(soundEntry.tag, out gotAudioSource) || group.TryGetTagSoundHittingLimit(soundEntry.tag, defaultLimitPerTag, out gotAudioSource))
            {
                UnregisterGameAudioSource(gotAudioSource);
                if (gotAudioSource.owner != null)
                {
                    gotAudioSource.owner.StopSound(gotAudioSource, release: false);
                }
            }else
            {
                gotAudioSource = GetPooledAudioSource(sourceConfig);
                gotAudioSource.soundEntry = soundEntry;
                gotAudioSource.config = sourceConfig;
                gotAudioSource.audioSource.minDistance = sourceConfig.defaultMinDistance;
                gotAudioSource.audioSource.maxDistance = sourceConfig.defaultMaxDistance;
            }

            gotAudioSource.transform.position = soundStartPosition;

            RegisterGameAudioSource(gotAudioSource);

            return gotAudioSource;
        }
        
        protected virtual void PlaySoundEvent(EventPlaySoundAtLocation callback)
        {
            EventKey eventKey = (EventKey)callback;
            var g = callback.Game;

            var ownerEntity = viewUpdater.GetView(callback.owner);
            var parentEntity = viewUpdater.GetView(callback.parentedTo);
            var soundPosition = callback.position.ToUnityVector3();

            var soundEntryAsset = QuantumUnityDB.GetGlobalAsset<SoundEntry>(callback.sound.Id);
            var audioSourceConfigAsset =
                QuantumUnityDB.GetGlobalAsset<AudioSourceConfig>(callback.audioSourceConfig.Id);

            var gas = RegisterAndGetBestAudioSource(soundPosition, callback.minDistance.AsFloat, soundEntryAsset, audioSourceConfigAsset);
            if (gas == null)
                return;
            
            EntitySoundManager ownerSoundManager;

            if (callback.isGlobal || ownerEntity == null)
            {
                ownerSoundManager = globalManager;
                ownerSoundManager.audioPool = audioSourcePools;
                
                gas.owner = ownerSoundManager;
                globalManager.PlaySound(
                    gas,
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
                ownerSoundManager = ownerEntity.GetComponent<EntitySoundManager>();
                ownerSoundManager.audioPool = audioSourcePools;
                
                gas.owner = ownerSoundManager;
                ownerSoundManager.PlaySound(
                    gas,
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
            
            if (gas) _unconfirmedSounds.Add(eventKey, (ownerSoundManager, gas));
        }

        protected virtual GameAudioSource GetPooledAudioSource(AudioSourceConfig sourceConfigAsset)
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
                        UnregisterGameAudioSource(ve);
                        ve.audioSource.Stop();
                        ve.audioSource.clip = null;
                        ve.gameObject.SetActive(false);
                    },
                    actionOnDestroy: (ve) =>
                    {
                        if (ve == null) return;
                        UnregisterGameAudioSource(ve);
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