using System.Collections.Generic;

namespace OpenKenshi
{
	internal class AnimationTrack
	{
		public Bone TargetBone { get; set; }
		public List<AnimationKeyFrame> KeyFrames { get; } = new List<AnimationKeyFrame>();
	}
}
