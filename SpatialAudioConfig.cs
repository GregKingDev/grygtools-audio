using GrygTools.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSpatialAudioConfig", menuName = "GrygTools/Spatial Audio Settings")]
public class SpatialAudioConfig : ScriptableObject
{
	[Range(0f, 1f)] public float spatialBlend = 1f;
	[Range(0f, 1.1f)] public float reverbZoneMix = 1f;

	[Header("3D Sound Settings")]
	[Range(0f, 5f)] public float dopplerLevel = 1f;
	[Range(0f, 360f)] public float spread = 0f;
	public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
	public float minDistance = 1f;
	public float maxDistance = 500f;

	[Header("Custom Curves")]
	public AnimationCurve volumeCurve = AnimationCurve.Linear(0, 1, 1, 0);
	public AnimationCurve spatialBlendCurve = AnimationCurve.Linear(0, 0, 1, 0);
	public AnimationCurve spreadCurve = AnimationCurve.Linear(0, 0, 1, 0);
	public AnimationCurve reverbZoneMixCurve = AnimationCurve.Linear(0, 1, 1, 1);
}
