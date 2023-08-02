using System.Collections.Generic;
using AssetManagementBase;

namespace OpenKenshi
{
	[AssetLoader(typeof(SkeletonLoader))]
	internal class Skeleton
	{
		public List<Bone> Bones { get; } = new List<Bone>();
		public Dictionary<string, Animation> Animations { get; } = new Dictionary<string, Animation>();
	}
}
