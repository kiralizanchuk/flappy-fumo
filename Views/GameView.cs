using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using FumoGame.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private Texture2D _coinTexture = null!;
        private Texture2D _shieldTexture = null!;
        private Texture2D _slowTexture = null!;
        private Texture2D _heartFullTexture = null!;
        private Texture2D _heartEmptyTexture = null!;
        private Texture2D? _logoTexture;
        private Texture2D? _leaderboardTexture;
        private List<Texture2D> _gameOverFrames = new();
        private Song? _music;
        private Song? _gameplayMusic;
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

        private const float InvincibilityDuration = 1.5f;
        private const float ShieldDuration = 4f;
        private const float SlowDuration = 4f;
        private const float SlowFactor = 0.5f;
        private const int CoinSpawnChance = 65;

        private const int MaxGapShift = 180; // макс. вертикальный сдвиг зазора между трубами
        private int _lastGapCenter = 540;  // середина экрана 1080p

        private string _scoresPath = "";

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

            _heartFullTexture = CreateHeartTexture(40, Color.Red);
            _heartEmptyTexture = CreateHeartTexture(40, new Color(80, 80, 80));

            _coinTexture = TryLoadTexture("coin.png") ?? CreateCircleTexture(14, Color.Gold);
            _shieldTexture = TryLoadTexture("shield.png") ?? CreateCircleTexture(14, Color.DeepSkyBlue);
            _slowTexture = TryLoadTexture("slow.png") ?? CreateCircleTexture(14, Color.LimeGreen);

            _logoTexture = TryLoadTexture("logo.png");
            _leaderboardTexture = TryLoadTexture("leaderboard.png");
            _playerTexture = TryLoadTexture("player.png");
            _pipeTexture = TryLoadTexture("pipe.png") ?? CreatePipeTexture();
            _backgroundTexture = TryLoadTexture("background.png");

            for (int i = 0; i < 100; i++)
            {
                var frame = TryLoadTexture($"fumofumo_{i:D2}.png");
                if (frame == null) break;
                _gameOverFrames.Add(frame);
            }

            try { _music = content.Load<Song>("baka"); } catch { }
            try { _gameplayMusic = content.Load<Song>("gameplay"); } catch { }

            _scoresPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scores.txt");
            LoadScores();
        }

        // --- Music ---

        public void UpdateMusic()
        {
            bool isGameOver = _model.State == GameState.GameOver;
            bool wasGameOver = _prevMusicState == GameState.GameOver;
            bool isPlaying = _model.State == GameState.Playing;
            bool wasPlaying = _prevMusicState == GameState.Playing;

            if (isGameOver && !wasGameOver)
            {
                MediaPlayer.Stop();
                if (_music != null)
                {
                    MediaPlayer.IsRepeating = true;
                    MediaPlayer.Play(_music);
                }
            }
            else if (isPlaying && !wasPlaying)
            {
                MediaPlayer.Stop();
                if (_gameplayMusic != null)
                {
                    MediaPlayer.IsRepeating = true;
                    MediaPlayer.Play(_gameplayMusic);
                }
            }
            else if (!isGameOver && wasGameOver && !isPlaying)
            {
                MediaPlayer.Stop();
            }
            else if (!isPlaying && wasPlaying)
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
            _model.Coins.Clear();
            _model.Score = 0;
            _model.PipeSpawnTimer = 0;
            _lastGapCenter = 540;
            _model.Lives = 3;
            _model.InvincibilityTimer = 0f;
            _model.ShieldTimer = 0f;
            _model.SlowTimer = 0f;
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
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var player = _model.Player;
            int viewH = _graphicsDevice.Viewport.Height;
            int viewW = _graphicsDevice.Viewport.Width;

            // Обновляем таймеры
            if (_model.InvincibilityTimer > 0) _model.InvincibilityTimer -= dt;
            if (_model.ShieldTimer > 0) _model.ShieldTimer -= dt;
            if (_model.SlowTimer > 0) _model.SlowTimer -= dt;

            player.VelocityY += Gravity * dt;
            player.Y += (int)(player.VelocityY * dt);

            _model.PipeSpawnTimer += dt;
            if (_model.PipeSpawnTimer >= PipeInterval)
            {
                _model.PipeSpawnTimer = 0;
                var pipe = SpawnPipe(viewW, viewH);
                _model.Pipes.Add(pipe);
                MaybeSpawnCoin(pipe, viewH);
            }

            float currentSpeed = _model.SlowTimer > 0 ? PipeSpeed * SlowFactor : PipeSpeed;
            float slowMoveScale = _model.SlowTimer > 0 ? SlowFactor : 1f;

            foreach (var pipe in _model.Pipes)
            {
                pipe.X -= (int)(currentSpeed * dt);

                if (pipe.Type == PipeType.Moving)
                {
                    pipe.TopHeight += (int)(pipe.MoveSpeed * pipe.MoveDirection * slowMoveScale * dt);
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

            foreach (var coin in _model.Coins)
                coin.X -= (int)(currentSpeed * dt);

            _model.Pipes.RemoveAll(p => p.X < -100);
            _model.Coins.RemoveAll(c => c.Collected || c.X < -50);

            // Сбор монет и бонусов
            var playerRect = new Rectangle(
                player.X + PlayerMargin, player.Y + PlayerMargin,
                player.Width - PlayerMargin * 2, player.Height - PlayerMargin * 2);

            foreach (var coin in _model.Coins)
            {
                if (coin.Collected) continue;
                if (playerRect.Intersects(new Rectangle(coin.X, coin.Y, coin.Width, coin.Height)))
                {
                    coin.Collected = true;
                    switch (coin.Type)
                    {
                        case PowerUpType.Coin: _model.Score += 3; break;
                        case PowerUpType.Shield: _model.ShieldTimer = ShieldDuration; break;
                        case PowerUpType.Slow: _model.SlowTimer = SlowDuration; break;
                    }
                }
            }

            bool isInvincible = _model.GodMode || _model.InvincibilityTimer > 0 || _model.ShieldTimer > 0;

            // Столкновение с трубами
            foreach (var pipe in _model.Pipes)
            {
                if (!isInvincible && CheckCollision(player, pipe, viewH))
                {
                    _model.Lives--;
                    if (_model.Lives <= 0)
                    {
                        if (_model.Score > _model.HighScore) _model.HighScore = _model.Score;
                        SaveScore(_model.Score);
                        _model.State = GameState.GameOver;
                        return;
                    }
                    _model.InvincibilityTimer = InvincibilityDuration;
                    break;
                }

                if (!pipe.Scored && pipe.X + pipe.Width < player.X)
                {
                    pipe.Scored = true;
                    _model.Score += 1;
                }
            }

            // Вылет за экран
            if (!isInvincible && (player.Y > viewH || player.Y < 0))
            {
                _model.Lives--;
                if (_model.Lives <= 0)
                {
                    if (_model.Score > _model.HighScore) _model.HighScore = _model.Score;
                    SaveScore(_model.Score);
                    _model.State = GameState.GameOver;
                }
                else
                {
                    player.Y = player.StartY;
                    player.VelocityY = 0;
                    _model.InvincibilityTimer = InvincibilityDuration;
                }
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
            Color bgColor = _model.State switch
            {
                GameState.GameOver => Color.White,
                GameState.Playing => GetSkyColor(),
                _ => Color.CornflowerBlue,
            };
            _graphicsDevice.Clear(bgColor);

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

        // --- Sky color by score ---

        private Color GetSkyColor()
        {
            int phase = (_model.Score / 10) % 4;
            return phase switch
            {
                0 => Color.CornflowerBlue,
                1 => new Color(255, 140, 60),
                2 => new Color(15, 15, 60),
                3 => new Color(55, 55, 65),
                _ => Color.CornflowerBlue,
            };
        }

        // --- Private helpers ---

        private void MaybeSpawnCoin(PipeModel pipe, int viewH)
        {
            if (Random.Shared.Next(100) >= CoinSpawnChance) return;
            int gapMid = pipe.TopHeight + pipe.Gap / 2 - 22;
            int coinX = pipe.X + pipe.Width / 2 - 22;
            int roll = Random.Shared.Next(100);
            PowerUpType type = roll < 70 ? PowerUpType.Coin
                             : roll < 85 ? PowerUpType.Shield
                             : PowerUpType.Slow;
            _model.Coins.Add(new CoinModel(coinX, gapMid, type));
        }

        private void LoadScores()
        {
            _model.TopScores.Clear();
            if (!File.Exists(_scoresPath)) return;
            try
            {
                foreach (var line in File.ReadAllLines(_scoresPath))
                    if (int.TryParse(line, out int s))
                        _model.TopScores.Add(s);
            }
            catch { }
        }

        private void SaveScore(int score)
        {
            if (score <= 0) return;
            _model.TopScores.Add(score);
            _model.TopScores = _model.TopScores.OrderByDescending(s => s).Take(5).ToList();
            try { File.WriteAllLines(_scoresPath, _model.TopScores.Select(s => s.ToString())); }
            catch { }
        }

        private PipeModel SpawnPipe(int viewW, int viewH)
        {
            int roll = Random.Shared.Next(100);

            if (roll < 40)
            {
                int margin = 120;
                int movingMaxTop = viewH - MovingPipeGap - margin;
                int half = MovingPipeGap / 2;
                int minTop = Math.Max(margin, _lastGapCenter - half - MaxGapShift);
                int maxTop = Math.Min(movingMaxTop, _lastGapCenter - half + MaxGapShift);
                if (minTop >= maxTop) maxTop = minTop + 1;
                int topHeight = Random.Shared.Next(minTop, maxTop);
                _lastGapCenter = topHeight + half;
                return new PipeModel(viewW, topHeight, MovingPipeGap, PipeType.Moving, MaxMovingPipeSpeed, margin, movingMaxTop);
            }

            int maxTopN = viewH - PipeGap - 50;
            int halfN = PipeGap / 2;
            int minTopN = Math.Max(50, _lastGapCenter - halfN - MaxGapShift);
            int maxTopN2 = Math.Min(maxTopN, _lastGapCenter - halfN + MaxGapShift);
            if (minTopN >= maxTopN2) maxTopN2 = minTopN + 1;
            int normalTopHeight = Random.Shared.Next(minTopN, maxTopN2);
            _lastGapCenter = normalTopHeight + halfN;
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
            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;

            // --- Лого по центру ---
            if (_logoTexture != null)
            {
                // Ширина логотипа — половина экрана, высота пропорционально
                int logoW = w / 2;
                int logoH = (int)((float)_logoTexture.Height / _logoTexture.Width * logoW);
                int logoX = (w - logoW) / 2;
                int logoY = h / 2 - logoH / 2 - 60;
                _spriteBatch.Draw(_logoTexture, new Rectangle(logoX, logoY, logoW, logoH), Color.White);
            }
            else
            {
                DrawTextCentered("FLAPPY FUMO", 80, Color.White, 2);
            }

            DrawTextCentered("ПРОБЕЛ или клик - начать игру", h / 2 + 100, Color.White, 1);

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

            foreach (var pipe in _model.Pipes)
                DrawPipe(pipe, viewH);

            // Монеты и бонусы
            foreach (var coin in _model.Coins)
            {
                if (coin.Collected) continue;
                var tex = coin.Type switch
                {
                    PowerUpType.Shield => _shieldTexture,
                    PowerUpType.Slow => _slowTexture,
                    _ => _coinTexture,
                };
                _spriteBatch.Draw(tex, new Rectangle(coin.X, coin.Y, coin.Width, coin.Height), Color.White);

            }

            // Игрок (мигает при неуязвимости)
            bool blink = _model.InvincibilityTimer > 0 && (int)(_model.InvincibilityTimer * 10) % 2 == 0;
            if (!blink)
            {
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
            }

            // Щит — синяя аура вокруг игрока
            if (_model.ShieldTimer > 0)
            {
                int aura = 8;
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X - aura, player.Y - aura, player.Width + aura * 2, aura),
                    Color.DeepSkyBlue * 0.6f);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X - aura, player.Y + player.Height, player.Width + aura * 2, aura),
                    Color.DeepSkyBlue * 0.6f);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X - aura, player.Y - aura, aura, player.Height + aura * 2),
                    Color.DeepSkyBlue * 0.6f);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X + player.Width, player.Y - aura, aura, player.Height + aura * 2),
                    Color.DeepSkyBlue * 0.6f);
            }

            DrawTextCentered($"Счет: {_model.Score}", 20, Color.White, 1);

            // Сердечки (жизни) — правый верхний угол
            DrawHearts();

            // Активные бонусы
            DrawPowerUpTimers();
        }

        private void DrawHearts()
        {
            int heartSize = 40;
            int gap = 4;
            int total = 3;
            int startX = _graphicsDevice.Viewport.Width - (heartSize + gap) * total - 10;
            int startY = 10;

            for (int i = 0; i < total; i++)
            {
                int hx = startX + i * (heartSize + gap);
                var tex = i < _model.Lives ? _heartFullTexture : _heartEmptyTexture;
                _spriteBatch.Draw(tex, new Rectangle(hx, startY, tex.Width, tex.Height), Color.White);
            }
        }

        private void DrawPowerUpTimers()
        {
            if (_font == null) return;
            int px = 10;
            int viewH = _graphicsDevice.Viewport.Height;
            int lineH = (int)_font.MeasureString("A").Y + 6;
            int row = 0;

            if (_model.ShieldTimer > 0)
            {
                string txt = $"ЩИТ {_model.ShieldTimer:F1}с";
                var sz = _font.MeasureString(txt);
                int py = viewH - lineH * (row + 1);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(px - 4, py - 2, (int)sz.X + 8, (int)sz.Y + 4),
                    Color.DarkBlue * 0.8f);
                _spriteBatch.DrawString(_font, txt, new Vector2(px, py), Color.DeepSkyBlue);
                row++;
            }

            if (_model.SlowTimer > 0)
            {
                string txt = $"ЗАМЕДЛЕНИЕ {_model.SlowTimer:F1}с";
                var sz = _font.MeasureString(txt);
                int py = viewH - lineH * (row + 1);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(px - 4, py - 2, (int)sz.X + 8, (int)sz.Y + 4),
                    Color.DarkGreen * 0.8f);
                _spriteBatch.DrawString(_font, txt, new Vector2(px, py), Color.LimeGreen);
                row++;
            }
        }

        private void DrawPipe(PipeModel pipe, int viewH)
        {
            Color tint = pipe.Type switch
            {
                PipeType.Moving => Color.DodgerBlue,
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
                    _ => Color.ForestGreen,
                };
                _spriteBatch.Draw(_pixelTexture, new Rectangle(pipe.X, 0, pipe.Width, pipe.TopHeight), fallback);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(pipe.X, bottomStart, pipe.Width, viewH - bottomStart), fallback);
            }
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
            string spaceLine = "ПРОБЕЛ или клик - вернуться в меню";

            var scoreSize = _font.MeasureString(scoreLine);
            var spaceSize = _font.MeasureString(spaceLine);

            int panelW = (int)Math.Max(scoreSize.X, spaceSize.X) + 80;
            int panelH = (int)(scoreSize.Y + spaceSize.Y) + 50;
            int panelX = (w - panelW) / 2;
            int panelY = h - panelH - 40;

            int border = 4;
            _spriteBatch.Draw(_pixelTexture,
                new Rectangle(panelX - border, panelY - border, panelW + border * 2, panelH + border * 2),
                Color.CornflowerBlue);
            _spriteBatch.Draw(_pixelTexture,
                new Rectangle(panelX, panelY, panelW, panelH),
                Color.MidnightBlue);

            float scoreX = panelX + (panelW - scoreSize.X) / 2;
            float spaceX = panelX + (panelW - spaceSize.X) / 2;
            float scoreY = panelY + 18;
            float spaceY = scoreY + scoreSize.Y + 10;

            _spriteBatch.DrawString(_font, scoreLine, new Vector2(scoreX, scoreY), Color.White);
            _spriteBatch.DrawString(_font, spaceLine, new Vector2(spaceX, spaceY), Color.CornflowerBlue);
        }

        private void DrawLeaderboardPanel(int screenW, int screenH)
        {
            var scores = _model.TopScores;

            if (_leaderboardTexture == null)
            {
                // Запасной вариант: нарисовать текстовую панель
                if (_font == null || (scores.Count == 0 && _model.HighScore == 0)) return;
                var fallback = scores.Count > 0 ? scores : new List<int> { _model.HighScore };
                string header = "ТОП 5";
                var headerSize = _font.MeasureString(header);
                var lines = fallback.Select((s, i) => $"{i + 1}.  {s}").ToList();
                float lhf = _font.MeasureString("0").Y;
                float maxLW = lines.Max(l => _font.MeasureString(l).X);
                int pW = (int)Math.Max(maxLW, headerSize.X) + 40;
                int pH = (int)(lhf * (lines.Count + 1.5f)) + 24;
                int pX = screenW - pW - 30, pY = (screenH - pH) / 2;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(pX - 4, pY - 4, pW + 8, pH + 8), Color.CornflowerBlue);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(pX, pY, pW, pH), Color.MidnightBlue * 0.92f);
                _spriteBatch.DrawString(_font, header, new Vector2(pX + (pW - headerSize.X) / 2, pY + 10), Color.CornflowerBlue);
                for (int i = 0; i < lines.Count; i++)
                {
                    var ls = _font.MeasureString(lines[i]);
                    _spriteBatch.DrawString(_font, lines[i], new Vector2(pX + (pW - ls.X) / 2, pY + 10 + lhf * (i + 1.3f)), Color.White);
                }
                return;
            }

            // Рисуем картинку TOP 5 справа (меньше + больший отступ, чтобы не обрезалась)
            int panelH = Math.Min(460, screenH - 100);
            int panelW = (int)((float)_leaderboardTexture.Width / _leaderboardTexture.Height * panelH);
            int panelX = screenW - panelW - 120;
            int panelY = (screenH - panelH) / 2;

            _spriteBatch.Draw(_leaderboardTexture, new Rectangle(panelX, panelY, panelW, panelH), Color.White);

            if (_font == null) return;

            // Строка 1 подтверждена по скриншоту = 0.305.
            // Шаг между строками = 0.122 (не 0.137 как раньше — было слишком много).
            float rowStart = 0.305f;
            float rowStep  = 0.122f;
            float[] rowYFrac = {
                rowStart,
                rowStart + rowStep,
                rowStart + rowStep * 2,
                rowStart + rowStep * 3,
                rowStart + rowStep * 4,
            };

            // X: перекрываем правую половину строки (где "000")
            float coverStartXFrac = 0.40f;
            float coverWFrac      = 0.52f;
            float rowHFrac        = 0.155f;  // повыше, чтобы перекрыть буббли-шрифт

            // Скрываем 6-ю лишнюю строку целиком (включая "5." и "000")
            int r6CenterY = panelY + (int)((rowStart + rowStep * 5) * panelH);
            int r6H       = (int)(rowHFrac * panelH);
            _spriteBatch.Draw(_pixelTexture,
                new Rectangle(panelX + (int)(0.09f * panelW), r6CenterY - r6H / 2,
                              (int)(0.84f * panelW), r6H),
                new Color(215, 232, 250));

            for (int i = 0; i < 5; i++)
            {
                string txt = i < scores.Count ? scores[i].ToString() : "-";

                int rowCenterY = panelY + (int)(rowYFrac[i] * panelH);
                int coverX     = panelX + (int)(coverStartXFrac * panelW);
                int coverW2    = (int)(coverWFrac * panelW);
                int coverH     = (int)(rowHFrac * panelH);

                // Перекрываем "000" цветом соответствующей строки
                Color rowBg = i % 2 == 0
                    ? new Color(248, 252, 255)
                    : new Color(215, 232, 250);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(coverX, rowCenterY - coverH / 2, coverW2, coverH), rowBg);

                // Рисуем реальное число
                var sz = _font.MeasureString(txt);
                float tx = coverX + (coverW2 - sz.X) / 2f;
                float ty = rowCenterY - sz.Y / 2f;
                _spriteBatch.DrawString(_font, txt, new Vector2(tx, ty), new Color(18, 36, 88));
            }
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

        private Texture2D CreateHeartTexture(int displaySize, Color mainColor)
        {
            // Пиксельная карта сердца 10x9 (0=прозрачный, 1=чёрная обводка, 2=основной, 3=светлый блик)
            byte[,] pat = {
                {0,0,1,1,0,0,1,1,0,0},
                {0,1,2,2,1,1,2,2,1,0},
                {1,2,3,2,2,2,2,2,2,1},
                {1,2,3,2,2,2,2,2,2,1},
                {1,2,2,2,2,2,2,2,2,1},
                {0,1,2,2,2,2,2,2,1,0},
                {0,0,1,2,2,2,2,1,0,0},
                {0,0,0,1,2,2,1,0,0,0},
                {0,0,0,0,1,1,0,0,0,0},
            };
            int logW = 10, logH = 9;
            int scale = Math.Max(1, displaySize / logW);
            int texW = logW * scale;
            int texH = logH * scale;

            var border = Color.Black;
            var highlight = Color.White;

            var tex = new Texture2D(_graphicsDevice, texW, texH);
            var data = new Color[texW * texH];

            for (int ly = 0; ly < logH; ly++)
                for (int lx = 0; lx < logW; lx++)
                {
                    Color c = pat[ly, lx] switch
                    {
                        1 => border,
                        2 => mainColor,
                        3 => highlight,
                        _ => Color.Transparent,
                    };
                    for (int sy = 0; sy < scale; sy++)
                        for (int sx = 0; sx < scale; sx++)
                            data[(ly * scale + sy) * texW + (lx * scale + sx)] = c;
                }

            tex.SetData(data);
            return tex;
        }

        private Texture2D CreateCircleTexture(int radius, Color color)
        {
            int diameter = radius * 2;
            var tex = new Texture2D(_graphicsDevice, diameter, diameter);
            var data = new Color[diameter * diameter];
            var center = new Vector2(radius - 0.5f, radius - 0.5f);
            for (int py = 0; py < diameter; py++)
                for (int px = 0; px < diameter; px++)
                {
                    float dist = Vector2.Distance(new Vector2(px, py), center);
                    data[py * diameter + px] = dist <= radius - 0.5f ? color : Color.Transparent;
                }
            tex.SetData(data);
            return tex;
        }
    }
}
