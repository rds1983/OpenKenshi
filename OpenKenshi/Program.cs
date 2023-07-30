using System;
using System.IO;
using System.Text;

namespace OpenKenshi
{
	class Program
	{
		static void Main(string[] args)
		{
			foreach (var arg in args)
			{
				if (arg == "/nf")
				{
					Configuration.NoFixedStep = true;
				}
			}

			var loader = new MeshLoader();
			//			using (var stream = File.OpenRead(@"D:\SteamLibrary\steamapps\common\Kenshi\data\meshes\katana1.mesh"))
			using (var stream = File.OpenRead(@"D:\Temp\Sinbad\Sinbad.mesh"))
			{
				var model = loader.Load(stream);

				var k = 5;
			}

			/*			using (var game = new SampleGame())
						{
							game.Run();
						}*/
		}
	}
}
