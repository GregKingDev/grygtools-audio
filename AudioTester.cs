using GrygTools.Utils.Attributes;
using UnityEngine;
namespace GrygTools.Audio
{
	public class AudioTester : MonoBehaviour
	{
		[SerializeField]
		private SfxConfig sfxConfig;
		[SerializeField] private AudioSource audioSource;
		[SerializeField] private SpatialAudioConfig spatialAudioConfig;
		
		[InspectorButton("Apply Spatial Audio Config")]
		private void ApplySettings(SpatialAudioConfig config)
		{
			if (audioSource != null && config != null)
			{
				config.ApplyTo(audioSource);
				Debug.Log("Applied Spatial Audio Config to AudioSource.");
			}
			else
			{
				Debug.LogWarning("AudioSource or SpatialAudioConfig is not assigned.");
			}
		}
	}
}
