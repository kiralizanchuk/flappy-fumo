using System.Collections.Generic;

namespace FumoGame.Models
{
    public enum GameState { Menu, Playing, GameOver }

    public class GameModel
    {
        public GameState State { get; set; } = GameState.Menu;
        public int Score { get; set; }
        public int HighScore { get; set; }
        public double PipeSpawnTimer { get; set; }
        public int AnimationFrameIndex { get; set; }
        public double AnimationFrameTime { get; set; }
        public PlayerModel Player { get; set; } = null!;
        public List<PipeModel> Pipes { get; set; } = new();
        public List<CoinModel> Coins { get; set; } = new();
        public bool GodMode { get; set; }

        // Жизни
        public int Lives { get; set; } = 3;
        public float InvincibilityTimer { get; set; } = 0f;

        // Активные бонусы
        public float ShieldTimer { get; set; } = 0f;
        public float SlowTimer { get; set; } = 0f;

        // Таблица рекордов (топ-5)
        public List<int> TopScores { get; set; } = new();

        // Босс
        public BossModel?      Boss          { get; set; }
        public List<BulletModel> Bullets     { get; set; } = new();
        public int             NextBossScore { get; set; } = 40;
    }
}
