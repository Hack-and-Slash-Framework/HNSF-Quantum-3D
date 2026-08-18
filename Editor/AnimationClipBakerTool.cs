#if ENABLE_ANIMANCER
using System.Collections.Generic;
using Animancer;
using Photon.Deterministic;
using Quantum;
using UnityEditor;
using UnityEngine;

namespace HnSF
{
    public class NewClipBaker : EditorWindow
    {
        private const float BoneGizmoHandleSize = 0.5f;
        
        [MenuItem("Tools/HnSF/Animation Entry Bone Baker")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(NewClipBaker), false, "Animation Clip Baker Tool") as NewClipBaker;

            if (Selection.activeGameObject != null)
            {
                window.SetActor(Selection.activeGameObject);
            }
        }

        public static readonly float SIMULATION_RATE = 60;
        
        [SerializeField] private Vector2 scrollPos;
        [SerializeField] private Vector2 tagBakeScrollPos;

        //[SerializeField] private AnimationClip animationClip;
        [SerializeField] private GameObject boneObject;

        [SerializeField] private float _lastSlider = 0;
        [SerializeField] private float previewSlider = 0;

        
        [SerializeField] private Animator animator;
        [SerializeField] private FighterVisualPositioner visualPositioner;
        [SerializeField] public Tag animatorTag;
        [SerializeField] private Transform realRoot;
        [SerializeField] private bool[] shouldBakeTag;
        [SerializeField] private AnimationEntry animationEntry;
        
