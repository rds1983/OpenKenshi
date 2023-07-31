using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Nursia;
using Nursia.Graphics3D.Modelling;

namespace OpenKenshi
{
	public class MeshLoader
	{
		const ushort OTHER_ENDIAN_HEADER_STREAM_ID = 0x0010;
		const ushort HEADER_STREAM_ID = 0x1000;
		const ushort M_MESH = 0x3000;
		const ushort M_SUBMESH = 0x4000;
		const ushort M_SUBMESH_OPERATION = 0x4010;
		const ushort M_SUBMESH_BONE_ASSIGNMENT = 0x4100;
		const ushort M_SUBMESH_TEXTURE_ALIAS = 0x4200;
		const ushort M_GEOMETRY = 0x5000;
		const ushort M_GEOMETRY_VERTEX_DECLARATION = 0x5100;
		const ushort M_GEOMETRY_VERTEX_ELEMENT = 0x5110;
		const ushort M_GEOMETRY_VERTEX_BUFFER = 0x5200;
		const ushort M_GEOMETRY_VERTEX_BUFFER_DATA = 0x5210;
		const ushort M_MESH_SKELETON_LINK = 0x6000;

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
			return new ChunkInfo(reader.ReadUInt16(), reader.ReadInt32());
		}

		private void ProcessChunks(Func<ushort, bool> chunkProcessor)
		{
			do
			{
				var chunk = ReadChunk();
				if (!chunkProcessor(chunk.id))
				{
					break;
				}
			} while (!reader.IsEOF());

			if (!reader.IsEOF())
			{
				stream.Seek(-(sizeof(ushort) + sizeof(int)), SeekOrigin.Current);
			}
		}

		private VertexElementInfo ReadVertexDeclarationElement()
		{
			var source = reader.ReadUInt16();
			var tmp = reader.ReadUInt16();
			VertexElementFormat format;

			if (tmp == 4 || tmp == 11)
			{
				format = VertexElementFormat.Byte4;
			}
			else
			{
				format = tmp.ToVertexElementFormat();
			}

			tmp = reader.ReadUInt16();
			var usage = tmp.ToVertexElementUsage();

			var offset = reader.ReadUInt16();
			var index = reader.ReadUInt16();

			return new VertexElementInfo(source, offset, format, usage, index);
		}

		private VertexElementInfo[] ReadVertexDeclaration()
		{
			var list = new List<VertexElementInfo>();
			ProcessChunks(id =>
			{
				if (id != M_GEOMETRY_VERTEX_ELEMENT) return false;
				list.Add(ReadVertexDeclarationElement());
				return true;
			});
			return list.ToArray();
		}

		private Dictionary<int, VertexBuffer> ReadGeometry()
		{
			var vertexCount = reader.ReadInt32();
			Dictionary<int, VertexBuffer> result = new Dictionary<int, VertexBuffer>();

			VertexElementInfo[] elements = null;
			ProcessChunks(id =>
				{
					switch (id)
					{
						case M_GEOMETRY_VERTEX_DECLARATION:
							elements = ReadVertexDeclaration();
							break;
						case M_GEOMETRY_VERTEX_BUFFER:
							var bindIndex = reader.ReadUInt16();
							var vertexSize = reader.ReadUInt16();
							var chunk = ReadChunk();

							if (chunk.id != M_GEOMETRY_VERTEX_BUFFER_DATA)
							{
								throw new Exception("Can't find vertex buffer data area");
							}

							if (elements.CalculateVertexSize(bindIndex) != vertexSize)
							{
								throw new Exception("Buffer vertex size does not agree with vertex declaration");
							}

							var vd = elements.CreateVertexDeclaration(bindIndex);
							var vertexBuffer = new VertexBuffer(Nrs.GraphicsDevice, vd, vertexCount, BufferUsage.None);

							var data = reader.ReadBytes(vertexCount * vd.VertexStride);
							vertexBuffer.SetData(data);

							result[bindIndex] = vertexBuffer;
							break;
						default:
							return false;
					}

					return true;
				}
			);

			return result;
		}

		private VertexBoneAssignment ReadBoneAssignment()
		{
			var result = new VertexBoneAssignment
			{
				VertexIndex = reader.ReadInt32(),
				BoneIndex = reader.ReadUInt16(),
				Weight = reader.ReadSingle()
			};

			return result;
		}

		private SubMesh ReadSubMesh()
		{
			var result = new SubMesh();

			var materialName = ReadString();

			// TODO: Set material

			result.UseSharedVertices = ReadBool();
			var indexCount = reader.ReadInt32();

			bool areIndices32Bit = ReadBool();
			if (areIndices32Bit)
			{
				throw new Exception("32-bit indices arent supported");
			}

			var bytes = reader.ReadBytes(sizeof(ushort) * indexCount);
			result.IndexBuffer = new IndexBuffer(Nrs.GraphicsDevice, IndexElementSize.SixteenBits, indexCount, BufferUsage.None);
			result.IndexBuffer.SetData(bytes);

			if (!result.UseSharedVertices)
			{
				var chunk = ReadChunk();
				if (chunk.id != M_GEOMETRY)
				{
					throw new Exception("Missing geometry data in mesh file");
				}

				result.VertexBuffers = ReadGeometry();
			}

			if (!reader.IsEOF())
			{
				ProcessChunks(id =>
				{
					switch(id)
					{
						case M_SUBMESH_OPERATION:
							var s = reader.ReadUInt16();
							result.PrimitiveType = s.ToPrimitiveType();
							break;
						case M_SUBMESH_BONE_ASSIGNMENT:
							result.BoneAssignments.Add(ReadBoneAssignment());
							break;
						case M_SUBMESH_TEXTURE_ALIAS:
							throw new Exception("texture aliases for SubMeshes are unsupported");
						default:
							return false;
					}

					return true;
				});
			}

			return result;
		}

		private OgreMesh ReadMesh()
		{
			var result = new OgreMesh();
			var skeletallyAnimated = ReadBool();

			ProcessChunks(id =>
			{
				switch(id)
				{
					case M_SUBMESH:
						result.SubMeshes.Add(ReadSubMesh());
						break;
					case M_MESH_SKELETON_LINK:
						break;
					default:
						return false;
				}

				return true;
			});

			return result;
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
