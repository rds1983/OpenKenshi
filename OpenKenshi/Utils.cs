using System.IO;

namespace OpenKenshi
{
	internal static class Utils
	{
		public static bool IsEOF(this BinaryReader reader)
		{
			return reader.PeekChar() == -1;
		}
	}
}
