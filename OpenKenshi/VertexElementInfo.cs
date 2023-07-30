using Microsoft.Xna.Framework.Graphics;

namespace OpenKenshi
{
	internal struct VertexElementInfo
	{
		public int source;
		public int offset;
		public VertexElementFormat format;
		public VertexElementUsage usage;
		public int index;

		public VertexElementInfo(int source, int offset, VertexElementFormat format, VertexElementUsage usage, int index)
		{
			this.source = source;
			this.offset = offset;
			this.format = format;
			this.usage = usage;
			this.index = index;
		}
	}
}
