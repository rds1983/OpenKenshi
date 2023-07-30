using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Nursia.Graphics3D.Modelling;

namespace OpenKenshi
{
	public class MeshLoader
	{
		const ushort HEADER_STREAM_ID = 0x1000;
		const ushort OTHER_ENDIAN_HEADER_STREAM_ID = 0x0010;
		const ushort M_MESH = 0x3000;
		const ushort M_GEOMETRY = 0x5000;
		const ushort M_SUBMESH = 0x4000;
		const ushort M_GEOMETRY_VERTEX_DECLARATION = 0x5100;
		const ushort M_GEOMETRY_VERTEX_ELEMENT = 0x5110;
		const ushort M_GEOMETRY_VERTEX_BUFFER = 0x5200;
		const ushort M_GEOMETRY_VERTEX_BUFFER_DATA = 0x5210;

		struct ChunkInfo
		{
			public ushort id;
			public int length;

			public ChunkInfo(ushort id, int length)
			{
				this.id = id;
				this.length = length;
			}
		}

		struct VertexElementInfo
		{
			public int source;
			public int offset;
			public VertexElementFormat format;
			public VertexElementUsage usage;
			public int index;

			public VertexElementInfo(int source, int offset,  VertexElementFormat format, VertexElementUsage usage, int index)
			{
				this.source = source;
				this.offset = offset;
				this.format = format;
				this.usage = usage;
				this.index = index;
			}

		}

		private Stream stream;
		private BinaryReader reader;

		private string ReadString()
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

		private bool ReadBool()
		{
			return stream.ReadByte() != 0;
		}

		private T[] ReadArray<T>(int size) where T : struct
		{
			var sizeOfT = Marshal.SizeOf<T>();

			var result = new T[size];
			var bytes = reader.ReadBytes(sizeOfT * size);

			Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
			return result;
		}

		private ChunkInfo ReadChunk()
		{
			ChunkInfo chunkInfo = new ChunkInfo
			{
				id = reader.ReadUInt16(),
				length = reader.ReadInt32()
			};

			return chunkInfo;
		}

		private void ProcessChunks(ushort[] types, Action<ushort> chunkProcessor)
		{
			var chunk = ReadChunk();
			while (!reader.IsEOF() && types.Contains(chunk.id))
			{
				chunkProcessor(chunk.id);

				if (!reader.IsEOF())
				{
					chunk = ReadChunk();
				}
			}

			if (!reader.IsEOF())
			{
				stream.Seek(-(sizeof(ushort) + sizeof(int)), SeekOrigin.Current);
			}
		}

		private VertexElement ReadVertexDeclarationElement()
		{
			var source = reader.ReadUInt16();
			var tmp = reader.ReadUInt16();
			VertexElementFormat format;

			if (tmp == 4 || tmp == 11)
			{
				format = VertexElementFormat.Byte4;
			} else
			{
				format = tmp.ToVertexElementFormat();
			}

			tmp = reader.ReadUInt16();
			var usage = tmp.ToVertexElementUsage();

			var offset = reader.ReadUInt16();
			var index = reader.ReadUInt16();

			return new VertexElement(offset, format, usage, index);
		}

		private VertexDeclaration ReadVertexDeclaration()
		{
			var list = new List<VertexElement>();
			ProcessChunks(new[] { M_GEOMETRY_VERTEX_ELEMENT }, id => list.Add(ReadVertexDeclarationElement()));
			return new VertexDeclaration(list.ToArray());
		}

		private VertexBuffer ReadGeometry()
		{
			var vertexCount = reader.ReadInt32();
			VertexBuffer result = null;

			VertexDeclaration vd;
			ProcessChunks(new ushort[] { M_GEOMETRY_VERTEX_DECLARATION, M_GEOMETRY_VERTEX_BUFFER },
				id =>
				{
					switch(id){
						case M_GEOMETRY_VERTEX_DECLARATION:
							vd = ReadVertexDeclaration();
							break;
						case M_GEOMETRY_VERTEX_BUFFER:
							var bindIndex = reader.ReadUInt16();
							var vertexSize = reader.ReadUInt16();
							var chunk = ReadChunk();

							if (chunk.id != M_GEOMETRY_VERTEX_BUFFER_DATA)
							{
								throw new Exception("Can't find vertex buffer data area");
							}
							break;
					}
				}
			);

			return result;
		}

		private void ReadSubMesh()
		{
			var materialName = ReadString();

			// TODO: Set material

			var useSharedVertices = ReadBool();
			var indexCount = reader.ReadInt32();

			bool areIndices32Bit = ReadBool();
			if (areIndices32Bit)
			{
				throw new Exception("32-bit indices arent supported");
			}

			var indices = ReadArray<UInt16>(indexCount);
			if (!useSharedVertices)
			{
				var chunk = ReadChunk();
				if (chunk.id != M_GEOMETRY)
				{
					throw new Exception("Missing geometry data in mesh file");
				}

				var vertexBuffer = ReadGeometry();
			}
		}

		private void ReadMesh()
		{
			var skeletallyAnimated = ReadBool();
			if (reader.IsEOF())
			{
				return;
			}

			var streamChunk = ReadChunk();
			while (!reader.IsEOF())
			{
				switch (streamChunk.id)
				{
					case M_SUBMESH:
						ReadSubMesh();
						break;
				}
			}
		}

		private NursiaModel InternalLoad()
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

			stream.Seek(-sizeof(short), SeekOrigin.Current);

			// Read header
			var header = reader.ReadUInt16();
			if (header != 4096)
			{
				throw new Exception("File header not found");
			}

			// Read version
			var version = ReadString();
			if (version != "[MeshSerializer_v1.100]")
			{
				throw new Exception($"Version {version} isn't supported.");
			}

			var streamChunk = ReadChunk();
			while (!reader.IsEOF())
			{
				switch (streamChunk.id)
				{
					case M_MESH:
						ReadMesh();
						break;
				}

				streamChunk = ReadChunk();
			}

			return null;
		}


		public NursiaModel Load(Stream stream)
		{
			try
			{
				this.stream = stream;
				reader = new BinaryReader(stream);
				return InternalLoad();
			}
			finally
			{
				reader.Dispose();
				reader = null;
				this.stream = null;
			}
		}
	}
}
