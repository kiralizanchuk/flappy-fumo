using Microsoft.Xna.Framework;
using FumoGame.Models;

namespace FumoGame.Helpers
{
    /// <summary>
    /// Чистая логика проверки столкновений — без зависимости от GraphicsDevice.
    /// Выделена отдельно, чтобы покрыть модульными тестами.
    /// </summary>
    public static class CollisionHelper
    {
        private const int PlayerMargin    = 5;
        private const int PlayerTopMargin = 22;
        private const int PipeMargin      = 5;

        /// <summary>Хитбокс игрока с учётом отступов.</summary>
        public static Rectangle GetPlayerRect(PlayerModel player)
            => new Rectangle(
                player.X + PlayerMargin,
                player.Y + PlayerTopMargin,
                player.Width  - PlayerMargin * 2,
                player.Height - PlayerTopMargin - PlayerMargin);

        /// <summary>Пересекается ли игрок с трубой.</summary>
        public static bool PlayerHitsPipe(PlayerModel player, PipeModel pipe, int screenHeight)
        {
            var playerRect = GetPlayerRect(player);

            var topPipe = new Rectangle(
                pipe.X + PipeMargin, 0,
                pipe.Width - PipeMargin * 2, pipe.TopHeight);

            var bottomPipe = new Rectangle(
                pipe.X + PipeMargin, pipe.TopHeight + pipe.Gap,
                pipe.Width - PipeMargin * 2, screenHeight - (pipe.TopHeight + pipe.Gap));

            return playerRect.Intersects(topPipe) || playerRect.Intersects(bottomPipe);
        }

        /// <summary>Находится ли монета внутри зазора трубы по вертикали.</summary>
        public static bool CoinInsidePipeGap(CoinModel coin, PipeModel pipe)
        {
            int gapTop    = pipe.TopHeight;
            int gapBottom = pipe.TopHeight + pipe.Gap;
            return coin.Y >= gapTop && (coin.Y + coin.Height) <= gapBottom;
        }

        /// <summary>Должен ли босс появиться при данном счёте и пороге.</summary>
        public static bool ShouldSpawnBoss(int score, int nextBossScore)
            => score >= nextBossScore;
    }
}
