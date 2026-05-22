using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using FumoGame.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace FumoGame.Views
{
    public class GameView
    {
        private readonly GameModel _model;
        private readonly GraphicsDevice _graphicsDevice;

        private SpriteBatch _spriteBatch = null!;
        private SpriteFont? _font;
        private Texture2D _pixelTexture = null!;
        private Texture2D? _pipeTexture;
        private Texture2D? _playerTexture;
        private Texture2D? _backgroundTexture;
        private List<Texture2D> _gameOverFrames = new();
        private Song? _music;
        private GameState _prevMusicState = GameState.Playing;

        private const float Gravity = 300f;
        private const float JumpPower = -200f;
        private const double FrameDuration = 0.08;
        private const int PipeGap = 185;
        private const int PlayerMargin = 5;
        private const int PipeMargin = 5;

        private const float PipeSpeed = 200f;
        private const double PipeInterval = 1.8;
        private const int MovingPipeGap = 220;
        private const float MaxMovingPipeSpeed = 120f;

        public GameView(GameModel model, GraphicsDevice graphicsDevice)
        {
            _model = model;
            _graphicsDevice = graphicsDevice;
        }

        public void LoadContent(ContentManager content)
        {
            _spriteBatch = new SpriteBatch(_graphicsDevice);

            try { _font = content.Load<SpriteFont>("Arial"); } catch { }

            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            _playerTexture = TryLoadTexture("player.png");
            _pipeTexture = TryLoadTexture("pipe.png") ?? CreatePipeTexture();
            _backgroundTexture = TryLoadTexture("background.png");

            for (int i = 0; i < 100; i++)
            {
                var frame = TryLoadTexture($"fumofumo_{i:D2}.png");
                if (frame == null) break;
                _gameOverFrames.Add(frame);
            }

            try
            {
                string musicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "baka.mp3");
                _music = Song.FromUri("baka", new Uri(musicPath));
            }
            catch { }
        }

        // --- Music ---

        public void UpdateMusic()
        {
            if (_music == null) return;
            bool shouldPlay = _model.State == GameState.Menu || _model.State == GameState.GameOver;
            bool wasPlaying = _prevMusicState == GameState.Menu || _prevMusicState == GameState.GameOver;

            if (shouldPlay && !wasPlaying)
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(_music);
            }
            else if (!shouldPlay && wasPlaying)
            {
                MediaPlayer.Stop();
            }

            _prevMusicState = _model.State;
        }

        // --- Game Logic ---

        public void Jump()
        {
            if (_model.Player.VelocityY > JumpPower)
                _model.Player.VelocityY = JumpPower;
        }

        public void StartNewGame()
        {
            var p = _model.Player;
            p.Y = p.StartY;
            p.VelocityY = 0;
            _model.Pipes.Clear();
            _model.Score = 0;
            _model.PipeSpawnTimer = 0;
            _model.State = GameState.Playing;
        }

        public void GoToMenu()
        {
            _model.State = GameState.Menu;
            _model.AnimationFrameIndex = 0;
            _model.AnimationFrameTime = 0;
        }

        public void UpdateGame(GameTime gameTime)
        {
            double dt = gameTime.ElapsedGameTime.TotalSeconds;
            var player = _model.Player;
            int viewH = _graphicsDevice.Viewport.Height;
            int viewW = _graphicsDevice.Viewport.Width;

            player.VelocityY += Gravity * (float)dt;
            player.Y += (int)(player.VelocityY * (float)dt);

            _model.PipeSpawnTimer += dt;
            if (_model.PipeSpawnTimer >= PipeInterval)
            {
                _model.PipeSpawnTimer = 0;
                _model.Pipes.Add(SpawnPipe(viewW, viewH));
            }

            foreach (var pipe in _model.Pipes)
            {
                pipe.X -= (int)(PipeSpeed * dt);

                if (pipe.Type == PipeType.Moving)
                {
                    pipe.TopHeight += (int)(pipe.MoveSpeed * pipe.MoveDirection * dt);
                    if (pipe.TopHeight >= pipe.MaxTopHeight)
                    {
                        pipe.TopHeight = pipe.MaxTopHeight;
                        pipe.MoveDirection = -1;
                    }
                    else if (pipe.TopHeight <= pipe.MinTopHeight)
                    {
                        pipe.TopHeight = pipe.MinTopHeight;
                        pipe.MoveDirection = 1;
                    }
                }
            }

            _model.Pipes.RemoveAll(p => p.X < -100);

            foreach (var pipe in _model.Pipes)
            {
                if (!_model.GodMode && CheckCollision(player, pipe, viewH))
                {
                    if (_model.Score > _model.HighScore) _model.HighScore = _model.Score;
                    _model.State = GameState.GameOver;
                    return;
                }

                if (!pipe.Scored && pipe.X + pipe.Width < player.X)
                {
                    pipe.Scored = true;
                    _model.Score += pipe.Type == PipeType.Narrow ? 3 : 1;
                }
            }

            if (!_model.GodMode && (player.Y > viewH || player.Y < 0))
            {
                if (_model.Score > _model.HighScore) _model.HighScore = _model.Score;
                _model.State = GameState.GameOver;
            }
        }

        public void UpdateGameOver(GameTime gameTime)
        {
            if (_gameOverFrames.Count == 0) return;
            _model.AnimationFrameTime += gameTime.ElapsedGameTime.TotalSeconds;
            if (_model.AnimationFrameTime >= FrameDuration)
            {
                _model.AnimationFrameTime = 0;
                _model.AnimationFrameIndex = (_model.AnimationFrameIndex + 1) % _gameOverFrames.Count;
            }
        }

        // --- Drawing ---

        public void Draw(GameTime gameTime)
        {
            _graphicsDevice.Clear(_model.State == GameState.GameOver ? Color.White : Color.CornflowerBlue);

            _spriteBatch.Begin();
            switch (_model.State)
            {
                case GameState.Menu: DrawMenu(); break;
                case GameState.Playing: DrawGame(); break;
                case GameState.GameOver: DrawGameOver(); break;
            }
            if (_model.GodMode)
                DrawGodModeIndicator();
            _spriteBatch.End();
        }

        // --- Private helpers ---

        private PipeModel SpawnPipe(int viewW, int viewH)
        {
            int roll = Random.Shared.Next(100);

            if (roll < 50)
            {
                int margin = 120;
                int movingMaxTop = viewH - MovingPipeGap - margin;
                int topHeight = Random.Shared.Next(margin, Math.Max(margin + 1, movingMaxTop));
                float moveSpeed = MaxMovingPipeSpeed;
                return new PipeModel(viewW, topHeight, MovingPipeGap, PipeType.Moving, moveSpeed, margin, movingMaxTop);
            }

            int maxTop = viewH - PipeGap - 50;
            int normalTopHeight = Random.Shared.Next(50, Math.Max(51, maxTop));

            if (roll < 65)
                return new PipeModel(viewW, normalTopHeight, PipeGap, PipeType.Narrow);

            return new PipeModel(viewW, normalTopHeight, PipeGap);
        }

        private bool CheckCollision(PlayerModel player, PipeModel pipe, int screenHeight)
        {
            var playerRect = new Rectangle(player.X + PlayerMargin, player.Y + PlayerMargin,
                player.Width - PlayerMargin * 2, player.Height - PlayerMargin * 2);
            var topRect = new Rectangle(pipe.X + PipeMargin, 0,
                pipe.Width - PipeMargin * 2, pipe.TopHeight);
            var bottomRect = new Rectangle(pipe.X + PipeMargin, pipe.TopHeight + pipe.Gap,
                pipe.Width - PipeMargin * 2, screenHeight - (pipe.TopHeight + pipe.Gap));
            return playerRect.Intersects(topRect) || playerRect.Intersects(bottomRect);
        }

        private void DrawMenu()
        {
            DrawTextCentered("FLAPPY FUMO", 150, Color.White, 2);
            DrawTextCentered("Нажмите ПРОБЕЛ или клик мышью", 300, Color.White, 1);
            DrawTextCentered("чтобы начать игру", 340, Color.White, 1);
            DrawTextCentered($"Лучший результат: {_model.HighScore}", 450, Color.Yellow, 1);
        }

        private void DrawGame()
        {
            int viewW = _graphicsDevice.Viewport.Width;
            int viewH = _graphicsDevice.Viewport.Height;
            var player = _model.Player;

            if (_backgroundTexture != null)
            {
                float sx = (float)viewW / _backgroundTexture.Width;
                float sy = (float)viewH / _backgroundTexture.Height;
                _spriteBatch.Draw(_backgroundTexture, Vector2.Zero, null, Color.White, 0, Vector2.Zero,
                    new Vector2(sx, sy), SpriteEffects.None, 0);
            }

            if (_playerTexture != null)
            {
                float sx = (float)player.Width / _playerTexture.Width;
                float sy = (float)player.Height / _playerTexture.Height;
                _spriteBatch.Draw(_playerTexture, new Vector2(player.X, player.Y), null, Color.White, 0,
                    Vector2.Zero, new Vector2(sx, sy), SpriteEffects.None, 0);
            }
            else
            {
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X, player.Y, player.Width, player.Height), Color.Yellow);
            }

            foreach (var pipe in _model.Pipes)
                DrawPipe(pipe, viewH);

            DrawTextCentered($"Счет: {_model.Score}", 20, Color.White, 1);
        }

        private void DrawPipe(PipeModel pipe, int viewH)
        {
            Color tint = pipe.Type switch
            {
                PipeType.Moving => Color.DodgerBlue,
                PipeType.Narrow => Color.Gold,
                _ => Color.White,
            };

            int bottomStart = pipe.TopHeight + pipe.Gap;

            if (_pipeTexture != null)
            {
                DrawTiledPipe(pipe.X, 0, pipe.TopHeight, pipe.Width, SpriteEffects.FlipVertically, tint);
                DrawTiledPipe(pipe.X, bottomStart, viewH - bottomStart, pipe.Width, SpriteEffects.None, tint);
            }
            else
            {
                Color fallback = pipe.Type switch
                {
                    PipeType.Moving => Color.DodgerBlue,
                    PipeType.Narrow => Color.Gold,
                    _ => Color.ForestGreen,
                };
                _spriteBatch.Draw(_pixelTexture, new Rectangle(pipe.X, 0, pipe.Width, pipe.TopHeight), fallback);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(pipe.X, bottomStart, pipe.Width, viewH - bottomStart), fallback);
            }

            if (pipe.Type == PipeType.Narrow)
                DrawTextCentered("+3", pipe.TopHeight + pipe.Gap / 2 - 14, Color.Gold, 1);
        }

        private void DrawTiledPipe(int x, int startY, int totalHeight, int width, SpriteEffects effects, Color tint = default)
        {
            if (_pipeTexture == null || totalHeight <= 0) return;
            if (tint == default) tint = Color.White;
            int texH = _pipeTexture.Height;
            int y = startY;
            while (y < startY + totalHeight)
            {
                int drawH = Math.Min(texH, startY + totalHeight - y);
                _spriteBatch.Draw(_pipeTexture, new Rectangle(x, y, width, drawH),
                    new Rectangle(0, 0, _pipeTexture.Width, drawH),
                    tint, 0, Vector2.Zero, effects, 0);
                y += drawH;
            }
        }

        private void DrawGameOver()
        {
            if (_font != null)
            {
                int w = _graphicsDevice.Viewport.Width;
                int h = _graphicsDevice.Viewport.Height;
                var measure = _font.MeasureString("BAKA ");
                int stepX = (int)measure.X;
                int stepY = (int)measure.Y + 4;
                for (int y = 0; y < h; y += stepY)
                    for (int x = 0; x < w; x += stepX)
                        _spriteBatch.DrawString(_font, "BAKA ", new Vector2(x, y), Color.CornflowerBlue);
            }

            if (_gameOverFrames.Count > 0)
            {
                var tex = _gameOverFrames[_model.AnimationFrameIndex];
                int x = (_graphicsDevice.Viewport.Width - tex.Width) / 2;
                int y = (_graphicsDevice.Viewport.Height - tex.Height) / 2;
                _spriteBatch.Draw(tex, new Rectangle(x, y, tex.Width, tex.Height), Color.White);
            }
            DrawGameOverPanel();
        }

        private void DrawGameOverPanel()
        {
            if (_font == null) return;
            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;

            string scoreLine = $"Счет: {_model.Score}      Рекорд: {_model.HighScore}";
            string spaceLine = "Нажмите ПРОБЕЛ чтобы вернуться в меню";

            var scoreSize = _font.MeasureString(scoreLine);
            var spaceSize = _font.MeasureString(spaceLine);

            int panelW = (int)Math.Max(scoreSize.X, spaceSize.X) + 80;
            int panelH = (int)(scoreSize.Y + spaceSize.Y) + 50;
            int panelX = (w - panelW) / 2;
            int panelY = h - panelH - 40;

            int border = 4;
            // Рамка (cornflower blue)
            _spriteBatch.Draw(_pixelTexture, new Rectangle(panelX - border, panelY - border, panelW + border * 2, panelH + border * 2), Color.CornflowerBlue);
            // Фон панели (тёмно-синий)
            _spriteBatch.Draw(_pixelTexture, new Rectangle(panelX, panelY, panelW, panelH), Color.MidnightBlue);

            // Текст
            float scoreX = panelX + (panelW - scoreSize.X) / 2;
            float spaceX = panelX + (panelW - spaceSize.X) / 2;
            float scoreY = panelY + 18;
            float spaceY = scoreY + scoreSize.Y + 10;

            _spriteBatch.DrawString(_font, scoreLine, new Vector2(scoreX, scoreY), Color.White);
            _spriteBatch.DrawString(_font, spaceLine, new Vector2(spaceX, spaceY), Color.CornflowerBlue);
        }

        private void DrawGodModeIndicator()
        {
            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;
            int b = 8;
            Color c = Color.HotPink;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, w, b), c);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, h - b, w, b), c);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, b, h), c);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(w - b, 0, b, h), c);
            DrawTextCentered("GOD MODE", 20, Color.HotPink, 1);
        }

        private void DrawTextCentered(string text, int y, Color color, int scale)
        {
            if (_font == null) return;
            var measure = _font.MeasureString(text) * scale;
            float x = (_graphicsDevice.Viewport.Width - measure.X) / 2;
            _spriteBatch.DrawString(_font, text, new Vector2(x, y), color, 0,
                Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private Texture2D? TryLoadTexture(string filename)
        {
            string[] paths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", filename),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Content", filename),
                Path.Combine(Environment.CurrentDirectory, "Content", filename),
            };
            foreach (var path in paths)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    using var stream = File.OpenRead(path);
                    return Texture2D.FromStream(_graphicsDevice, stream);
                }
                catch { }
            }
            return null;
        }

        private Texture2D CreatePipeTexture()
        {
            int width = 60, height = 256;
            var tex = new Texture2D(_graphicsDevice, width, height);
            var data = new Color[width * height];
            Array.Fill(data, Color.ForestGreen);
            tex.SetData(data);
            return tex;
        }
    }
}
