namespace FumoGame.Models
{
    public enum PowerUpType { Coin, Shield, Slow }

    public class CoinModel
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 44;
        public int Height { get; set; } = 44;
        public PowerUpType Type { get; set; }
        public bool Collected { get; set; }

        public CoinModel(int x, int y, PowerUpType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }
}
