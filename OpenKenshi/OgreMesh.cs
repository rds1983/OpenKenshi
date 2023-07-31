using System.Collections.Generic;
using AssetManagementBase;

namespace OpenKenshi
{
	[AssetLoader(typeof(MeshLoader))]
	internal class OgreMesh
	{
		public List<SubMesh> SubMeshes = new List<SubMesh>();
	}
}
