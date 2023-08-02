using System.Collections.Generic;
using AssetManagementBase;
using Microsoft.Xna.Framework;

namespace OpenKenshi
{
	[AssetLoader(typeof(MeshLoader))]
	internal class OgreMesh
	{
		public List<SubMesh> SubMeshes = new List<SubMesh>();
		public Skeleton Skeleton { get; set; }
		public BoundingBox BoundingBox { get; set; }
		public float Radius { get; set; }
	}
}
