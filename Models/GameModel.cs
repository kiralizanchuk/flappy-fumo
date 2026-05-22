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
        public bool GodMode { get; set; }
    }
}
