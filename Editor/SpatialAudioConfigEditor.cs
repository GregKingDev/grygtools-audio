using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(SpatialAudioConfig))]
public class SpatialAudioConfigEditor : Editor
{
    private GameObject dummyGo;
    private AudioSource dummySource;
    private Editor dummyEditor;
    private AudioClip dummyClip;

    private void OnEnable()
    {
        CreateDummyResources();
    }

    private void OnDisable()
    {
        CleanupDummyResources();
    }

    private void CreateDummyResources()
    {
        CleanupDummyResources();

        dummyGo = new GameObject("SpatialAudioConfig_DummySource") { hideFlags = HideFlags.HideAndDontSave };
        dummySource = dummyGo.AddComponent<AudioSource>();

        dummyClip = AudioClip.Create("DummyClip", 1, 1, 44100, false);
        dummySource.clip = dummyClip;

        SyncAssetToSource();
        dummyEditor = Editor.CreateEditor(dummySource);
    }

    private void CleanupDummyResources()
    {
        if (dummyEditor != null) DestroyImmediate(dummyEditor);
        if (dummyClip != null) DestroyImmediate(dummyClip);
        if (dummyGo != null) DestroyImmediate(dummyGo);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SpatialAudioConfig config = (SpatialAudioConfig)target;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Base Configuration Settings", EditorStyles.boldLabel);
        config.spatialBlend = EditorGUILayout.Slider("Spatial Blend", config.spatialBlend, 0f, 1f);
        config.reverbZoneMix = EditorGUILayout.Slider("Reverb Zone Mix", config.reverbZoneMix, 0f, 1.1f);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3D Sound Settings", EditorStyles.boldLabel);
        config.dopplerLevel = EditorGUILayout.Slider("Doppler Level", config.dopplerLevel, 0f, 1.1f);
        config.spread = EditorGUILayout.Slider("Spread", config.spread, 0f, 360f);
        config.rolloffMode = (AudioRolloffMode)EditorGUILayout.EnumPopup("Volume Rolloff", config.rolloffMode);
        config.minDistance = EditorGUILayout.FloatField("Min Distance", config.minDistance);
        config.maxDistance = EditorGUILayout.FloatField("Max Distance", config.maxDistance);

        if (config.rolloffMode != AudioRolloffMode.Custom)
        {
            EditorGUILayout.HelpBox("Set Volume Rolloff to 'Custom' to freely manipulate all nodes directly inside the overlapping graph timeline below.", MessageType.Info);
        }

        if (EditorGUI.EndChangeCheck())
        {
            SyncAssetToSource();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Native 3D Spatial Audio Curves", EditorStyles.boldLabel);

        if (dummyEditor != null)
        {
            dummyEditor.serializedObject.Update();

            MethodInfo updateWrappersMethod = dummyEditor.GetType().GetMethod("UpdateWrappers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (updateWrappersMethod != null)
            {
                updateWrappersMethod.Invoke(dummyEditor, null);
            }

            MethodInfo audio3DGUIMethod = dummyEditor.GetType().GetMethod("Audio3DGUI", BindingFlags.NonPublic | BindingFlags.Instance);
            if (audio3DGUIMethod != null)
            {
                bool previousGUIState = GUI.enabled;
                GUI.enabled = true; 
                audio3DGUIMethod.Invoke(dummyEditor, null);
                GUI.enabled = previousGUIState;
            }

            if (dummyEditor.serializedObject.ApplyModifiedProperties())
            {
                SyncSourceToAsset();
                EditorUtility.SetDirty(config);
            }
        }

        EditorGUILayout.Space(15);

        // -------------------------------------------------------------
        // RESET BUTTON SECTION
        // -------------------------------------------------------------
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace(); // Pushes button layout towards the center/right area seamlessly
            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(150), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Reset Spatial Audio Config?", "Are you sure you want to revert all spatial sound curves and fields back to standard engine defaults?", "Yes", "Cancel"))
                {
                    ResetToEngineDefaults(config);
                }
            }
            GUILayout.FlexibleSpace();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ResetToEngineDefaults(SpatialAudioConfig config)
    {
        // Revert variables to Unity's standard factory values
        config.spatialBlend = 1f;
        config.reverbZoneMix = 1f;
        config.dopplerLevel = 1f;
        config.spread = 0f;
        config.rolloffMode = AudioRolloffMode.Logarithmic;
        config.minDistance = 1f;
        config.maxDistance = 500f;

        // Reset default vector graph linear trajectories
        config.volumeCurve = AnimationCurve.Linear(0, 1, 1, 0);
        config.spatialBlendCurve = AnimationCurve.Linear(0, 0, 1, 0);
        config.spreadCurve = AnimationCurve.Linear(0, 0, 1, 0);
        config.reverbZoneMixCurve = AnimationCurve.Linear(0, 1, 1, 1);

        // Mark asset database dirty and re-instantiate backend graph modules
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        CreateDummyResources();
    }

    private void SyncAssetToSource()
    {
        SpatialAudioConfig config = (SpatialAudioConfig)target;
        if (dummySource == null || config == null) return;

        dummySource.spatialBlend = config.spatialBlend;
        dummySource.reverbZoneMix = config.reverbZoneMix;
        dummySource.dopplerLevel = config.dopplerLevel;
        dummySource.spread = config.spread;
        dummySource.rolloffMode = config.rolloffMode;
        dummySource.minDistance = config.minDistance;
        dummySource.maxDistance = config.maxDistance;

        dummySource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, config.volumeCurve);
        dummySource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, config.spatialBlendCurve);
        dummySource.SetCustomCurve(AudioSourceCurveType.Spread, config.spreadCurve);
        dummySource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, config.reverbZoneMixCurve);
    }

    private void SyncSourceToAsset()
    {
        SpatialAudioConfig config = (SpatialAudioConfig)target;
        if (dummySource == null || config == null) return;

        config.spatialBlend = dummySource.spatialBlend;
        config.reverbZoneMix = dummySource.reverbZoneMix;
        config.dopplerLevel = dummySource.dopplerLevel;
        config.spread = dummySource.spread;
        config.rolloffMode = dummySource.rolloffMode;
        config.minDistance = dummySource.minDistance;
        config.maxDistance = dummySource.maxDistance;

        config.volumeCurve = dummySource.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
        config.spatialBlendCurve = dummySource.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
        config.spreadCurve = dummySource.GetCustomCurve(AudioSourceCurveType.Spread);
        config.reverbZoneMixCurve = dummySource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix);
    }
}
