using FumoGame.Models;
using FumoGame.Helpers;

namespace FumoGame.Tests;

/// <summary>
/// Тесты коллизий — игрок и трубы, монеты в зазоре.
/// </summary>
public class CollisionTests
{
    private const int ScreenHeight = 1080;

    // --- Хитбокс игрока ---

    [Fact]
    public void PlayerRect_Width_IsReducedByMargins()
    {
        var player = new PlayerModel(100, 100, ScreenHeight);
        var rect = CollisionHelper.GetPlayerRect(player);
        Assert.Equal(player.Width - 10, rect.Width); // PlayerMargin*2 = 10
    }

    [Fact]
    public void PlayerRect_Top_IsShiftedDownByTopMargin()
    {
        var player = new PlayerModel(100, 200, ScreenHeight);
        var rect = CollisionHelper.GetPlayerRect(player);
        Assert.Equal(200 + 22, rect.Y); // PlayerTopMargin = 22
    }

    // --- Коллизия с трубами ---

    [Fact]
    public void PlayerHitsPipe_WhenInsideTopPipe()
    {
        // Игрок в верхней трубе (труба сверху высотой 300)
        var player = new PlayerModel(200, 0, ScreenHeight);
        var pipe   = new PipeModel(x: 190, topHeight: 300, gap: 185);
        Assert.True(CollisionHelper.PlayerHitsPipe(player, pipe, ScreenHeight));
    }

    [Fact]
    public void PlayerHitsPipe_WhenInsideBottomPipe()
    {
        // Игрок в нижней трубе (после зазора)
        var player = new PlayerModel(200, 900, ScreenHeight);
        var pipe   = new PipeModel(x: 190, topHeight: 300, gap: 185);
        Assert.True(CollisionHelper.PlayerHitsPipe(player, pipe, ScreenHeight));
    }

    [Fact]
    public void PlayerHitsPipe_False_WhenInGap()
    {
        // Игрок в зазоре трубы — столкновения нет
        var player = new PlayerModel(200, 450, ScreenHeight);
        var pipe   = new PipeModel(x: 190, topHeight: 300, gap: 400);
        Assert.False(CollisionHelper.PlayerHitsPipe(player, pipe, ScreenHeight));
    }

    [Fact]
    public void PlayerHitsPipe_False_WhenPipeIsFarAway()
    {
        var player = new PlayerModel(100, 400, ScreenHeight);
        var pipe   = new PipeModel(x: 600, topHeight: 300, gap: 185);
        Assert.False(CollisionHelper.PlayerHitsPipe(player, pipe, ScreenHeight));
    }

    // --- Монета в зазоре ---

    [Fact]
    public void CoinInsidePipeGap_True_WhenInGap()
    {
        // Зазор: 300–500, монета на Y=350
        var pipe = new PipeModel(x: 0, topHeight: 300, gap: 200);
        var coin = new CoinModel(0, 350, PowerUpType.Magnet);
        Assert.True(CollisionHelper.CoinInsidePipeGap(coin, pipe));
    }

    [Fact]
    public void CoinInsidePipeGap_False_WhenInTopPipe()
    {
        // Зазор: 300–500, монета на Y=100 (верхняя труба)
        var pipe = new PipeModel(x: 0, topHeight: 300, gap: 200);
        var coin = new CoinModel(0, 100, PowerUpType.Magnet);
        Assert.False(CollisionHelper.CoinInsidePipeGap(coin, pipe));
    }

    [Fact]
    public void CoinInsidePipeGap_False_WhenInBottomPipe()
    {
        // Зазор: 300–500, монета на Y=520 (нижняя труба)
        var pipe = new PipeModel(x: 0, topHeight: 300, gap: 200);
        var coin = new CoinModel(0, 520, PowerUpType.Magnet);
        Assert.False(CollisionHelper.CoinInsidePipeGap(coin, pipe));
    }
}
