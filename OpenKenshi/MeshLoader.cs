using System;
using System.IO;
using System.Text;
using Nursia.Graphics3D.Modelling;

namespace OpenKenshi
{
	public class MeshLoader
	{
		const ushort HEADER_STREAM_ID = 0x1000;
		const ushort OTHER_ENDIAN_HEADER_STREAM_ID = 0x0010;
		const ushort M_MESH = 0x3000;
		const ushort M_GEOMETRY = 0x5000;

		struct ChunkInfo
		{
			public ushort id;
			public int length;
		}

		private Stream stream;
		private BinaryReader reader;

		private ChunkInfo ReadChunk()
		{
			ChunkInfo chunkInfo = new ChunkInfo
			{
				id = reader.ReadUInt16(),
				length = reader.ReadInt32()
			};

			return chunkInfo;
		}

		private void ReadMesh()
		{
			var skeletallyAnimated = stream.ReadByte() != 0 ? true : false;
			if (reader.IsEOF())
			{
				return;
			}

			var streamChunk = ReadChunk();
			while(!reader.IsEOF())
			{

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
			var sb = new StringBuilder();
			while (true)
			{
				var c = reader.ReadByte();
				if (c == '\n')
				{
					break;
				}

				sb.Append((char)c);
			}

			var version = sb.ToString();
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
