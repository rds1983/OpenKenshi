using System.Collections.Generic;

namespace OpenKenshi
{
	internal class Animation
	{
		public string Name { get; set; }
		public float Length { get; set; }
		public List<AnimationTrack> Tracks { get; } = new List<AnimationTrack>();
	}
}
