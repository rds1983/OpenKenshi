using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace OpenKenshi
{
	internal static class Utility
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

		public static PrimitiveType ToPrimitiveType(this ushort type)
		{
			switch(type)
			{
				case 1:
					return PrimitiveType.PointListEXT;
				case 2:
					return PrimitiveType.LineList;
				case 3:
					return PrimitiveType.LineStrip;
				case 4:
					return PrimitiveType.TriangleList;
				case 5:
					return PrimitiveType.TriangleStrip;
			}

			throw new Exception($"Type {type} isnt supported");
		}

		public static int GetSize(this VertexElementFormat elementFormat)
		{
			switch (elementFormat)
			{
				case VertexElementFormat.Single:
					return 4;
				case VertexElementFormat.Vector2:
					return 8;
				case VertexElementFormat.Vector3:
					return 12;
				case VertexElementFormat.Vector4:
					return 16;
				case VertexElementFormat.Color:
					return 4;
				case VertexElementFormat.Byte4:
					return 4;
				case VertexElementFormat.Short2:
					return 4;
				case VertexElementFormat.Short4:
					return 8;
				case VertexElementFormat.NormalizedShort2:
					return 4;
				case VertexElementFormat.NormalizedShort4:
					return 8;
				case VertexElementFormat.HalfVector2:
					return 4;
				case VertexElementFormat.HalfVector4:
					return 8;
			}

			return 0;
		}

		public static int CalculateVertexSize(this VertexElementInfo[] elements, int source)
		{
			var result = 0;
			for (var i = 0; i < elements.Length; ++i)
			{
				if (elements[i].source != source)
				{
					continue;
				}

				result += elements[i].format.GetSize();
			}

			return result;
		}

		public static VertexDeclaration CreateVertexDeclaration(this VertexElementInfo[] elements, int source)
		{
			var list = new List<VertexElement>();
			for (var i = 0; i < elements.Length; ++i)
			{
				if (elements[i].source != source)
				{
					continue;
				}

				list.Add(new VertexElement(elements[i].offset, elements[i].format, elements[i].usage, elements[i].index));
			}

			return new VertexDeclaration(list.ToArray());
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
