using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nursia;

namespace OpenKenshi
{
	public class KenshiGame : Game
	{
		private readonly GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;
		private readonly FramesPerSecondCounter _fpsCounter = new FramesPerSecondCounter();

		public KenshiGame()
		{
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1200,
				PreferredBackBufferHeight = 800
			};

			Window.AllowUserResizing = true;
			IsMouseVisible = true;

			if (Configuration.NoFixedStep)
			{
				IsFixedTimeStep = false;
				_graphics.SynchronizeWithVerticalRetrace = false;
			}
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			_spriteBatch = new SpriteBatch(GraphicsDevice);

			// Nursia
			Nrs.Game = this;

			var loader = new MeshLoader();
			//			using (var stream = File.OpenRead(@"D:\SteamLibrary\steamapps\common\Kenshi\data\meshes\katana1.mesh"))
			using (var stream = File.OpenRead(@"D:\Temp\Sinbad\Sinbad.mesh"))
			{
				var model = loader.Load(stream);
			}
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			_fpsCounter.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			GraphicsDevice.Clear(Color.Black);

/*			_spriteBatch.Begin();

			_spriteBatch.Draw(_renderer.WaterReflection, 
				new Rectangle(0, 500, 600, 300), 
				Color.White);

			_spriteBatch.End();*/

			_fpsCounter.Draw(gameTime);
		}
	}
}