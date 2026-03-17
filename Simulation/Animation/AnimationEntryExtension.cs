using HnSF;

namespace Quantum
{
    public partial class AnimationEntry : AssetObject
    {
        public partial class AnimEntry
        {
            public AssetRef<AnimationClipBakedData> bakedClipData;
        }

        public AssetRef<AnimationClipBakedData> GetAnimTargetBakedClipData(AssetRef<Tag> targetTag)
        {
            foreach (var v in animsTargets)
            {
                if (v.animTargetTag != targetTag) continue;
                return v.anims[0].bakedClipData;
            }

            return default;
        }

        public bool TryGetAnimTargetBakedClipData(AssetRef<Tag> targetTag,
            out AssetRef<AnimationClipBakedData> bakedClipData)
        {
            bakedClipData = default;
            foreach (var v in animsTargets)
            {
                if (v.animTargetTag != targetTag) continue;
                bakedClipData = v.anims[0].bakedClipData;
                return true;
            }

            return false;
        }
    }
}