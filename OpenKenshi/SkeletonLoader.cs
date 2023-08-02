using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using AssetManagementBase;
using Nursia.Graphics3D.Modelling;

namespace OpenKenshi
{
	internal class SkeletonLoader : IAssetLoader<Skeleton>
	{
		private const ushort SKELETON_BONE = 0x2000;
		private const ushort SKELETON_BONE_PARENT = 0x3000;
		private const ushort SKELETON_ANIMATION = 0x4000;
		private const ushort SKELETON_ANIMATION_BASEINFO = 0x4010;
		private const ushort SKELETON_ANIMATION_TRACK = 0x4100;
		private const ushort SKELETON_ANIMATION_TRACK_KEYFRAME = 0x4110;
		private const ushort SKELETON_ANIMATION_LINK = 0x5000;

		private const int BoneSizeWithoutScale = 6 +
			sizeof(ushort) +    // Handle
			sizeof(float) * 3 + // Position
			sizeof(float) * 4;  // Orientation

		private const int KeyFrameSizeWithoutScale = 6 +
			sizeof(float) +     // Time
			sizeof(float) * 3 + // Position
			sizeof(float) * 4;  // Orientation

		private Stream stream;
		private BinaryReader reader;
		private readonly Dictionary<int, Bone> bones = new Dictionary<int, Bone>();

		private Bone ReadBone()
		{
			var result = new Bone
			{
				Name = reader.ReadOgreString(),
				Handle = reader.ReadUInt16(),
				Position = reader.ReadVector3(),
				Orientation = reader.ReadQuaternion(),
			};

			return result;
		}

		private AnimationTrack ReadAnimationTrack()
		{
			var result = new AnimationTrack();

			var bone = bones[reader.ReadUInt16()];
			result.TargetBone = bone;

			if (reader.IsEOF())
			{
				return result;
			}

			reader.ProcessChunks(chunk =>
			{
				if (chunk.id != SKELETON_ANIMATION_TRACK_KEYFRAME)
				{
					return false;
				}
				var keyFrame = new AnimationKeyFrame
				{
					TimeInSeconds = reader.ReadSingle(),
					Rotation = reader.ReadQuaternion(),
					Translate = reader.ReadVector3(),
				};

				if (chunk.length > KeyFrameSizeWithoutScale)
				{
					keyFrame.Scale = reader.ReadVector3();
				}

				result.KeyFrames.Add(keyFrame);
				return true;
			});

			return result;
		}

		private Animation ReadAnimation()
		{
			var result = new Animation
			{
				Name = reader.ReadOgreString(),
				Length = reader.ReadSingle()
			};

			if (reader.IsEOF())
			{
				return result;
			}

			reader.ProcessChunks(chunk =>
			{
				if (chunk.id == SKELETON_ANIMATION_BASEINFO)
				{
					throw new Exception("Skeleton animation base info isn't supported.");
				}

				if (chunk.id != SKELETON_ANIMATION_TRACK)
				{
					return false;
				}

				result.Tracks.Add(ReadAnimationTrack());

				return true;
			});

			return result;
		}

		private Skeleton InternalLoad()
		{
			var result = new Skeleton();

			bones.Clear();
			var parentsToChild = new Dictionary<int, List<int>>();

			// Read version
			var version = reader.ReadHeader();
			if (version != "[Serializer_v1.10]")
			{
				throw new Exception($"Version {version} isn't supported.");
			}

			do
			{
				var chunk = reader.ReadChunk();

				switch (chunk.id)
				{
					case SKELETON_BONE:
						var bone = ReadBone();
						if (chunk.length > BoneSizeWithoutScale)
						{
							bone.Scale = reader.ReadVector3();
						}
						bones[bone.Handle] = bone;
						break;
					case SKELETON_BONE_PARENT:
						int childHandle = reader.ReadUInt16();
						int parentHandle = reader.ReadUInt16();

						if (!parentsToChild.ContainsKey(parentHandle))
						{
							parentsToChild[parentHandle] = new List<int>();
						}

						parentsToChild[parentHandle].Add(childHandle);
						break;
					case SKELETON_ANIMATION:
						var animation = ReadAnimation();
						result.Animations[animation.Name] = animation;
						break;
					case SKELETON_ANIMATION_LINK:
						throw new NotSupportedException("SKELETON_ANIMATION_LINK chunk isn't supported");
					default:
						break;
				}
			} while (!reader.IsEOF());

			// Set hierarchy
			foreach (var pair in parentsToChild)
			{
				foreach (var childHandle in pair.Value)
				{
					bones[pair.Key].Children.Add(bones[childHandle]);
				}
			}

			// Set bones
			var maxId = -1;
			foreach(var pair in bones)
			{
				if (pair.Key > maxId)
				{
					maxId = pair.Key;
				}
			}

			for(var i = 0; i <= maxId; ++i)
			{
				result.Bones.Add(null);
			}

			foreach(var pair in bones)
			{
				result.Bones[pair.Key] = pair.Value;
			}

			return result;
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
