using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace OpenKenshi
{
	internal static class Utils
	{
		public static bool IsEOF(this BinaryReader reader)
		{
			return reader.PeekChar() == -1;
		}

		public static VertexElementFormat ToVertexElementFormat(this ushort format)
		{
			switch (format)
			{
				case 0:
					return VertexElementFormat.Single;
				case 1:
					return VertexElementFormat.Vector2;
				case 2:
					return VertexElementFormat.Vector3;
				case 3:
					return VertexElementFormat.Vector4;
				case 6:
					return VertexElementFormat.Short2;
				case 8:
					return VertexElementFormat.Short4;
				case 9:
					return VertexElementFormat.Byte4;
				case 30:
					return VertexElementFormat.Color;
				case 31:
					return VertexElementFormat.Short2;
				case 32:
					return VertexElementFormat.Short4;
			}

			throw new Exception($"Format {format} isnt supported");
		}

		public static VertexElementUsage ToVertexElementUsage(this ushort usage)
		{
			switch(usage)
			{
				case 1:
					return VertexElementUsage.Position;
				case 2:
					return VertexElementUsage.BlendWeight;
				case 3:
					return VertexElementUsage.BlendIndices;
				case 4:
					return VertexElementUsage.Normal;
				case 5:
					return VertexElementUsage.Color;
				case 7:
					return VertexElementUsage.TextureCoordinate;
				case 8:
					return VertexElementUsage.Binormal;
				case 9:
					return VertexElementUsage.Tangent;
			}

			throw new Exception($"Usage {usage} isnt supported");
		}
	}
}
