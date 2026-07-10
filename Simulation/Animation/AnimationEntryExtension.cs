using HnSF;

namespace Quantum
{
    public partial class AnimationEntry
    {
        public partial class AnimWithTargetEntry
        {
            public AssetRef<AnimationEntryBakedData> bakedAnimEntryData;
        }

        public AssetRef<AnimationEntryBakedData> GetAnimTargetBakedClipData(AssetRef<Tag> targetTag)
        {
            foreach (var v in animsTargets)
            {
                if (v.animTargetTag != targetTag) continue;
                return v.bakedAnimEntryData;
            }
            return default;
        }

        public bool TryGetAnimTargetBakedClipData(AssetRef<Tag> targetTag,
            out AssetRef<AnimationEntryBakedData> bakedClipData)
        {
            bakedClipData = default;
            foreach (var v in animsTargets)
            {
                if (v.animTargetTag != targetTag) continue;
                bakedClipData = v.bakedAnimEntryData;
                return true;
            }
            return false;
        }
    }
}