namespace FumoGame.Models
{
    public enum PipeType { Normal, Moving, Narrow }

    public class PipeModel
    {
        public int X { get; set; }
        public int Width { get; }
        public int TopHeight { get; set; }
        public int Gap { get; }
        public bool Scored { get; set; }
        public PipeType Type { get; }

        public float MoveSpeed { get; }
        public int MoveDirection { get; set; }
        public int MinTopHeight { get; }
        public int MaxTopHeight { get; }

        private const int NormalWidth = 60;
        private const int NarrowWidth = 35;

        public PipeModel(int x, int topHeight, int gap, PipeType type = PipeType.Normal,
            float moveSpeed = 0, int minTopHeight = 0, int maxTopHeight = 0)
        {
            X = x;
            TopHeight = topHeight;
            Gap = gap;
            Type = type;
            Width = type == PipeType.Narrow ? NarrowWidth : NormalWidth;
            MoveSpeed = moveSpeed;
            MoveDirection = 1;
            MinTopHeight = minTopHeight;
            MaxTopHeight = maxTopHeight;
        }
    }
}
