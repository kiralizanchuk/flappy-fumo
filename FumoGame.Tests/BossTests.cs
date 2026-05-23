using FumoGame.Models;
using FumoGame.Helpers;

namespace FumoGame.Tests;

/// <summary>
/// Тесты логики спавна босса.
/// </summary>
public class BossTests
{
    [Theory]
    [InlineData(60,  60,  true)]
    [InlineData(59,  60,  false)]
    [InlineData(120, 120, true)]
    [InlineData(180, 180, true)]
    [InlineData(0,   60,  false)]
    public void ShouldSpawnBoss_CorrectlyAtThreshold(int score, int threshold, bool expected)
    {
        Assert.Equal(expected, CollisionHelper.ShouldSpawnBoss(score, threshold));
    }

    [Fact]
    public void BossModel_DefaultSize_Is90x90()
    {
        var boss = new BossModel();
        Assert.Equal(90, boss.Width);
        Assert.Equal(90, boss.Height);
    }

    [Fact]
    public void BossModel_InitiallyInactive()
    {
        var boss = new BossModel();
        Assert.False(boss.Active);
    }

    [Fact]
    public void GameModel_BossThreshold_IncreasesBy60EachTime()
    {
        var model = new GameModel();
        Assert.Equal(60, model.NextBossScore);
        model.NextBossScore += 60;
        Assert.Equal(120, model.NextBossScore);
        model.NextBossScore += 60;
        Assert.Equal(180, model.NextBossScore);
    }
}
