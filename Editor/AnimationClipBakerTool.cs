using Quantum;
using UnityEditor;
using UnityEngine;

namespace HnSF
{
    public class AnimationClipBakerTool : EditorWindow
    {
        [MenuItem("Tools/HnSF/Animation Clip Baker Tool")]
        public static void ShowWindow()
        {
            GetWindow(typeof(AnimationClipBakerTool), false, "Animation Clip Baker Tool");
        }

        public static readonly float SIMULATION_RATE = 60;
        
        [SerializeField] private Vector2 scrollPos;

        [SerializeField] private AnimationClip animationClip;
        [SerializeField] private Transform realRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject boneObject;

        [SerializeField] private float _lastSlider = 0;
        [SerializeField] private float previewSlider = 0;

        [SerializeField] private AnimationClipBakedData dataAsset;
        [SerializeField] private Tag tagAsset;

        [SerializeField] private AnimationFrame[] Frames;

        private void Update()
        {
            if (animator == null || dataAsset == null || Frames == null || realRoot == null) return;
            var realTimeValue = animationClip.length * previewSlider;
            if (!dataAsset.TryGetFrameAtTime(realTimeValue.ToFP(), tagAsset, out var f)) return;

            var pos = f.Position;
            var rot = f.Rotation;

            Gizmos.DrawRay(realRoot.transform.position + realRoot.transform.TransformDirection(pos.ToUnityVector3()),
                (rot.ToUnityQuaternion() * Vector3.forward));
        }

        public void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            realRoot = EditorGUILayout.ObjectField("Root", realRoot, typeof(Transform), true) as Transform;
            animator = EditorGUILayout.ObjectField("Animator", animator, typeof(Animator), true) as Animator;
            boneObject = (GameObject)EditorGUILayout.ObjectField("Bone", boneObject, typeof(GameObject), true);
            animationClip =
                (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip),
                    false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Bake Animation Curve"))
            {
                BakeAnimationCurve();
            }

            EditorGUILayout.Space();

            dataAsset =
                EditorGUILayout.ObjectField(dataAsset, typeof(AnimationClipBakedData), false) as AnimationClipBakedData;
            tagAsset = EditorGUILayout.ObjectField(tagAsset, typeof(Tag), false) as Tag;

            EditorGUILayout.Space();

            previewSlider = EditorGUILayout.Slider(previewSlider, 0, 1);

            if (!Mathf.Approximately(previewSlider, _lastSlider))
            {
                if (animator != null && boneObject != null && animationClip != null)
                {
                    animationClip.SampleAnimation(animator.gameObject, animationClip.length * previewSlider);
                }
            }

            int animationFrames = animationClip ? (int)(animationClip.length * SIMULATION_RATE) : 0;
            GUILayout.Label("Animation Total Frames: " + animationFrames);

            EditorGUILayout.EndScrollView();

            _lastSlider = previewSlider;
        }

        private void TransferToAsset()
        {
            if (dataAsset == null || tagAsset == null) return;

            float totalFramesFloat = animationClip.length * animationClip.frameRate;
            int totalFrames = Mathf.RoundToInt(totalFramesFloat);

            Undo.RecordObject(dataAsset, "Transferred Frames To Asset");
            dataAsset.ClipName = animationClip.name;
            dataAsset.FrameCount = totalFrames;
            dataAsset.FrameRate = (int)animationClip.frameRate;
            dataAsset.Length = animationClip.length.ToFP();
            dataAsset.SetEntry(tagAsset, Frames, tagAsset.name);
            dataAsset.bakedEntries = null;
            EditorUtility.SetDirty(dataAsset);
        }

        private void BakeAnimationCurve()
        {
            if (animator == null || animationClip == null || boneObject == null || realRoot == null) return;

            float totalFramesFloat = animationClip.length * animationClip.frameRate;
            int totalFrames = Mathf.RoundToInt(totalFramesFloat);

            Frames = new AnimationFrame[totalFrames];

            for (int i = 0; i < Frames.Length; i++)
            {
                var t = (float)i / (float)animationClip.frameRate;
                animationClip.SampleAnimation(animator.gameObject, t);
                animationClip.SampleAnimation(animator.gameObject, t);

                Frames[i].Time = t.ToFP();
                Frames[i].Position =
                    (realRoot.InverseTransformDirection(boneObject.transform.position) - realRoot.transform.position)
                    .ToFPVector3();
                Frames[i].Rotation = boneObject.transform.rotation.ToFPQuaternion();
            }

            TransferToAsset();
        }
    }
}