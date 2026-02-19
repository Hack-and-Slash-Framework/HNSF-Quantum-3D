using System;
using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;

namespace HnSF
{
    public class AnimationClipBakedData : AssetObject
    {
        [System.Serializable]
        public struct BakedEntry : IEquatable<BakedEntry>
        {
            public string name;
            public AssetRef<Tag> tag;
            public AnimationFrame[] Frames;

            public bool Equals(BakedEntry other)
            {
                return tag.Equals(other.tag) && Frames == other.Frames;
            }

            public override bool Equals(object obj)
            {
                return obj is BakedEntry other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(tag, Frames);
            }
        }
        
        [NonSerialized] public Dictionary<AssetRef<Tag>, BakedEntry> bakedEntries = null;
        
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
            if(bakedEntries == null) BuildEntries();
            return bakedEntries[tag];
        }

        public BakedEntry GetEntrySlow(AssetRef<Tag> tag)
        {
            for (int i = 0; i < BakedEntries.Count; i++)
            {
                if(BakedEntries[i].tag == tag) return BakedEntries[i];
            }
            return default;
        }

        public bool SetEntry(AssetRef<Tag> tag, AnimationFrame[] frames, string entryName)
        {
            for (int i = 0; i < BakedEntries.Count; i++)
            {
                if(BakedEntries[i].tag != tag) continue;

                BakedEntries[i] = new BakedEntry()
                {
                    name = entryName,
                    tag = tag,
                    Frames = frames,
                };
                return true;
            }
            BakedEntries.Add(new BakedEntry()
            {
                name = entryName,
                tag = tag,
                Frames = frames,
            });
            return true;
        }
        
        void BuildEntries()
        {
            if (bakedEntries != null) return;
            bakedEntries = new Dictionary<AssetRef<Tag>, BakedEntry>();

            foreach (var be in BakedEntries)
            {
                bakedEntries.Add(be.tag, be);
            }
        }
        
        public AnimationFrame CalculateDelta(AssetRef<Tag> tag, FP lastTime, FP currentTime)
        {
            BuildEntries();
            if (bakedEntries.ContainsKey(tag) == false) return default;
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
            if (bakedEntries.ContainsKey(tag) == false) return false;
            frame = GetFrameAtTime(time, tag);
            return true;
        }
        
        public bool TryGetClosestFrame(FP time, AssetRef<Tag> tag, out AnimationFrame frame)
        {
            frame = default;
            BuildEntries();
            if (bakedEntries.ContainsKey(tag) == false) return false;

            if (time > Length)
            {
                frame = bakedEntries[tag].Frames[^1];
                return true;
            }
            
            int timeIndex = FrameCount - 1;
            for (int f = 1; f < FrameCount; f++)
            {
                if (bakedEntries[tag].Frames[f].Time > time)
                {
                    frame = bakedEntries[tag].Frames[f];
                    return true;
                }
            }
            return false;
        }

        public AnimationFrame GetFrameAtTime(FP time, AssetRef<Tag> tag)
        {
            BuildEntries();
            if (bakedEntries.ContainsKey(tag) == false) return default;
            AnimationFrame output = new AnimationFrame(FPQuaternion.Identity);
            if (Length == FP._0)
                return bakedEntries[tag].Frames[0];

            while (time > Length)
            {
                time -= Length;
                output += bakedEntries[tag].Frames[FrameCount - 1];
            }
      

            int timeIndex = FrameCount - 1;
            for (int f = 1; f < FrameCount; f++)
            {
                if (bakedEntries[tag].Frames[f].Time > time)
                {
                    timeIndex = f;
                    break;
                }
            }

            AnimationFrame frameA = bakedEntries[tag].Frames[timeIndex - 1];
            AnimationFrame frameB = bakedEntries[tag].Frames[timeIndex];
            FP currentTime = time - frameA.Time;
            FP frameTime = frameB.Time - frameA.Time;
            FP lerp = currentTime / frameTime;
            return output + AnimationFrame.Lerp(frameA, frameB, lerp);
        }
    }
}
