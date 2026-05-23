using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        private Texture2D? _nyanTexture;
        private Texture2D  _bossTex     = null!;
        private Texture2D  _bossTex2    = null!;
        private Texture2D  _bossTex3    = null!;
        private Texture2D  _bossTex4    = null!;
        private Texture2D  _bossTex5    = null!;
        private Texture2D  _bossTexYa   = null!;
        private Texture2D  _bulletTex   = null!;
        private int        _bossIndex   = 0;   // чередование боссов
        private Texture2D  _magnetTex   = null!;
        private float      _shakeTimer  = 0f;
        private float      _shakeAmount = 0f;
        private float      _scorePulse  = 0f;   // анимация счёта
        private int        _lastScore   = 0;
        private List<Texture2D> _gameOverFrames = new();

        // --- Частицы ---
        private struct Particle
        {
            public float X, Y, VX, VY, Life, MaxLife, Size;
            public Color Color;
        }
        private readonly List<Particle> _particles = new();
        private float _shieldHitCooldown = 0f;
        private Song? _music;
        private Song? _gameplayMusic;
        private GameState _prevMusicState = GameState.Playing;

        // Выбор музыки
        private readonly (string Key, string Label)[] _musicTracks =
        {
            ("gameplay",   "а может просто негром стать..."),
            ("pulsewidth", "Aphex Twin - Pulsewidth"),
            ("freebird",   "Lynyrd Skynyrd - Free Bird"),
            ("xtal",       "Aphex Twin - Xtal"),
        };
        private Song?[] _allSongs = Array.Empty<Song?>();

        private const float Gravity = 300f;
        private const float JumpPower = -200f;
        private const double FrameDuration = 0.08;
        private const int PipeGap = 185;
        private const int PlayerMargin    = 5;
        private const int PlayerTopMargin = 22; // обрезка хитбокса сверху
        private const int PipeMargin = 5;

        private const float PipeSpeed = 200f;
        private const double PipeInterval = 1.8;
        private const int MovingPipeGap = 220;
        private const float MaxMovingPipeSpeed = 120f;

        private const float InvincibilityDuration = 1.5f;
        private const float ShieldDuration = 4f;
        private const float SlowDuration = 4f;
        private const float SlowFactor = 0.5f;
        private const int   CoinSpawnChance = 65;

        private const float BossLifetime      = 20f;
        private const float BossShootInterval = 1.5f;
        private const float BossSpeed         = 130f;
        private const float BulletSpeed       = 500f;

        private const float MagnetDuration    = 6f;
        private const float MagnetRadius      = 220f;
        private const float ShakeMaxTime      = 0.35f;
        private const float ShakeIntensityHit = 10f;

        private const float DashDuration      = 0.35f;
        private const float DashCooldownTime  = 2.0f;
        private const int   DashDistance      = 200;

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
            _shieldTexture = TryLoadTexture("shield.png") ?? CreateCircleTexture(14, Color.HotPink);
            _slowTexture = TryLoadTexture("slow.png") ?? CreateCircleTexture(14, Color.LimeGreen);

            _logoTexture  = TryLoadTexture("logo.png");
            _nyanTexture  = TryLoadTexture("nyan.png");
            _bossTex      = TryLoadTexture("boss.png")  ?? CreateCircleTexture(45, new Color(200, 30, 30));
            _bossTex2     = TryLoadTexture("boss2.png") ?? _bossTex;
            _bossTex3     = TryLoadTexture("boss3.png") ?? _bossTex;
            _bossTex4     = TryLoadTexture("boss4.png") ?? _bossTex;
            _bossTex5     = TryLoadTexture("boss5.png") ?? _bossTex;
            _bossTexYa    = TryLoadTexture("boss_ya.png") ?? _bossTex;
            _bulletTex    = CreateCircleTexture(7, new Color(255, 210, 0));
            _magnetTex    = TryLoadTexture("magnet.png") ?? CreateCircleTexture(14, new Color(255, 80, 180));
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

            _allSongs = new Song?[_musicTracks.Length];
            for (int i = 0; i < _musicTracks.Length; i++)
            {
                try { _allSongs[i] = content.Load<Song>(_musicTracks[i].Key); } catch { }
            }

            _scoresPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scores.txt");
            LoadScores();
        }

        // --- Music ---

        public void UpdateMusic()
        {
            // MusicSelect считается как Menu для музыки
            var effectiveState    = _model.State    == GameState.MusicSelect ? GameState.Menu : _model.State;
            var effectivePrevState = _prevMusicState == GameState.MusicSelect ? GameState.Menu : _prevMusicState;

            bool isGameOver  = effectiveState     == GameState.GameOver;
            bool wasGameOver = effectivePrevState  == GameState.GameOver;
            bool isPlaying   = effectiveState     == GameState.Playing;
            bool wasPlaying  = effectivePrevState  == GameState.Playing;

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
                int idx = Math.Clamp(_model.SelectedMusicTrack, 0, _allSongs.Length - 1);
                var track = _allSongs[idx] ?? _gameplayMusic;
                if (track != null)
                {
                    MediaPlayer.IsRepeating = true;
                    MediaPlayer.Play(track);
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
            _model.ShieldTimer  = 0f;
            _model.SlowTimer    = 0f;
            _model.MagnetTimer   = 0f;
            _model.DashTimer     = 0f;
            _model.DashCooldown  = 0f;
            _model.DashRequested = false;
            _shieldHitCooldown   = 0f;
            _shakeTimer          = 0f;
            _particles.Clear();
            _model.Boss = null;
            _model.Bullets.Clear();
            _model.NextBossScore = 60;
            _bossIndex = 0;
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
            if (_model.ShieldTimer > 0)        _model.ShieldTimer        -= dt;
            if (_model.SlowTimer > 0)          _model.SlowTimer          -= dt;
            if (_model.MagnetTimer > 0)        _model.MagnetTimer        -= dt;
            if (_model.DashCooldown > 0)       _model.DashCooldown       -= dt;
            if (_shieldHitCooldown > 0)        _shieldHitCooldown        -= dt;
            if (_scorePulse > 0)               _scorePulse               -= dt;

            // Запускаем пульс при изменении счёта
            if (_model.Score != _lastScore) { _scorePulse = 0.2f; _lastScore = _model.Score; }
            if (_shakeTimer > 0)               _shakeTimer               -= dt;

            // Рывок — старт
            if (_model.DashRequested && _model.DashCooldown <= 0)
            {
                _model.DashRequested = false;
                _model.DashTimer     = DashDuration;
                _model.DashCooldown  = DashCooldownTime;
                SpawnParticles(player.X + player.Width / 2, player.Y + player.Height / 2,
                               Color.Cyan, 10);
            }
            _model.DashRequested = false; // сбросить если кулдаун ещё идёт

            // Рывок — движение по синусоиде вперёд и назад
            if (_model.DashTimer > 0)
            {
                _model.DashTimer -= dt;
                float progress = 1f - (_model.DashTimer / DashDuration);
                float curve    = MathF.Sin(progress * MathF.PI); // 0→1→0
                player.X = player.StartX + (int)(curve * DashDistance);
                if (_model.DashTimer <= 0)
                    player.X = player.StartX;
                // Шлейф частиц
                if (Random.Shared.Next(3) == 0)
                    SpawnParticles(player.X, player.Y + player.Height / 2,
                                   Color.Cyan * 0.7f, 3);
            }

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
                player.X + PlayerMargin, player.Y + PlayerTopMargin,
                player.Width - PlayerMargin * 2, player.Height - PlayerTopMargin - PlayerMargin);

            foreach (var coin in _model.Coins)
            {
                if (coin.Collected) continue;
                if (playerRect.Intersects(new Rectangle(coin.X, coin.Y, coin.Width, coin.Height)))
                {
                    coin.Collected = true;
                    Color particleColor = coin.Type switch
                    {
                        PowerUpType.Shield => Color.HotPink,
                        PowerUpType.Slow   => Color.DeepSkyBlue,
                        PowerUpType.Heart  => Color.HotPink,
                        PowerUpType.Magnet => Color.HotPink,
                        _                  => Color.LimeGreen,     // монетка — зелёные искры
                    };
                    SpawnParticles(coin.X + coin.Width / 2, coin.Y + coin.Height / 2, particleColor);
                    switch (coin.Type)
                    {
                        case PowerUpType.Coin:   _model.Score += 3; break;
                        case PowerUpType.Shield: _model.ShieldTimer = ShieldDuration; break;
                        case PowerUpType.Slow:   _model.SlowTimer   = SlowDuration; break;
                        case PowerUpType.Heart:  _model.Lives++; break;
                        case PowerUpType.Magnet: _model.MagnetTimer = MagnetDuration; break;
                    }
                }
            }

            UpdateParticles(dt);

            // Магнит: притягиваем монеты
            if (_model.MagnetTimer > 0)
            {
                float px = player.X + player.Width  / 2f;
                float py = player.Y + player.Height / 2f;
                foreach (var coin in _model.Coins)
                {
                    if (coin.Collected) continue;
                    float cx = coin.X + coin.Width  / 2f;
                    float cy = coin.Y + coin.Height / 2f;
                    float dx = px - cx, dy = py - cy;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    if (dist < MagnetRadius && dist > 1f)
                    {
                        float spd = 350f * (1f - dist / MagnetRadius) + 80f;
                        coin.X += (int)(dx / dist * spd * dt);
                        coin.Y += (int)(dy / dist * spd * dt);
                    }
                }
            }

            // Босс: спавн и обновление
            if ((_model.Boss == null || !_model.Boss.Active) && _model.Score >= _model.NextBossScore)
            {
                SpawnBoss(viewH);
                _model.NextBossScore += 60;
            }
            bool isInvincible = _model.GodMode || _model.InvincibilityTimer > 0
                             || _model.ShieldTimer > 0 || _model.DashTimer > 0;
            if (_model.Boss?.Active == true)
                UpdateBoss(dt, viewH, isInvincible);
            UpdateBullets(dt, viewW, isInvincible);

            // Столкновение с трубами
            foreach (var pipe in _model.Pipes)
            {
                if (CheckCollision(player, pipe, viewH))
                {
                    if (_model.ShieldTimer > 0)
                    {
                        // Щит поглощает удар — розовые частицы
                        if (_shieldHitCooldown <= 0f)
                        {
                            SpawnParticles(
                                player.X + player.Width / 2,
                                player.Y + player.Height / 2,
                                Color.HotPink, 22);
                            _shieldHitCooldown = 0.4f;
                        }
                    }
                    else if (!isInvincible)
                    {
                        _model.Lives--;
                        if (_model.Lives <= 0)
                        {
                            if (_model.Score > _model.HighScore) _model.HighScore = _model.Score;
                            SaveScore(_model.Score);
                            _model.DeathCount++;
                            _model.GhostX = _model.Player.X;
                            _model.GhostY = _model.Player.Y;
                            _model.State = GameState.GameOver;
                            return;
                        }
                        _model.InvincibilityTimer = InvincibilityDuration;
                    TriggerShake();
                        break;
                    }
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
                    _model.DeathCount++;
                    _model.State = GameState.GameOver;
                }
                else
                {
                    player.Y = player.StartY;
                    player.VelocityY = 0;
                    _model.InvincibilityTimer = InvincibilityDuration;
                    TriggerShake();
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
                GameState.GameOver    => Color.White,
                GameState.Playing     => GetSkyColor(),
                GameState.MusicSelect => new Color(15, 15, 35),
                _                     => Color.CornflowerBlue,
            };
            _graphicsDevice.Clear(bgColor);

            Matrix shakeMatrix = Matrix.Identity;
            if (_shakeTimer > 0)
            {
                float intensity = ShakeIntensityHit * (_shakeTimer / ShakeMaxTime);
                float ox = (float)(Random.Shared.NextDouble() * 2 - 1) * intensity;
                float oy = (float)(Random.Shared.NextDouble() * 2 - 1) * intensity;
                shakeMatrix = Matrix.CreateTranslation(ox, oy, 0);
            }
            _spriteBatch.Begin(transformMatrix: shakeMatrix);
            switch (_model.State)
            {
                case GameState.Menu:        DrawMenu();        break;
                case GameState.Playing:     DrawGame();        break;
                case GameState.GameOver:    DrawGameOver();    break;
                case GameState.MusicSelect: DrawMusicSelect(); break;
            }
            if (_model.GodMode)
                DrawGodModeIndicator();
            DrawNyanCursor();
            _spriteBatch.End();
        }

        private void DrawNyanCursor()
        {
            if (_nyanTexture == null) return;
            var mouse = Mouse.GetState();
            int size = 48;
            _spriteBatch.Draw(_nyanTexture,
                new Rectangle(mouse.X, mouse.Y, size, size),
                Color.White);
        }

        // --- Sky color by score (день → закат → ночь) ---

        private Color GetSkyColor()
        {
            var day    = new Color(100, 149, 237);  // cornflower blue
            var sunset = new Color(255, 120, 50);   // оранжевый закат
            var night  = new Color(8,   8,  45);    // тёмно-синяя ночь

            if (_model.Score < 60)
            {
                float t = _model.Score / 60f;
                return LerpColor(day, sunset, t);
            }
            else if (_model.Score < 120)
            {
                float t = (_model.Score - 60) / 60f;
                return LerpColor(sunset, night, t);
            }
            return night;
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Color(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private void TriggerShake()
        {
            _shakeTimer  = ShakeMaxTime;
            _shakeAmount = ShakeIntensityHit;
        }

        // --- Private helpers ---

        private void MaybeSpawnCoin(PipeModel pipe, int viewH)
        {
            int gapMid = pipe.TopHeight + pipe.Gap / 2 - 22;
            int coinX  = pipe.X + pipe.Width / 2 - 22;

            // Сердце — на сложных трубах
            bool isHard = pipe.Type == PipeType.Moving
                       || Math.Abs(_lastGapCenter - viewH / 2) > 220;
            if (isHard && Random.Shared.Next(100) < 18)
            {
                _model.Coins.Add(new CoinModel(coinX, gapMid, PowerUpType.Heart));
                return;
            }

            // Магнит + Щит перед скоплением труб (3+ труб на экране)
            bool magnetPending = _model.Coins.Any(c => !c.Collected && c.Type == PowerUpType.Magnet);
            bool shieldPending = _model.Coins.Any(c => !c.Collected && c.Type == PowerUpType.Shield);
            if (_model.Pipes.Count >= 3 && !magnetPending && _model.MagnetTimer <= 0)
            {
                // Щит чуть левее (игрок встретит его первым, потом магнит, потом трубы)
                if (!shieldPending && _model.ShieldTimer <= 0)
                    _model.Coins.Add(new CoinModel(coinX - 180, gapMid, PowerUpType.Shield));
                _model.Coins.Add(new CoinModel(coinX, gapMid, PowerUpType.Magnet));
                return;
            }

            if (Random.Shared.Next(100) >= CoinSpawnChance) return;
            int roll = Random.Shared.Next(100);
            PowerUpType type = roll < 70 ? PowerUpType.Coin
                             : roll < 84 ? PowerUpType.Shield
                             :             PowerUpType.Slow;
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
            var playerRect = new Rectangle(player.X + PlayerMargin, player.Y + PlayerTopMargin,
                player.Width - PlayerMargin * 2, player.Height - PlayerTopMargin - PlayerMargin);
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

            if (_model.HighScore > 0)
                DrawTextCentered($"рекорд: {_model.HighScore}", h / 2 + 140, Color.Gold, 1);

            if (_model.DeathCount > 0)
                DrawTextCentered($"смертей: {_model.DeathCount}", h / 2 + 165, new Color(200, 80, 80), 1);

            // --- Кнопка выбора музыки ---
            bool unlocked = _model.DeathCount >= 3;
            var btnRect = GetMusicButtonRect();
            var tiffany = new Color(10, 186, 181);
            var btnColor    = unlocked ? tiffany                  : new Color(40, 40, 40);
            var borderColor = unlocked ? new Color(180, 255, 252) : new Color(80, 80, 80);
            DrawRect(btnRect, btnColor);
            DrawRectBorder(btnRect, borderColor, 3);
            if (_font != null && unlocked)
            {
                const int scale = 1;
                string label = "САУНДТРЕК";
                var size = _font.MeasureString(label) * scale;
                _spriteBatch.DrawString(_font, label,
                    new Vector2(btnRect.X + (btnRect.Width  - size.X) / 2,
                                btnRect.Y + (btnRect.Height - size.Y) / 2),
                    Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }

        private Rectangle GetMusicButtonRect()
        {
            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;
            int btnW = 280; int btnH = 60;
            return new Rectangle(w / 2 - btnW / 2, h / 2 + 210, btnW, btnH);
        }

        public void HandleMenuClick(int mx, int my)
        {
            if (_model.DeathCount >= 3 && GetMusicButtonRect().Contains(mx, my))
            {
                _model.State = GameState.MusicSelect;
                return;
            }
            StartNewGame();
        }

        private void DrawMusicSelect()
        {
            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;

            DrawTextCentered("ВЫБОР МУЗЫКИ", h / 4, Color.White, 2);

            int btnW = 400; int btnH = 70; int gap = 20;
            int totalH = _musicTracks.Length * (btnH + gap) - gap;
            int startY = h / 2 - totalH / 2;

            for (int i = 0; i < _musicTracks.Length; i++)
            {
                var rect = new Rectangle(w / 2 - btnW / 2, startY + i * (btnH + gap), btnW, btnH);
                bool selected = _model.SelectedMusicTrack == i;
                var bg     = selected ? new Color(60, 120, 60)  : new Color(50, 50, 80);
                var border = selected ? new Color(100, 255, 100) : new Color(100, 100, 180);
                DrawRect(rect, bg);
                DrawRectBorder(rect, border, 3);
                if (_font != null)
                {
                    string label = (selected ? "> " : "  ") + _musicTracks[i].Label;
                    var sz = _font.MeasureString(label);
                    _spriteBatch.DrawString(_font, label,
                        new Vector2(rect.X + (rect.Width - sz.X) / 2, rect.Y + (rect.Height - sz.Y) / 2),
                        Color.White);
                }
            }

            // Кнопка назад
            var backRect = new Rectangle(w / 2 - 150, startY + _musicTracks.Length * (btnH + gap) + 30, 300, 55);
            DrawRect(backRect, new Color(80, 40, 40));
            DrawRectBorder(backRect, new Color(200, 80, 80), 3);
            if (_font != null)
            {
                string back = "НАЗАД";
                var sz = _font.MeasureString(back);
                _spriteBatch.DrawString(_font, back,
                    new Vector2(backRect.X + (backRect.Width - sz.X) / 2, backRect.Y + (backRect.Height - sz.Y) / 2),
                    Color.White);
            }
        }

        public void HandleMusicSelectClick(int mx, int my)
        {
            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;
            int btnW = 400; int btnH = 70; int gap = 20;
            int totalH = _musicTracks.Length * (btnH + gap) - gap;
            int startY = h / 2 - totalH / 2;

            for (int i = 0; i < _musicTracks.Length; i++)
            {
                var rect = new Rectangle(w / 2 - btnW / 2, startY + i * (btnH + gap), btnW, btnH);
                if (rect.Contains(mx, my))
                {
                    _model.SelectedMusicTrack = i;
                    _model.State = GameState.Menu;
                    return;
                }
            }

            var backRect = new Rectangle(w / 2 - 150, startY + _musicTracks.Length * (btnH + gap) + 30, 300, 55);
            if (backRect.Contains(mx, my))
                _model.State = GameState.Menu;
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
                    PowerUpType.Slow   => _slowTexture,
                    PowerUpType.Heart  => _heartFullTexture,
                    PowerUpType.Magnet => _magnetTex,
                    _                  => _coinTexture,
                };
                // Круговая синяя обводка для замедления
                if (coin.Type == PowerUpType.Slow)
                {
                    int ring = 4;
                    _spriteBatch.Draw(_slowTexture,
                        new Rectangle(coin.X - ring, coin.Y - ring, coin.Width + ring * 2, coin.Height + ring * 2),
                        Color.DeepSkyBlue);
                }
                _spriteBatch.Draw(tex, new Rectangle(coin.X, coin.Y, coin.Width, coin.Height), Color.White);

            }

            // Призрак — силуэт места прошлой смерти
            if (_model.GhostY >= 0 && _playerTexture != null)
            {
                float gsx = (float)_model.Player.Width  / _playerTexture.Width;
                float gsy = (float)_model.Player.Height / _playerTexture.Height;
                _spriteBatch.Draw(_playerTexture,
                    new Vector2(_model.GhostX, _model.GhostY),
                    null, Color.White * 0.28f, 0, Vector2.Zero,
                    new Vector2(gsx, gsy), SpriteEffects.None, 0);
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

            // Щит — розовая аура вокруг игрока
            if (_model.ShieldTimer > 0)
            {
                int aura = 8;
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X - aura, player.Y - aura, player.Width + aura * 2, aura),
                    Color.HotPink * 0.7f);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X - aura, player.Y + player.Height, player.Width + aura * 2, aura),
                    Color.HotPink * 0.7f);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X - aura, player.Y - aura, aura, player.Height + aura * 2),
                    Color.HotPink * 0.7f);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X + player.Width, player.Y - aura, aura, player.Height + aura * 2),
                    Color.HotPink * 0.7f);
            }

            DrawParticles();
            DrawBullets();
            DrawBoss(viewH);

            // Магнит — пульсирующая розовая аура
            if (_model.MagnetTimer > 0)
            {
                float pulse = 0.5f + 0.3f * MathF.Sin((float)(_model.MagnetTimer * 8));
                int ring = 12 + (int)(pulse * 6);
                Color mc = new Color(255, 80, 180) * (0.5f + pulse * 0.3f);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X - ring, player.Y - ring, player.Width + ring * 2, ring), mc);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X - ring, player.Y + player.Height, player.Width + ring * 2, ring), mc);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X - ring, player.Y - ring, ring, player.Height + ring * 2), mc);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(player.X + player.Width, player.Y - ring, ring, player.Height + ring * 2), mc);
            }

            // Анимация счёта — пульсирует при получении очка
            if (_font != null)
            {
                string scoreText = $"Счет: {_model.Score}";
                float pulse = _scorePulse > 0 ? 1f + 0.3f * (_scorePulse / 0.2f) : 1f;
                var sz = _font.MeasureString(scoreText) * pulse;
                float sx = (_graphicsDevice.Viewport.Width - sz.X) / 2f;
                float sy = 20f - (sz.Y - _font.MeasureString(scoreText).Y) / 2f;
                _spriteBatch.DrawString(_font, scoreText, new Vector2(sx, sy),
                    Color.White, 0, Vector2.Zero, pulse, SpriteEffects.None, 0);
            }

            // Сердечки (жизни) — правый верхний угол
            DrawHearts();

            // Активные бонусы
            DrawPowerUpTimers();
        }

        private void DrawHearts()
        {
            int heartSize = 40;
            int gap = 4;
            // минимум 3 слота, если жизней больше — показываем все
            int total = Math.Max(3, _model.Lives);
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
                    new Color(100, 0, 60) * 0.85f);
                _spriteBatch.DrawString(_font, txt, new Vector2(px, py), Color.HotPink);
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

            if (_model.MagnetTimer > 0)
            {
                string txt = $"МАГНИТ {_model.MagnetTimer:F1}с";
                var sz = _font.MeasureString(txt);
                int py = viewH - lineH * (row + 1);
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(px - 4, py - 2, (int)sz.X + 8, (int)sz.Y + 4),
                    new Color(120, 0, 60) * 0.85f);
                _spriteBatch.DrawString(_font, txt, new Vector2(px, py), new Color(255, 80, 180));
                row++;
            }

            // Рывок: иконка внизу по центру
            {
                bool dashing  = _model.DashTimer > 0;
                bool ready    = _model.DashCooldown <= 0;
                float cdFrac  = ready ? 1f : 1f - (_model.DashCooldown / DashCooldownTime);
                string label  = dashing ? "РЫВОК!" : ready ? "РЫВОК [2 клика]" : $"рывок {_model.DashCooldown:F1}с";
                Color  col    = dashing ? Color.Cyan : ready ? Color.Cyan * 0.9f : Color.Gray * 0.7f;
                var sz2       = _font.MeasureString(label);
                int vw        = _graphicsDevice.Viewport.Width;
                int ix        = vw / 2 - (int)sz2.X / 2;
                int iy        = viewH - lineH - 6;
                // Кулдаун-бар под текстом
                int barW2 = 120; int barH2 = 4;
                int barX2 = vw / 2 - barW2 / 2;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(barX2, iy + lineH, barW2, barH2), Color.DarkSlateGray);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(barX2, iy + lineH, (int)(barW2 * cdFrac), barH2),
                    dashing ? Color.Cyan : Color.SteelBlue);
                _spriteBatch.DrawString(_font, label, new Vector2(ix, iy), col);
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

        // --- Босс ---

        private void SpawnBoss(int viewH)
        {
            int viewW = _graphicsDevice.Viewport.Width;
            _bossIndex++;
            _model.Boss = new BossModel
            {
                X             = viewW * 0.62f,
                Y             = viewH / 2f - 45,
                Active        = true,
                LifeTimer     = BossLifetime,
                ShootTimer    = 1.0f,
                AnnounceTimer = 2.5f,
            };
            _model.Bullets.Clear();
        }

        private Texture2D CurrentBossTex
        {
            get
            {
                int idx = Math.Min(_bossIndex, 6); // после 360 не циклится
                return idx switch
                {
                    1 => _bossTex4,  // 60  — dog7
                    2 => _bossTex2,  // 120 — котик с языком
                    3 => _bossTex3,  // 180 — свинья
                    4 => _bossTex5,  // 240 — фото
                    5 => _bossTexYa, // 300 — я
                    _ => _bossTex,   // 360+ — котик в шляпе навсегда
                };
            }
        }

        private void UpdateBoss(float dt, int viewH, bool isInvincible)
        {
            var boss   = _model.Boss!;
            var player = _model.Player;

            boss.AnnounceTimer -= dt;
            float slowMult = _model.SlowTimer > 0 ? SlowFactor : 1f;
            boss.LifeTimer -= dt * slowMult;

            if (boss.LifeTimer <= 0)
            {
                boss.Active = false;
                _model.Bullets.Clear();
                SpawnParticles((int)(boss.X + boss.Width / 2), (int)(boss.Y + boss.Height / 2),
                               Color.OrangeRed, 30);
                return;
            }

            // Следим за игроком по Y
            float targetY = player.Y + player.Height / 2f - boss.Height / 2f;
            float spd = BossSpeed * slowMult;
            if (boss.Y < targetY) boss.Y = Math.Min(boss.Y + spd * dt, targetY);
            else                  boss.Y = Math.Max(boss.Y - spd * dt, targetY);
            boss.Y = Math.Clamp(boss.Y, 0, viewH - boss.Height);

            // Стрельба
            boss.ShootTimer -= dt * slowMult;
            if (boss.ShootTimer <= 0)
            {
                boss.ShootTimer = BossShootInterval;
                ShootAtPlayer(boss);
            }

            // Коллизия с игроком
            var playerRect = new Rectangle(player.X + PlayerMargin, player.Y + PlayerTopMargin, player.Width - PlayerMargin * 2, player.Height - PlayerTopMargin - PlayerMargin);
            var bossRect   = new Rectangle((int)boss.X, (int)boss.Y, boss.Width, boss.Height);
            if (playerRect.Intersects(bossRect))
            {
                if (_model.ShieldTimer > 0 && _shieldHitCooldown <= 0)
                {
                    SpawnParticles(player.X + player.Width / 2, player.Y + player.Height / 2, Color.HotPink, 22);
                    _shieldHitCooldown = 0.4f;
                }
                else if (!isInvincible)
                {
                    _model.Lives--;
                    if (_model.Lives <= 0)
                    {
                        if (_model.Score > _model.HighScore) _model.HighScore = _model.Score;
                        SaveScore(_model.Score);
                        _model.DeathCount++;
                        _model.State = GameState.GameOver;
                        return;
                    }
                    _model.InvincibilityTimer = InvincibilityDuration;
                    TriggerShake();
                }
            }
        }

        private void ShootAtPlayer(BossModel boss)
        {
            float bx = boss.X + boss.Width  / 2f;
            float by = boss.Y + boss.Height / 2f;
            float px = _model.Player.X + _model.Player.Width  / 2f;
            float py = _model.Player.Y + _model.Player.Height / 2f;
            float dx = px - bx; float dy = py - by;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 1) return;
            _model.Bullets.Add(new BulletModel
            {
                X = bx, Y = by,
                VX = dx / len * BulletSpeed,
                VY = dy / len * BulletSpeed,
            });
        }

        private void UpdateBullets(float dt, int viewW, bool isInvincible)
        {
            var player = _model.Player;
            float slowMult = _model.SlowTimer > 0 ? SlowFactor : 1f;

            for (int i = _model.Bullets.Count - 1; i >= 0; i--)
            {
                var b = _model.Bullets[i];
                b.X += b.VX * dt * slowMult;
                b.Y += b.VY * dt * slowMult;
                _model.Bullets[i] = b;

                if (b.X < -60 || b.X > viewW + 60 || b.Y < -60 || b.Y > _graphicsDevice.Viewport.Height + 60)
                { _model.Bullets.RemoveAt(i); continue; }

                var bRect = new Rectangle((int)(b.X - b.Size / 2), (int)(b.Y - b.Size / 2), b.Size, b.Size);
                var pRect = new Rectangle(player.X + PlayerMargin, player.Y + PlayerTopMargin, player.Width - PlayerMargin * 2, player.Height - PlayerTopMargin - PlayerMargin);
                if (!bRect.Intersects(pRect)) continue;

                if (_model.ShieldTimer > 0 && _shieldHitCooldown <= 0)
                {
                    _model.Bullets.RemoveAt(i);
                    SpawnParticles(player.X + player.Width / 2, player.Y + player.Height / 2, Color.HotPink, 18);
                    _shieldHitCooldown = 0.4f;
                }
                else if (!isInvincible)
                {
                    _model.Bullets.RemoveAt(i);
                    _model.Lives--;
                    if (_model.Lives <= 0)
                    {
                        if (_model.Score > _model.HighScore) _model.HighScore = _model.Score;
                        SaveScore(_model.Score);
                        _model.DeathCount++;
                        _model.State = GameState.GameOver;
                        return;
                    }
                    _model.InvincibilityTimer = InvincibilityDuration;
                    TriggerShake();
                }
            }
        }

        private void DrawBoss(int viewH)
        {
            var boss = _model.Boss;
            if (boss == null || !boss.Active) return;

            int viewW = _graphicsDevice.Viewport.Width;

            // Пульсирующая красная рамка пока босс активен
            float pulse = 0.55f + 0.3f * MathF.Sin((float)(boss.LifeTimer * 6));
            int b2 = 6;
            Color rimCol = Color.Crimson * pulse;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, viewW, b2), rimCol);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, viewH - b2, viewW, b2), rimCol);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, b2, viewH), rimCol);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(viewW - b2, 0, b2, viewH), rimCol);

            // Тело босса
            bool blink = boss.LifeTimer < 3f && (int)(boss.LifeTimer * 8) % 2 == 0;
            if (!blink)
                _spriteBatch.Draw(CurrentBossTex,
                    new Rectangle((int)boss.X, (int)boss.Y, boss.Width, boss.Height),
                    Color.White);

            // Таймер-бар сверху по центру
            int barW = 220; int barH = 12;
            int barX = viewW / 2 - barW / 2;
            float frac = Math.Clamp(boss.LifeTimer / BossLifetime, 0, 1);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(barX - 2, 6, barW + 4, barH + 4), Color.DarkRed);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, 8, (int)(barW * frac), barH), Color.OrangeRed);

            // Анонс "BOSS!"
            if (boss.AnnounceTimer > 0)
            {
                float alpha = Math.Clamp(boss.AnnounceTimer / 2.5f, 0, 1);
                DrawTextCentered("! BOSS !", viewH / 2 - 50, Color.Red * alpha, 3);
                DrawTextCentered("УКЛОНЯЙСЯ!", viewH / 2 + 20, Color.White * alpha, 1);
            }
        }

        private void DrawBullets()
        {
            foreach (var b in _model.Bullets)
            {
                int s  = b.Size;
                int bx = (int)(b.X - s / 2f);
                int by = (int)(b.Y - s / 2f);
                // Свечение
                _spriteBatch.Draw(_bulletTex, new Rectangle(bx - 3, by - 3, s + 6, s + 6),
                    new Color(255, 210, 0) * 0.4f);
                _spriteBatch.Draw(_bulletTex, new Rectangle(bx, by, s, s), Color.White);
            }
        }

        // --- Частицы ---

        private void SpawnParticles(int cx, int cy, Color color, int count = 14)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);
                float speed = 90f + (float)(Random.Shared.NextDouble() * 140f);
                float life  = 0.30f + (float)(Random.Shared.NextDouble() * 0.25f);
                float size  = 4f + (float)(Random.Shared.NextDouble() * 5f);
                _particles.Add(new Particle
                {
                    X = cx, Y = cy,
                    VX = MathF.Cos(angle) * speed,
                    VY = MathF.Sin(angle) * speed - 40f, // лёгкий импульс вверх
                    Life = life, MaxLife = life,
                    Color = color, Size = size,
                });
            }
        }

        private void UpdateParticles(float dt)
        {
            const float ParticleGravity = 200f;
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X    += p.VX * dt;
                p.Y    += p.VY * dt;
                p.VY   += ParticleGravity * dt;
                p.Life -= dt;
                if (p.Life <= 0f)
                    _particles.RemoveAt(i);
                else
                    _particles[i] = p;
            }
        }

        private void DrawParticles()
        {
            foreach (var p in _particles)
            {
                float t    = p.Life / p.MaxLife;   // 1 = только родилась, 0 = умирает
                float size = p.Size * t;
                int   s    = Math.Max(1, (int)size);
                int   px   = (int)(p.X - s * 0.5f);
                int   py   = (int)(p.Y - s * 0.5f);

                // Ядро искры (яркий цвет)
                _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, s, s), p.Color * t);
                // Свечение вокруг (больший квадрат, полупрозрачный)
                int gs = s + 3;
                _spriteBatch.Draw(_pixelTexture,
                    new Rectangle(px - 1, py - 1, gs, gs),
                    p.Color * (t * 0.35f));
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

        private void DrawRect(Rectangle r, Color c)
            => _spriteBatch.Draw(_pixelTexture, r, c);

        private void DrawRectBorder(Rectangle r, Color c, int thickness)
        {
            _spriteBatch.Draw(_pixelTexture, new Rectangle(r.X, r.Y, r.Width, thickness), c);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(r.X, r.Y, thickness, r.Height), c);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
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
