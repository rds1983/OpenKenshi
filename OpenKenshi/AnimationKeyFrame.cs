using Microsoft.Xna.Framework;

namespace OpenKenshi
{
	internal class AnimationKeyFrame
	{
		public float TimeInSeconds { get; set; }
		public Quaternion Rotation { get; set; } = Quaternion.Identity;
		public Vector3 Translate { get; set; }
		public Vector3 Scale { get; set; } = Vector3.One;
	}
}
