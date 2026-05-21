using Moq;
using SkillDuel.Application.Services;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using SkillDuel.Domain.Constants;
using SkillDuel.Domain.Enums;

namespace SkillDuel.UnitTests.Services;

public class MatchmakingServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDb;
    private readonly MatchmakingService _service;
    private const GameMode DefaultMode = GameMode.Short;
    private static readonly string QueueKey = RedisKeys.MatchmakingQueue((int)DefaultMode);

    public MatchmakingServiceTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDb = new Mock<IDatabase>();
        _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDb.Object);
        _service = new MatchmakingService(_mockRedis.Object);
    }

    [Fact]
    public async Task JoinQueueAsync_ShouldPushToRedisList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mode = GameMode.Short;

        // Act
        await _service.JoinQueueAsync(userId, mode, null);

        // Assert
        _mockDb.Verify(x => x.ListRightPushAsync(RedisKeys.MatchmakingQueue((int)mode), userId.ToString(), When.Always, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task LeaveQueueAsync_ShouldRemoveFromAllRedisLists()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.LeaveQueueAsync(userId);

        // Assert
        _mockDb.Verify(x => x.ListRemoveAsync(RedisKeys.MatchmakingQueue((int)GameMode.Short), userId.ToString(), 0, CommandFlags.None), Times.Once);
        _mockDb.Verify(x => x.ListRemoveAsync(RedisKeys.MatchmakingQueue((int)GameMode.Long), userId.ToString(), 0, CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task TryMatchAsync_ShouldReturnNull_WhenQueueIsEmpty()
    {
        // Arrange
        _mockDb.Setup(x => x.ListLengthAsync(QueueKey, CommandFlags.None)).ReturnsAsync(0);

        // Act
        var result = await _service.TryMatchAsync(DefaultMode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryMatchAsync_ShouldReturnNull_WhenOnlyOnePlayerInQueue()
    {
        // Arrange
        _mockDb.Setup(x => x.ListLengthAsync(QueueKey, CommandFlags.None)).ReturnsAsync(1);

        // Act
        var result = await _service.TryMatchAsync(DefaultMode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryMatchAsync_ShouldReturnPair_WhenAtLeastTwoPlayersInQueue()
    {
        // Arrange
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        _mockDb.Setup(x => x.ListLengthAsync(QueueKey, CommandFlags.None)).ReturnsAsync(2);
        _mockDb.SetupSequence(x => x.ListLeftPopAsync(QueueKey, CommandFlags.None))
            .ReturnsAsync(p1.ToString())
            .ReturnsAsync(p2.ToString());

        // Act
        var result = await _service.TryMatchAsync(DefaultMode);

        // Assert
        result.Should().NotBeNull();
        result.Value.Player1.Should().Be(p1);
        result.Value.Player2.Should().Be(p2);
    }
}
