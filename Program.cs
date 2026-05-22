using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FumoGame.Controllers;
using FumoGame.Models;
using FumoGame.Views;

namespace FumoGame
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _scaleBatch = null!;
        private RenderTarget2D _renderTarget = null!;
        private GameModel _model = null!;
        private GameView _view = null!;
        private GameController _controller = null!;

        // Игра всегда рендерится в 1080p, потом растягивается на экран
        private const int GameWidth = 1920;
        private const int GameHeight = 1080;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = display.Width;
            _graphics.PreferredBackBufferHeight = display.Height;
            _graphics.HardwareModeSwitch = false; // borderless fullscreen, без смены разрешения монитора
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            _renderTarget = new RenderTarget2D(GraphicsDevice, GameWidth, GameHeight);
            _scaleBatch = new SpriteBatch(GraphicsDevice);

            // PlayerModel всегда получает игровую высоту 1080, а не 4К
            _model = new GameModel { Player = new PlayerModel(50, 300, GameHeight) };
            _view = new GameView(_model, GraphicsDevice);
            _controller = new GameController(_model, _view);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _view.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _controller.HandleInput(Keyboard.GetState(), Mouse.GetState());
            _view.UpdateMusic();

            if (_model.State == GameState.Playing)
                _view.UpdateGame(gameTime);
            else if (_model.State == GameState.GameOver)
                _view.UpdateGameOver(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // 1. Рисуем игру в render target (1920x1080)
            GraphicsDevice.SetRenderTarget(_renderTarget);
            _view.Draw(gameTime);

            // 2. Растягиваем render target на весь экран (4K или любое другое)
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            int screenW = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int screenH = GraphicsDevice.PresentationParameters.BackBufferHeight;
            _scaleBatch.Begin();
            _scaleBatch.Draw(_renderTarget, new Rectangle(0, 0, screenW, screenH), Color.White);
            _scaleBatch.End();

            base.Draw(gameTime);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new Game1();
            game.Run();
        }
    }
}
