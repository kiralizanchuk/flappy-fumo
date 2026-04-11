using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FumoGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;

        private GameState _gameState = GameState.Menu;
        private Player _player;
        private List<Pipe> _pipes = new();
        private int _score = 0;
        private int _highScore = 0;
        private double _pipeSpawnTimer = 0;
        private const double PIPE_SPAWN_INTERVAL = 2.0; // секунды

        private Texture2D _pixelTexture;
        private Texture2D _pipeTexture;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Установим размер окна
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
        }

        protected override void Initialize()
        {
            _player = new Player(50, 300);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            // Загрузим шрифт (если файл существует)
            try
            {
                _font = Content.Load<SpriteFont>("Arial");
            }
            catch
            {
                // Если нет шрифта, создадим текстуру для отладки
            }

            // Создадим простые текстуры из пикселей
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            _pipeTexture = CreatePipeTexture();
        }

        private Texture2D CreatePipeTexture()
        {
            var texture = new Texture2D(GraphicsDevice, 60, 1);
            var data = new Color[60];
            for (int i = 0; i < 60; i++)
                data[i] = Color.ForestGreen;
            texture.SetData(data);
            return texture;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            switch (_gameState)
            {
                case GameState.Menu:
                    if (keyboardState.IsKeyDown(Keys.Space) || mouseState.LeftButton == ButtonState.Pressed)
                    {
                        StartNewGame();
                    }
                    break;

                case GameState.Playing:
                    UpdateGame(gameTime, keyboardState, mouseState);
                    break;

                case GameState.GameOver:
                    if (keyboardState.IsKeyDown(Keys.Space) || mouseState.LeftButton == ButtonState.Pressed)
                    {
                        _gameState = GameState.Menu;
                    }
                    break;
            }

            base.Update(gameTime);
        }

        private void UpdateGame(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
        {
            // Обновляем игрока
            _player.Update(gameTime);

            // Обработка прыжка
            if (keyboardState.IsKeyDown(Keys.Space) || mouseState.LeftButton == ButtonState.Pressed)
            {
                _player.Jump();
            }

            // Генерация препятствий
            _pipeSpawnTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_pipeSpawnTimer >= PIPE_SPAWN_INTERVAL)
            {
                _pipeSpawnTimer = 0;
                int gap = 100;
                int maxTopHeight = GraphicsDevice.Viewport.Height - gap - 50;
                int topHeight = Random.Shared.Next(50, Math.Max(51, maxTopHeight));
                _pipes.Add(new Pipe(GraphicsDevice.Viewport.Width, topHeight, gap));
            }

            // Обновляем трубы
            foreach (var pipe in _pipes)
            {
                pipe.Update(gameTime);
            }

            // Удаляем трубы, вышедшие за пределы экрана
            _pipes.RemoveAll(p => p.X < -100);

            // Проверяем столкновения
            foreach (var pipe in _pipes)
            {
                if (_player.CheckCollision(pipe))
                {
                    _gameState = GameState.GameOver;
                    if (_score > _highScore)
                        _highScore = _score;
                }

                // Добавляем очко, если прошли трубу
                if (!pipe.Scored && pipe.X + pipe.Width < _player.X)
                {
                    pipe.Scored = true;
                    _score++;
                }
            }

            // Проверяем границы экрана
            if (_player.Y > GraphicsDevice.Viewport.Height || _player.Y < 0)
            {
                _gameState = GameState.GameOver;
                if (_score > _highScore)
                    _highScore = _score;
            }
        }

        private void StartNewGame()
        {
            _player.Reset();
            _pipes.Clear();
            _score = 0;
            _pipeSpawnTimer = 0;
            _gameState = GameState.Playing;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            switch (_gameState)
            {
                case GameState.Menu:
                    DrawMenu();
                    break;

                case GameState.Playing:
                    DrawGame();
                    break;

                case GameState.GameOver:
                    DrawGameOver();
                    break;
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawMenu()
        {
            DrawTextCentered("FUMO BIRD", 150, Color.White, 2);
            DrawTextCentered("Нажмите ПРОБЕЛ или клик мышью", 300, Color.White, 1);
            DrawTextCentered("чтобы начать игру", 340, Color.White, 1);
            DrawTextCentered($"Лучший результат: {_highScore}", 450, Color.Yellow, 1);
        }

        private void DrawGame()
        {
            // Рисуем игрока
            _spriteBatch.Draw(_pixelTexture, new Rectangle(_player.X, _player.Y, _player.Width, _player.Height), Color.Yellow);

            // Рисуем трубы
            foreach (var pipe in _pipes)
            {
                // Верхняя труба
                if (pipe.TopHeight > 0)
                {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(pipe.X, 0, pipe.Width, pipe.TopHeight), Color.ForestGreen);
                }

                // Нижняя труба
                int bottomStart = pipe.TopHeight + pipe.Gap;
                int bottomHeight = GraphicsDevice.Viewport.Height - bottomStart;
                if (bottomHeight > 0)
                {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(pipe.X, bottomStart, pipe.Width, bottomHeight), Color.ForestGreen);
                }
            }

            // Рисуем счет
            DrawTextCentered($"Счет: {_score}", 20, Color.White, 1);
        }

        private void DrawGameOver()
        {
            DrawTextCentered("GAME OVER", 150, Color.Red, 2);
            DrawTextCentered($"Ваш результат: {_score}", 250, Color.White, 1);
            DrawTextCentered($"Лучший результат: {_highScore}", 300, Color.Yellow, 1);
            DrawTextCentered("Нажмите ПРОБЕЛ чтобы вернуться в меню", 400, Color.White, 1);
        }

        private void DrawTextCentered(string text, int y, Color color, int scale)
        {
            if (_font != null)
            {
                var fontHeight = _font.LineSpacing * scale;
                var measure = _font.MeasureString(text) * scale;
                var x = (GraphicsDevice.Viewport.Width - measure.X) / 2;
                _spriteBatch.DrawString(_font, text, new Vector2(x, y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }
    }

    public enum GameState
    {
        Menu,
        Playing,
        GameOver
    }

    public class Player
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; } = 30;
        public int Height { get; } = 30;

        private float _velocityY = 0;
        private const float GRAVITY = 300;
        private const float JUMP_POWER = -200;
        private int _startY;

        public Player(int x, int y)
        {
            X = x;
            Y = y;
            _startY = y;
        }

        public void Update(GameTime gameTime)
        {
            _velocityY += GRAVITY * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Y += (int)(_velocityY * (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        public void Jump()
        {
            if (_velocityY > JUMP_POWER)
                _velocityY = JUMP_POWER;
        }

        public void Reset()
        {
            Y = _startY;
            _velocityY = 0;
        }

        public bool CheckCollision(Pipe pipe)
        {
            Rectangle playerRect = new Rectangle(X, Y, Width, Height);
            Rectangle topPipeRect = new Rectangle(pipe.X, 0, pipe.Width, pipe.TopHeight);
            Rectangle bottomPipeRect = new Rectangle(pipe.X, pipe.TopHeight + pipe.Gap, pipe.Width, 600 - (pipe.TopHeight + pipe.Gap));

            return playerRect.Intersects(topPipeRect) || playerRect.Intersects(bottomPipeRect);
        }
    }

    public class Pipe
    {
        public int X { get; set; }
        public int Width { get; } = 60;
        public int TopHeight { get; }
        public int Gap { get; }
        public bool Scored { get; set; } = false;

        public Pipe(int x, int topHeight, int gap)
        {
            X = x;
            TopHeight = topHeight;
            Gap = gap;
        }

        public void Update(GameTime gameTime)
        {
            X -= (int)(200 * gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }
}
