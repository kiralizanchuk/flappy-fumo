using FumoGame.Models;

namespace FumoGame.Tests;

/// <summary>
/// Тесты модели игрока — стартовая позиция, размеры, скорость.
/// </summary>
public class PlayerModelTests
{
    [Fact]
    public void PlayerModel_Size_Is80x80()
    {
        var player = new PlayerModel(100, 200, 1080);
        Assert.Equal(80, player.Width);
        Assert.Equal(80, player.Height);
    }

    [Fact]
    public void PlayerModel_StartPosition_IsRemembered()
    {
        var player = new PlayerModel(150, 300, 1080);
        Assert.Equal(150, player.StartX);
        Assert.Equal(300, player.StartY);
    }

    [Fact]
    public void PlayerModel_InitialVelocity_IsZero()
    {
        var player = new PlayerModel(100, 200, 1080);
        Assert.Equal(0f, player.VelocityY);
    }

    [Fact]
    public void PlayerModel_Position_CanBeChanged()
    {
        var player = new PlayerModel(100, 200, 1080);
        player.Y = 400;
        Assert.Equal(400, player.Y);
    }

    [Fact]
    public void PlayerModel_StartPosition_DoesNotChangeAfterMove()
    {
        var player = new PlayerModel(100, 200, 1080);
        player.Y = 999;
        Assert.Equal(200, player.StartY); // стартовая позиция неизменна
    }
}
