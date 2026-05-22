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
        private GameModel _model = null!;
        private GameView _view = null!;
        private GameController _controller = null!;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            _model = new GameModel { Player = new PlayerModel(50, 300, 1080) };
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
            _view.Draw(gameTime);
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
