namespace FumoGame.Models
{
    public class PlayerModel
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; } = 80;
        public int Height { get; } = 80;
        public float VelocityY { get; set; }
        public int StartY { get; }
        public int ScreenHeight { get; }

        public PlayerModel(int x, int y, int screenHeight)
        {
            X = x;
            Y = y;
            StartY = y;
            ScreenHeight = screenHeight;
        }
    }
}
