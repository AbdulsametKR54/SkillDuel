using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;

namespace SkillDuel.Application.Interfaces;

public interface IUserRepository : IGenericRepository<User> 
{
    Task<User?> GetByEmailAsync(string email, System.Threading.CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, System.Threading.CancellationToken cancellationToken = default);
    Task<List<User>> GetTopByEloAsync(int limit, System.Threading.CancellationToken cancellationToken = default);
    Task<(List<User> Items, int TotalCount)> GetPagedUsersAsync(string? search, int page, int pageSize, System.Threading.CancellationToken cancellationToken = default);
}
public interface IQuestionRepository : IGenericRepository<Question> 
{ 
    Task<List<Question>> GetRandomQuestionsAsync(int count, Guid? categoryId, DifficultyLevel? difficulty = null, QuestionType? questionType = null, System.Threading.CancellationToken cancellationToken = default);
    Task<(List<Question> Items, int TotalCount)> GetPagedAsync(Guid? categoryId, DifficultyLevel? difficulty, QuestionType? questionType, int page, int pageSize, System.Threading.CancellationToken cancellationToken = default);
    Task<Question?> GetWithCategoryByIdAsync(Guid id, System.Threading.CancellationToken cancellationToken = default);
    Task<List<Question>> GetPendingQuestionsAsync(System.Threading.CancellationToken cancellationToken = default);
}
public interface ICategoryRepository : IGenericRepository<Category> { }
public interface IGameSessionRepository : IGenericRepository<GameSession> 
{
    Task<GameSession?> GetActiveSessionByUserIdAsync(Guid userId);
    Task<List<GameSession>> GetByUserIdPagedAsync(Guid userId, int page, int pageSize);
}
public interface IGameRoundRepository : IGenericRepository<GameRound> { }
public interface IPlayerAnswerRepository : IGenericRepository<PlayerAnswer> { }
public interface IRoomRepository : IGenericRepository<Room> 
{
    Task<Room?> GetByCodeAsync(string code);
    Task<(List<Room> Items, int TotalCount)> GetPublicWaitingRoomsAsync(int page = 1, int pageSize = 10, string? searchName = null, Guid? categoryId = null, int? roundCount = null);
    Task<Room?> GetActiveRoomByUserIdAsync(Guid userId);
}
public interface IUserCategoryStatRepository : IGenericRepository<UserCategoryStat>
{
    Task<List<UserCategoryStat>> GetByUserIdAsync(Guid userId);
    Task<UserCategoryStat?> GetByUserAndCategoryAsync(Guid userId, Guid categoryId);
}
public interface IFriendshipRepository : IGenericRepository<Friendship>
{
    Task<List<Friendship>> GetFriendsByUserIdAsync(Guid userId);
    Task<Friendship?> GetFriendshipAsync(Guid userId, Guid friendId);
    Task<List<Friendship>> GetPendingRequestsAsync(Guid userId);
}