        [SerializeField] private Dictionary<AssetRef<Tag>, AnimationFrameListWithParam[]> boneTagToFrameList = new();
        
        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawSceneGizmos;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneGizmos;
        }
        
        public void SetActor(GameObject source)
        {
            if (source == null
                || !source.TryGetComponent(out FighterVisualPositioner fvp)
                || !source.TryGetComponent(out EntityAnimationUpdaterAnimancer eaua))
                return;

            visualPositioner = fvp;
            animator = null;
            foreach (var animatorTagged in eaua.animators)
            {
                if(animatorTagged.tag != animatorTag)
                    continue;
                animator = animatorTagged.animancer.Animator;
                realRoot = animator.transform.parent;
                break;
            }
            
            EnsureShouldBakeTagSize();
        }

        private void EnsureShouldBakeTagSize()
        {
            int tagCount = visualPositioner != null && visualPositioner.tagToBones != null
                ? visualPositioner.tagToBones.Length
                : 0;

            if (shouldBakeTag != null && shouldBakeTag.Length == tagCount)
                return;

            var previous = shouldBakeTag;
            shouldBakeTag = new bool[tagCount];

            if (previous == null)
                return;

            int countToCopy = Mathf.Min(previous.Length, shouldBakeTag.Length);
            for (int i = 0; i < countToCopy; i++)
            {
                shouldBakeTag[i] = previous[i];
            }
        }

        private void DrawTagBakeToggles()
        {
            EnsureShouldBakeTagSize();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tags To Bake", EditorStyles.boldLabel);

            if (visualPositioner == null)
            {
                EditorGUILayout.HelpBox("Assign a Visual Positioner to choose which tag bones to bake.", MessageType.Info);
                return;
            }

            if (visualPositioner.tagToBones == null || visualPositioner.tagToBones.Length == 0)
            {
                EditorGUILayout.HelpBox("The assigned Visual Positioner has no tag bone definitions.", MessageType.Info);
                return;
            }

            float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float listHeight = Mathf.Min(180f, visualPositioner.tagToBones.Length * lineHeight + 8f);

            tagBakeScrollPos = EditorGUILayout.BeginScrollView(tagBakeScrollPos, GUILayout.Height(listHeight));
            EditorGUI.indentLevel++;
            for (int i = 0; i < visualPositioner.tagToBones.Length; i++)
            {
                var tagBone = visualPositioner.tagToBones[i];
                string label = tagBone != null && !string.IsNullOrWhiteSpace(tagBone.name)
                    ? tagBone.name
                    : $"Tag Bone {i}";

                EditorGUI.BeginChangeCheck();
                shouldBakeTag[i] = EditorGUILayout.ToggleLeft(label, shouldBakeTag[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndScrollView();
        }

        private void DrawSceneGizmos(SceneView sceneView)
        {
            DrawBaked();
            return;
            if (visualPositioner == null
                || visualPositioner.tagToBones == null
                || shouldBakeTag == null)
                return;

            int tagCount = Mathf.Min(visualPositioner.tagToBones.Length, shouldBakeTag.Length);
            for (int i = 0; i < tagCount; i++)
            {
                if (!shouldBakeTag[i])
                    continue;

                var tagBone = visualPositioner.tagToBones[i];
                if (tagBone == null || tagBone.bone == null)
                    continue;

                DrawBoneOrientationGizmo(tagBone.bone.transform);
            }
        }

        private static void DrawBoneOrientationGizmo(Transform bone)
        {
            Vector3 position = bone.position;
            Quaternion rotation = bone.rotation;
            float size = HandleUtility.GetHandleSize(position) * BoneGizmoHandleSize;

            Handles.color = Color.white;
            Handles.SphereHandleCap(0, position, Quaternion.identity, size * 0.2f, EventType.Repaint);

            DrawOrientationAxis(position, rotation * Vector3.right, Color.red, size);
            DrawOrientationAxis(position, rotation * Vector3.up, Color.green, size);
            DrawOrientationAxis(position, rotation * Vector3.forward, Color.blue, size);
        }

        private static void DrawOrientationAxis(Vector3 position, Vector3 direction, Color color, float size)
        {
            Handles.color = color;
            Handles.DrawLine(position, position + direction * size, 2f);
            Handles.ConeHandleCap(
                0,
                position + direction * size,
                Quaternion.LookRotation(direction),
                size * 0.15f,
                EventType.Repaint);
        }
        
        private void DrawBaked()
        {
            //if (boneTagToFrameList == null || realRoot == null) return;
            if (visualPositioner == null) return;
            var animWithTarget = GetAnimWithTargetEntry();
            if (animWithTarget == null) return;
            if (animWithTarget.bakedAnimEntryData == default
                || !QuantumUnityDB.TryGetGlobalAssetEditorInstance(animWithTarget.bakedAnimEntryData,
                    out var bakedDataAsset))
                return;
            
            float animLength = 0;
            float animFramerate = 0;
            GetClipData(animWithTarget, ref animLength, ref animFramerate);

            var realTimeValue = animLength * previewSlider;

            for (var index = 0; index < visualPositioner.tagToBones.Length; index++)
            {
                if(!shouldBakeTag[index])
                    continue;
                
                if(!bakedDataAsset.TryGetFrameAtTime(realTimeValue.ToFP(), visualPositioner.tagToBones[index].tag, out var f))
                    continue;

                var rRoot = realRoot.transform.position;
                var pos = f.Position;
                var rot = f.Rotation;
                
                DrawOrientationAxis(rRoot + pos.ToUnityVector3(),
                    (rot.ToUnityQuaternion() * Vector3.up),
                    Color.green,
                    BoneGizmoHandleSize);
            }
        }

        public void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            visualPositioner = EditorGUILayout.ObjectField("Visual Positioner", visualPositioner, typeof(FighterVisualPositioner), true) as FighterVisualPositioner;
            realRoot = EditorGUILayout.ObjectField("Root", realRoot, typeof(Transform), true) as Transform;
            animatorTag = EditorGUILayout.ObjectField("Animator Tag", animatorTag, typeof(Tag), true) as Tag;
            animationEntry = EditorGUILayout.ObjectField("Animation Entry", animationEntry, typeof(AnimationEntry), true) as AnimationEntry;
            
            DrawTagBakeToggles();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Bake"))
            {
                BakeAnimationCurve();
            }
            
            
            EditorGUILayout.LabelField("Preview");
            float animLength = 0;
            previewSlider = EditorGUILayout.Slider(previewSlider, 0, 1);
            if (!Mathf.Approximately(previewSlider, _lastSlider))
            {
                if (animator != null && animationEntry != null && animatorTag != null)
                {
                    foreach (var ae in animationEntry.animsTargets)
                    {
                        if(ae.animTargetTag != animatorTag)
                            continue;

                        switch (ae.animancerTransition)
                        {
                            case ClipTransition ct:
                                AnimationClip clip = ct.Clip;
                                ct.Clip.SampleAnimation(animator.gameObject, ct.Clip.length * previewSlider);

                                if (clip.TryGetClipRotationOffsetY(out var offsetY)
                                    || clip.TryGetStandaloneClipOrientationOffsetY(out offsetY))
                                {
                                    animator.transform.localEulerAngles = new Vector3(0, -offsetY, 0);
                                }
                                animLength = clip.length;
                                break;
                        }
                        break;
                    }
                    animator.gameObject.transform.position = Vector3.zero;
                    SceneView.RepaintAll();
                }
            }

            int animationFrames = (int)(animLength * SIMULATION_RATE);
            GUILayout.Label("Animation Total Frames: " + animationFrames);
            
            
            EditorGUILayout.EndScrollView();
            _lastSlider = previewSlider;
        }

        private void BakeAnimationCurve()
        {
            Debug.Log("Attempting to bake curve");
            if (animationEntry == null || animatorTag == null) return;
            boneTagToFrameList = new();
            
            float animLength = 0;
            float animFramerate = 0;
            AnimationEntry.AnimWithTargetEntry animWithTargetEntry = GetAnimWithTargetEntry();

            if (animWithTargetEntry == null)
            {
                Debug.LogError("Anim Target Entry is Null");
                return;
            }

            GetClipData(animWithTargetEntry, ref animLength, ref animFramerate);
                
            float totalFramesFloat = animLength * animFramerate;
            int totalFrames = Mathf.RoundToInt(totalFramesFloat);
            
            for (int w = 0; w < totalFrames; w++)
            {
                var t = (float)w / (float)animFramerate;
                SampleClip(animWithTargetEntry, t);
                
                for (int i = 0; i < visualPositioner.tagToBones.Length; i++)
                {
                    if(shouldBakeTag[i] == false)
                        continue;

                    if (!boneTagToFrameList.ContainsKey(visualPositioner.tagToBones[i].tag))
                    {
                        boneTagToFrameList.Add(visualPositioner.tagToBones[i].tag, new AnimationFrameListWithParam[1]);
                        boneTagToFrameList[visualPositioner.tagToBones[i].tag][0].param = new FPVector2(0, 0);
                        boneTagToFrameList[visualPositioner.tagToBones[i].tag][0].Frames = new AnimationFrame[totalFrames];
                    }

                    boneTagToFrameList[visualPositioner.tagToBones[i].tag][0].Frames[w].Time = t.ToFP();
                    boneTagToFrameList[visualPositioner.tagToBones[i].tag][0].Frames[w].Position =
                        (visualPositioner.tagToBones[i].bone.transform.position - realRoot.transform.position).ToFPVector3();
                    boneTagToFrameList[visualPositioner.tagToBones[i].tag][0].Frames[w].Rotation =
                        visualPositioner.tagToBones[i].bone.transform.rotation.ToFPQuaternion();
                    //Quaternion.LookRotation(visualPositioner.tagToBones[i].bone.transform.up, visualPositioner.tagToBones[i].bone.transform.forward).ToFPQuaternion();
                }
            }
            
            TransferToAsset();
        }

        private void TransferToAsset()
        {
            Debug.Log("Transferring to Asset.");
            var animWithTarget = GetAnimWithTargetEntry();
            if (animWithTarget == null) return;
            if (animWithTarget.bakedAnimEntryData == default
                || !QuantumUnityDB.TryGetGlobalAssetEditorInstance(animWithTarget.bakedAnimEntryData,
                    out var bakedDataAsset))
                return;
            
            float animLength = 0;
            float animFramerate = 0;
            GetClipData(animWithTarget, ref animLength, ref animFramerate);
            
            float totalFramesFloat = animLength * animFramerate;
            int totalFrames = Mathf.RoundToInt(totalFramesFloat);
            
            Undo.RecordObject(bakedDataAsset, "Transferred Frames To Asset");
            //dataAsset.ClipName = animationClip.name;
            bakedDataAsset.FrameCount = totalFrames;
            bakedDataAsset.FrameRate = (int)animFramerate;
            bakedDataAsset.Length = animLength.ToFP();
            bakedDataAsset.BakedEntries = new List<AnimationEntryBakedData.BakedEntry>();
            foreach (var t in boneTagToFrameList)
            {
                bakedDataAsset.AddOrUpdateEntry(t.Key, t.Value, "");
            }
            bakedDataAsset.boneTagToEntry = null;
            EditorUtility.SetDirty(bakedDataAsset);
        }

        private AnimationEntry.AnimWithTargetEntry GetAnimWithTargetEntry()
        {
            foreach (var ae in animationEntry.animsTargets)
            {
                if (ae.animTargetTag != animatorTag)
                    continue;
                return ae;
            }
            return null;
        }
        
        private void GetClipData(AnimationEntry.AnimWithTargetEntry ae, ref float animLength, ref float animFramerate)
        {
            switch (ae.animancerTransition)
            {
                case ClipTransition ct:
                    AnimationClip clip = ct.Clip;
                    animLength = clip.length;
                    animFramerate = clip.frameRate;
                    break;
                case LinearMixerTransition lmt:
                    foreach (var anim in lmt.Animations)
                    {
                        Debug.Log(anim);
                    }
                    break;
            }
        }
        
        private void SampleClip(AnimationEntry.AnimWithTargetEntry ae, float time)
        {
            switch (ae.animancerTransition)
            {
                case ClipTransition ct:
                    AnimationClip clip = ct.Clip;
                    ct.Clip.SampleAnimation(animator.gameObject, time);

                    if (clip.TryGetClipRotationOffsetY(out var offsetY)
                        || clip.TryGetStandaloneClipOrientationOffsetY(out offsetY))
                    {
                        animator.transform.SetLocalPositionAndRotation(animator.transform.localPosition, Quaternion.Euler(new Vector3(0, -offsetY, 0)));
                    }
                    break;
            }
        }
    }
}
#endif