using System.Diagnostics;
namespace GrygTools.Audio
{
	public static class Logger
	{
		[Conditional("GRYGTOOL_AUDIO_DEBUG")]
		public static void Log(string message, UnityEngine.Object context = null)
		{
			UnityEngine.Debug.Log($"[GRYGTOOL_AUDIO_DEBUG] {message}", context);
		}
		
		[Conditional("GRYGTOOL_AUDIO_DEBUG")]
		public static void LogWarning(string message, UnityEngine.Object context = null)
		{
			UnityEngine.Debug.LogWarning($"[GRYGTOOL_AUDIO_DEBUG] {message}", context);
		}
		
		[Conditional("GRYGTOOL_AUDIO_DEBUG")]
		public static void LogError(string message, UnityEngine.Object context = null)
		{
			UnityEngine.Debug.LogError($"[GRYGTOOL_AUDIO_DEBUG] {message}", context);
		}
	}
}
