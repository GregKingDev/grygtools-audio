using System;
using UnityEngine;

namespace GrygTools.Audio
{
	[Serializable]
	public struct AudioCollisionTag : IEquatable<AudioCollisionTag>
	{
		// 1. Unity needs to see this field to serialize it
		[SerializeField]
		private string _value;

		public string Value => _value ?? string.Empty;

		public AudioCollisionTag(string value)
		{
			_value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public static implicit operator AudioCollisionTag(string value) => new(value);
		public static implicit operator string(AudioCollisionTag tag) => tag.Value;

		public bool Equals(AudioCollisionTag other) => Value == other.Value;
		public override bool Equals(object obj) => obj is AudioCollisionTag other && Equals(other);
		public override int GetHashCode() => Value.GetHashCode();
		public override string ToString() => Value;

		public static bool operator==(AudioCollisionTag left, AudioCollisionTag right) => left.Equals(right);
		public static bool operator!=(AudioCollisionTag left, AudioCollisionTag right) => !left.Equals(right);
	}
}