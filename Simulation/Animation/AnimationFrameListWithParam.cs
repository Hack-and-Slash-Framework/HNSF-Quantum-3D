using System;
using Photon.Deterministic;

namespace HnSF
{
    [Serializable]
    public struct AnimationFrameListWithParam : IEquatable<AnimationFrameListWithParam>
    {
        public FPVector2 param;
        public AnimationFrame[] Frames;


        public bool Equals(AnimationFrameListWithParam other)
        {
            if (!param.Equals(other.param)) return false;
            if(Frames == null && other.Frames == null) return true;
            if (Frames == null || other.Frames == null) return false;
            if (Frames.Length != other.Frames.Length) return false;
            for (int i = 0; i < Frames.Length; i++)
            {
                if(!Frames[i].Equals(other.Frames[i])) return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is AnimationFrameListWithParam other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(param, Frames);
        }
    }
}