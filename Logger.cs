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
	}
}
