namespace FumoGame.Models
{
    public class BossModel
    {
        public float X             { get; set; }
        public float Y             { get; set; }
        public int   Width         { get; set; } = 90;
        public int   Height        { get; set; } = 90;
        public bool  Active        { get; set; }
        public float LifeTimer     { get; set; }   // seconds remaining
        public float ShootTimer    { get; set; }   // time until next shot
        public float AnnounceTimer { get; set; }   // "BOSS!" overlay
    }
}
