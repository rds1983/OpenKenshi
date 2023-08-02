using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace OpenKenshi
{
	internal class Bone
	{
		public string Name { get; set; }
		public int Handle { get; set; }
		public Vector3 Position { get; set; } = Vector3.Zero;
		public Quaternion Orientation { get; set; } = Quaternion.Identity;
		public Vector3 Scale { get; set; } = Vector3.One;

		public List<Bone> Children { get; } = new List<Bone>();
	}
}
