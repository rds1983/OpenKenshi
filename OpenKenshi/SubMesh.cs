using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace OpenKenshi
{
	internal class SubMesh
	{
		public bool UseSharedVertices { get; set; }
		public IndexBuffer IndexBuffer { get; set; }
		public Dictionary<int, VertexBuffer> VertexBuffers { get; set; }
		public PrimitiveType PrimitiveType { get; set; }
		public List<VertexBoneAssignment> BoneAssignments { get; } = new List<VertexBoneAssignment>();
	}
}
