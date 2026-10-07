using UnityEngine;

[CreateAssetMenu(fileName = "NewSpatialAudioConfig", menuName = "GrygTools/Spatial Audio Settings")]
public class SpatialAudioConfig : ScriptableObject
{
	[Range(0f, 1f)] public float spatialBlend = 1f;
	[Range(0f, 1.1f)] public float reverbZoneMix = 1f;

	[Header("3D Sound Settings")]
	[Range(0f, 1.1f)] public float dopplerLevel = 1f;
	[Range(0f, 360f)] public float spread = 0f;
	public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
	public float minDistance = 1f;
	public float maxDistance = 500f;

	[Header("Custom Curves")]
	public AnimationCurve volumeCurve = AnimationCurve.Linear(0, 1, 1, 0);
	public AnimationCurve spatialBlendCurve = AnimationCurve.Linear(0, 0, 1, 0);
	public AnimationCurve spreadCurve = AnimationCurve.Linear(0, 0, 1, 0);
	public AnimationCurve reverbZoneMixCurve = AnimationCurve.Linear(0, 1, 1, 1);

	public void ApplyTo(AudioSource audioSource)
	{
		if (audioSource == null) return;

		// Apply basic settings
		audioSource.spatialBlend = spatialBlend;
		audioSource.reverbZoneMix = reverbZoneMix;
		audioSource.dopplerLevel = dopplerLevel;
		audioSource.spread = spread;
		audioSource.rolloffMode = rolloffMode;
		audioSource.minDistance = minDistance;
		audioSource.maxDistance = maxDistance;

		// Apply the custom 3D graphs if using Custom Rolloff
		if (rolloffMode == AudioRolloffMode.Custom)
		{
			audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, volumeCurve);
			audioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, spatialBlendCurve);
			audioSource.SetCustomCurve(AudioSourceCurveType.Spread, spreadCurve);
			audioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, reverbZoneMixCurve);
		}
	}
}
