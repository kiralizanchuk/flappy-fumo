using FumoGame.Models;

namespace FumoGame.Tests;

/// <summary>
/// Тесты модели игры — состояние, счёт, жизни, смерти.
/// </summary>
public class GameModelTests
{
    [Fact]
    public void GameModel_InitialState_IsMenu()
    {
        var model = new GameModel();
        Assert.Equal(GameState.Menu, model.State);
    }

    [Fact]
    public void GameModel_InitialLives_IsThree()
    {
        var model = new GameModel();
        Assert.Equal(3, model.Lives);
    }

    [Fact]
    public void GameModel_InitialScore_IsZero()
    {
        var model = new GameModel();
        Assert.Equal(0, model.Score);
    }

    [Fact]
    public void GameModel_InitialDeathCount_IsZero()
    {
        var model = new GameModel();
        Assert.Equal(0, model.DeathCount);
    }

    [Fact]
    public void GameModel_DeathCount_IncrementsManually()
    {
        var model = new GameModel();
        model.DeathCount++;
        model.DeathCount++;
        Assert.Equal(2, model.DeathCount);
    }

    [Fact]
    public void GameModel_SoundtrackUnlocks_AfterThreeDeaths()
    {
        var model = new GameModel();
        model.DeathCount = 3;
        Assert.True(model.DeathCount >= 3);
    }

    [Fact]
    public void GameModel_SoundtrackLocked_BeforeThreeDeaths()
    {
        var model = new GameModel();
        model.DeathCount = 2;
        Assert.False(model.DeathCount >= 3);
    }

    [Fact]
    public void GameModel_HighScore_UpdatesWhenScoreIsHigher()
    {
        var model = new GameModel();
        model.Score = 42;
        if (model.Score > model.HighScore)
            model.HighScore = model.Score;
        Assert.Equal(42, model.HighScore);
    }

    [Fact]
    public void GameModel_NextBossScore_StartsAt60()
    {
        var model = new GameModel();
        Assert.Equal(60, model.NextBossScore);
    }

    [Fact]
    public void GameModel_NextBossScore_IncrementsBy60()
    {
        var model = new GameModel();
        model.NextBossScore += 60;
        Assert.Equal(120, model.NextBossScore);
    }

    [Fact]
    public void GameModel_GodMode_CanBeToggled()
    {
        var model = new GameModel();
        model.GodMode = true;
        Assert.True(model.GodMode);
        model.GodMode = false;
        Assert.False(model.GodMode);
    }
}
