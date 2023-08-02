using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenKenshi
{
	internal static class Utility
	{
		public const ushort OTHER_ENDIAN_HEADER_STREAM_ID = 0x0010;
		public const ushort HEADER_STREAM_ID = 0x1000;

		public static bool IsEOF(this BinaryReader reader)
		{
			try
			{
				var b = reader.ReadByte();
				reader.BaseStream.Seek(-1, SeekOrigin.Current);
				return false;
			}
			catch (EndOfStreamException)
			{
				return true;
			}
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
			switch (type)
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
			switch (usage)
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

		public static void ProcessChunks(this BinaryReader reader, Func<ChunkInfo, bool> chunkProcessor)
		{
			do
			{
				var chunk = reader.ReadChunk();
				if (!chunkProcessor(chunk))
				{
					break;
				}
			} while (!reader.IsEOF());

			if (!reader.IsEOF())
			{
				reader.BaseStream.Seek(-(sizeof(ushort) + sizeof(int)), SeekOrigin.Current);
			}
		}

		public static string ReadOgreString(this BinaryReader reader)
		{
			var sb = new StringBuilder();
			while (!reader.IsEOF())
			{
				var c = reader.ReadByte();
				if (c == '\n')
				{
					break;
				}

				sb.Append((char)c);
			}

			return sb.ToString();
		}

		public static Vector3 ReadVector3(this BinaryReader reader)
		{
			return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		public static Quaternion ReadQuaternion(this BinaryReader reader)
		{
			return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		public static BoundingBox ReadBoundingBox(this BinaryReader reader)
		{
			return new BoundingBox(reader.ReadVector3(), reader.ReadVector3());
		}

		public static ChunkInfo ReadChunk(this BinaryReader reader)
		{
			return new ChunkInfo(reader.ReadUInt16(), reader.ReadInt32());
		}

		public static string ReadHeader(this BinaryReader reader)
		{
			// Determine endianess
			var s = reader.ReadUInt16();
			if (s == HEADER_STREAM_ID)
			{
			}
			else if (s == OTHER_ENDIAN_HEADER_STREAM_ID)
			{
				throw new NotSupportedException("Endian flipping isn't supported.");
			}
			else
			{
				throw new Exception("Header chunk didn't match either endian: Corrupted stream?");
			}

			// Read version
			return reader.ReadOgreString();
		}
	}
}