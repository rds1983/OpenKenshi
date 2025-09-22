using System;
using System.Collections.Generic;
using System.IO;
using AssetManagementBase;
using Microsoft.Xna.Framework.Graphics;
using Nursia;

namespace OpenKenshi
{
	internal class MeshLoader : IAssetLoader<OgreMesh>
	{
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
		const ushort M_MESH_BOUNDS = 0x9000;
		const ushort M_SUBMESH_NAME_TABLE = 0xA000;
		const ushort M_SUBMESH_NAME_TABLE_ELEMENT = 0xA100;
		const ushort M_EDGE_LISTS = 0xB000;

		private AssetLoaderContext _context;
		private Stream stream;
		private BinaryReader reader;

		private bool ReadBool()
		{
			return stream.ReadByte() != 0;
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
			reader.ProcessChunks(chunk =>
			{
				if (chunk.id != M_GEOMETRY_VERTEX_ELEMENT) return false;
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
			reader.ProcessChunks(chunk =>
				{
					switch (chunk.id)
					{
						case M_GEOMETRY_VERTEX_DECLARATION:
							elements = ReadVertexDeclaration();
							break;
						case M_GEOMETRY_VERTEX_BUFFER:
							var bindIndex = reader.ReadUInt16();
							var vertexSize = reader.ReadUInt16();
							var chunk2 = reader.ReadChunk();

							if (chunk2.id != M_GEOMETRY_VERTEX_BUFFER_DATA)
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

			var materialName = reader.ReadOgreString();

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
				var chunk = reader.ReadChunk();
				if (chunk.id != M_GEOMETRY)
				{
					throw new Exception("Missing geometry data in mesh file");
				}

				result.VertexBuffers = ReadGeometry();
			}

			if (!reader.IsEOF())
			{
				reader.ProcessChunks(chunk =>
				{
					switch (chunk.id)
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

		private Dictionary<int, string> ReadSubmeshNamesTable()
		{
			var result = new Dictionary<int, string>();

			reader.ProcessChunks(chunk =>
			{
				if (chunk.id != M_SUBMESH_NAME_TABLE_ELEMENT)
				{
					return false;
				}

				result[reader.ReadUInt16()] = reader.ReadOgreString();

				return true;
			});

			return result;
		}

		private OgreMesh ReadMesh()
		{
			var result = new OgreMesh();
			var skeletallyAnimated = ReadBool();

			reader.ProcessChunks(chunk =>
			{
				switch (chunk.id)
				{
					case M_SUBMESH:
						result.SubMeshes.Add(ReadSubMesh());
						break;
					case M_MESH_SKELETON_LINK:
						result.Skeleton = _context.Load<Skeleton>(reader.ReadOgreString());
						break;
					case M_MESH_BOUNDS:
						result.BoundingBox = reader.ReadBoundingBox();
						result.Radius = reader.ReadSingle();
						break;
					case M_SUBMESH_NAME_TABLE:
						var table = ReadSubmeshNamesTable();
						break;
					case M_EDGE_LISTS:
						break;
					default:
						return false;
				}

				return true;
			});

			return result;
		}

		private OgreMesh InternalLoad()
		{
			// Read version
			var version = reader.ReadHeader();
			if (version != "[MeshSerializer_v1.100]")
			{
				throw new Exception($"Version {version} isn't supported.");
			}

			var streamChunk = reader.ReadChunk();
			while (!reader.IsEOF())
			{
				switch (streamChunk.id)
				{
					case M_MESH:
						ReadMesh();
						break;
				}

				streamChunk = reader.ReadChunk();
			}

			return null;
		}

		public OgreMesh Load(AssetLoaderContext context, string name)
		{
			try
			{
				_context = context;
				stream = context.Open(name);
				reader = new BinaryReader(stream);
				return InternalLoad();
			}
			finally
			{
				reader.Dispose();
				stream.Dispose();
			}
		}
	}
}