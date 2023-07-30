﻿namespace OpenKenshi
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

			using (var game = new KenshiGame())
			{
				game.Run();
			}
		}
	}
}
