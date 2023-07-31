using System;
using System.IO;
using AssetManagementBase;

namespace OpenKenshi
{
	internal class SkeletonLoader : IAssetLoader<Skeleton>
	{
		private const ushort SKELETON_BONE = 0x2000;
		private const ushort SKELETON_BONE_PARENT = 0x3000;
		private const ushort SKELETON_ANIMATION = 0x4000;
		private const ushort SKELETON_ANIMATION_LINK = 0x5000;

		private Stream stream;
		private BinaryReader reader;

		private Skeleton InternalLoad()
		{
			// Read version
			var version = reader.ReadHeader();
			if (version != "[Serializer_v1.10]")
			{
				throw new Exception($"Version {version} isn't supported.");
			}


			return null;
		}

		public Skeleton Load(AssetLoaderContext context, string name)
		{
			try
			{
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
