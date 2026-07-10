using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;

namespace HnSF
{
    public class AnimationEntryBakedData : AssetObject
    {
        [System.Serializable]
        public struct BakedEntry : IEquatable<BakedEntry>
        {
            public string name;
            public AssetRef<Tag> tag;
            public AnimationFrameListWithParam[] paramFrameLists;

            public void Bake()
            {
            }

            public void AddOrSet(AnimationFrameListWithParam newFrameListWithParam)
            {
                for (var index = 0; index < paramFrameLists.Length; index++)
                {
                    if (paramFrameLists[index].param != newFrameListWithParam.param)
                        continue;
                    paramFrameLists[index] = newFrameListWithParam;
                    return;
                }

                Array.Resize(ref paramFrameLists, paramFrameLists.Length + 1);
                paramFrameLists[^1] = newFrameListWithParam;
            }

            public bool TryGetForParam(FPVector2 param, out AnimationFrameListWithParam afl)
            {
                afl = default;
                for (int i = 0; i < paramFrameLists.Length; i++)
                {
                    if (paramFrameLists[i].param == param)
                    {
                        afl = paramFrameLists[i];
                        return true;
                    }
                }

                return false;
            }

            public int GetIndexOfParam(FPVector2 param)
            {
                for (int i = 0; i < paramFrameLists.Length; i++)
                {
                    if (paramFrameLists[i].param == param) return i;
                }

                return -1;
            }

            public bool TryGetIndexOfParam(FPVector2 param, out int index)
            {
                index = -1;
                for (int i = 0; i < paramFrameLists.Length; i++)
                {
                    if (paramFrameLists[i].param == param)
                    {
                        index = i;
                        return true;
                    }
                }

                return false;
            }

            public bool Equals(BakedEntry other)
            {
                return tag.Equals(other.tag) && paramFrameLists.Equals(other.paramFrameLists);
            }

            public override bool Equals(object obj)
            {
                return obj is BakedEntry other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(tag, paramFrameLists);
            }
        }
        
        [NonSerialized] public Dictionary<AssetRef<Tag>, BakedEntry> boneTagToEntry = null;
        
        public string ClipName;
        public int MotionId;
        public FP Length;
        public int Index;
        public int FrameRate;
        public int FrameCount;
        public List<BakedEntry> BakedEntries = new List<BakedEntry>();
        
        public bool LoopTime;
        public bool Mirror;
        public bool DisableRootMotion;

        public BakedEntry GetEntry(AssetRef<Tag> tag)
        {
            if(boneTagToEntry == null) BuildEntries();
            return boneTagToEntry[tag];
        }

        public BakedEntry GetEntrySlow(AssetRef<Tag> tag)
        {
            for (int i = 0; i < BakedEntries.Count; i++)
            {
                if(BakedEntries[i].tag == tag) return BakedEntries[i];
            }
            return default;
        }

        public bool AddOrUpdateEntry(AssetRef<Tag> boneTag, AnimationFrameListWithParam[] frames, string entryName)
        {
            for (int i = 0; i < BakedEntries.Count; i++)
            {
                if (BakedEntries[i].tag != boneTag) continue;

                BakedEntries[i] = new BakedEntry()
                {
                    name = entryName,
                    tag = boneTag,
                    paramFrameLists = frames
                };
                return true;
            }

            BakedEntries.Add(new BakedEntry()
            {
                name = entryName,
                tag = boneTag,
                paramFrameLists = frames
            });
            return true;
        }

        public bool AddOrUpdateEntry(AssetRef<Tag> boneTag, AnimationFrameListWithParam paramFrameList,
            string entryName)
        {
            for (int i = 0; i < BakedEntries.Count; i++)
            {
                if (BakedEntries[i].tag != boneTag) continue;

                var temp = BakedEntries[i];

                temp.AddOrSet(paramFrameList);

                BakedEntries[i] = temp;
                return true;
            }

            BakedEntries.Add(new BakedEntry()
            {
                name = entryName,
                tag = boneTag,
                paramFrameLists = new[] { paramFrameList }
            });
            return true;
        }

        void BuildEntries()
        {
            if (boneTagToEntry != null) return;
            boneTagToEntry = new Dictionary<AssetRef<Tag>, BakedEntry>();

            foreach (var be in BakedEntries)
            {
                be.Bake();
                boneTagToEntry.Add(be.tag, be);
            }
        }
        
        public AnimationFrame CalculateDelta(AssetRef<Tag> tag, FP lastTime, FP currentTime)
        {
            BuildEntries();
            if (boneTagToEntry.ContainsKey(tag) == false) return default;
            var currentFrame = GetFrameAtTime(currentTime, tag); 
            var lastFrame = GetFrameAtTime(lastTime, tag);
            if (lastTime > currentTime)
            {
                var excessFrame  = GetFrameAtTime(Length, tag) - lastFrame;
                return excessFrame - currentFrame;
            }
            return lastFrame - currentFrame;
        }

        public bool TryGetFrameAtTime(FP time, AssetRef<Tag> tag, out AnimationFrame frame)
        {
            frame = default;
            BuildEntries();
            if (boneTagToEntry.ContainsKey(tag) == false) return false;
            frame = GetFrameAtTime(time, tag);
            return true;
        }
        
        public bool TryGetClosestFrame(FP time, AssetRef<Tag> boneTag, out AnimationFrame frame)
        {
            frame = default;
            BuildEntries();
            if (boneTagToEntry.ContainsKey(boneTag) == false) return false;

            if (time > Length)
            {
                frame = boneTagToEntry[boneTag].paramFrameLists[0].Frames[^1];
                return true;
            }

            int timeIndex = FrameCount - 1;
            for (int f = 1; f < FrameCount; f++)
            {
                if (boneTagToEntry[boneTag].paramFrameLists[0].Frames[f].Time > time)
                {
                    frame = boneTagToEntry[boneTag].paramFrameLists[0].Frames[f];
                    return true;
                }
            }

            return false;
        }

        public AnimationFrame GetFrameAtTime(FP time, AssetRef<Tag> boneTag)
        {
            BuildEntries();
            if (boneTagToEntry.ContainsKey(boneTag) == false) return default;
            AnimationFrame output = new AnimationFrame(FPQuaternion.Identity);
            if (Length == FP._0)
                return boneTagToEntry[boneTag].paramFrameLists[0].Frames[0];

            while (time > Length)
            {
                time -= Length;
                output += boneTagToEntry[boneTag].paramFrameLists[0].Frames[FrameCount - 1];
            }


            int timeIndex = FrameCount - 1;
            for (int f = 1; f < FrameCount; f++)
            {
                if (boneTagToEntry[boneTag].paramFrameLists[0].Frames[f].Time > time)
                {
                    timeIndex = f;
                    break;
                }
            }

            AnimationFrame frameA = boneTagToEntry[boneTag].paramFrameLists[0].Frames[timeIndex - 1];
            AnimationFrame frameB = boneTagToEntry[boneTag].paramFrameLists[0].Frames[timeIndex];
            FP currentTime = time - frameA.Time;
            FP frameTime = frameB.Time - frameA.Time;
            FP lerp = currentTime / frameTime;
            return output + AnimationFrame.Lerp(frameA, frameB, lerp);
        }
    }
}
