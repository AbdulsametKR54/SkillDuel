using Moq;
using SkillDuel.Application.Services;
using SkillDuel.Application.Interfaces;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Hangfire;

namespace SkillDuel.UnitTests.Services;

public class GameServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDb;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IQuestionRepository> _mockQuestionRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IGameSessionRepository> _mockSessionRepo;
    private readonly Mock<IGameNotificationService> _mockNotification;
    private readonly Mock<IBackgroundJobClient> _mockHangfire;
    private readonly GameService _service;

    public GameServiceTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDb = new Mock<IDatabase>();
        _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDb.Object);
        
        _mockUow = new Mock<IUnitOfWork>();
        _mockQuestionRepo = new Mock<IQuestionRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockSessionRepo = new Mock<IGameSessionRepository>();
        _mockNotification = new Mock<IGameNotificationService>();
        _mockHangfire = new Mock<IBackgroundJobClient>();

        _service = new GameService(
            _mockRedis.Object,
            _mockUow.Object,
            _mockQuestionRepo.Object,
            _mockUserRepo.Object,
            _mockSessionRepo.Object,
            _mockNotification.Object,
            _mockHangfire.Object
        );
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        _service.Should().NotBeNull();
    }
}
