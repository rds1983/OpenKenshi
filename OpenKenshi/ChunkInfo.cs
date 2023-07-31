namespace OpenKenshi
{
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
}
