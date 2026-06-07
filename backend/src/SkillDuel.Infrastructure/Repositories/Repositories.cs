using Microsoft.EntityFrameworkCore;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using SkillDuel.Domain.Enums;
using SkillDuel.Infrastructure.Data;

namespace SkillDuel.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(SkillDuelDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, System.Threading.CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, System.Threading.CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<List<User>> GetTopByEloAsync(int limit, System.Threading.CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderByDescending(u => u.EloRating)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<User> Items, int TotalCount)> GetPagedUsersAsync(string? search, int page, int pageSize, System.Threading.CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(cleanSearch) || u.Email.ToLower().Contains(cleanSearch));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
{
    public QuestionRepository(SkillDuelDbContext context) : base(context) { }

    public async Task<List<Question>> GetRandomQuestionsAsync(int count, Guid? categoryId, DifficultyLevel? difficulty = null, QuestionType? questionType = null, System.Threading.CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        
        query = query.Where(q => q.Status == QuestionStatus.Approved);

        if (categoryId.HasValue)
        {
            query = query.Where(q => q.CategoryId == categoryId.Value);
        }
        if (difficulty.HasValue)
        {
            query = query.Where(q => q.DifficultyLevel == difficulty.Value);
        }
        if (questionType.HasValue)
        {
            query = query.Where(q => q.QuestionType == questionType.Value);
        }

        return await query
            .OrderBy(q => EF.Functions.Random()) 
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Question>> GetPendingQuestionsAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(q => q.Category)
            .Include(q => q.CreatedByUser)
            .Where(q => q.Status == QuestionStatus.Pending)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Question> Items, int TotalCount)> GetPagedAsync(Guid? categoryId, DifficultyLevel? difficulty, QuestionType? questionType, int page, int pageSize, System.Threading.CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        
        query = query.Where(q => q.Status == QuestionStatus.Approved);

        if (categoryId.HasValue)
        {
            query = query.Where(q => q.CategoryId == categoryId.Value);
        }

        if (difficulty.HasValue)
        {
            query = query.Where(q => q.DifficultyLevel == difficulty.Value);
        }

        if (questionType.HasValue)
        {
            query = query.Where(q => q.QuestionType == questionType.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(q => q.Category)
            .Include(q => q.CreatedByUser)
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Question?> GetWithCategoryByIdAsync(Guid id, System.Threading.CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(q => q.Category)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }
}

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(SkillDuelDbContext context) : base(context) { }
}

public class GameSessionRepository : GenericRepository<GameSession>, IGameSessionRepository
{
    public GameSessionRepository(SkillDuelDbContext context) : base(context) { }

    public async Task<GameSession?> GetActiveSessionByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => (s.Player1Id == userId || s.Player2Id == userId || s.Player3Id == userId || s.Player4Id == userId) && s.Status == GameStatus.Active);
    }

    public async Task<List<GameSession>> GetByUserIdPagedAsync(Guid userId, int page, int pageSize)
    {
        return await _dbSet
            .Include(s => s.Player1)
            .Include(s => s.Player2)
            .Include(s => s.Player3)
            .Include(s => s.Player4)
            .Where(s => s.Player1Id == userId || s.Player2Id == userId || s.Player3Id == userId || s.Player4Id == userId)
            .OrderByDescending(s => s.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}

public class GameRoundRepository : GenericRepository<GameRound>, IGameRoundRepository
{
    public GameRoundRepository(SkillDuelDbContext context) : base(context) { }
}

public class PlayerAnswerRepository : GenericRepository<PlayerAnswer>, IPlayerAnswerRepository
{
    public PlayerAnswerRepository(SkillDuelDbContext context) : base(context) { }
}

public class RoomRepository : GenericRepository<Room>, IRoomRepository
{
    public RoomRepository(SkillDuelDbContext context) : base(context) { }

    public async Task<Room?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .Include(r => r.Host)
            .Include(r => r.Guest)
            .Include(r => r.Category)
            .Include(r => r.Players).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.Code == code);
    }

    public async Task<List<Room>> GetPublicWaitingRoomsAsync()
    {
        return await _dbSet
            .Include(r => r.Host)
            .Include(r => r.Category)
            .Include(r => r.Players).ThenInclude(p => p.User)
            .Where(r => r.Status == RoomStatus.Waiting && !r.IsPrivate && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Room?> GetActiveRoomByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(r => r.Host)
            .Include(r => r.Players).ThenInclude(p => p.User)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(r => r.Status != RoomStatus.Closed && r.Players.Any(p => p.UserId == userId));
    }
}

public class UserCategoryStatRepository : GenericRepository<UserCategoryStat>, IUserCategoryStatRepository
{
    public UserCategoryStatRepository(SkillDuelDbContext context) : base(context) { }

    public async Task<List<UserCategoryStat>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(s => s.Category)
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserCategoryStat?> GetByUserAndCategoryAsync(Guid userId, Guid categoryId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CategoryId == categoryId);
    }
}

public class FriendshipRepository : GenericRepository<Friendship>, IFriendshipRepository
{
    public FriendshipRepository(SkillDuelDbContext context) : base(context) { }

    public async Task<List<Friendship>> GetFriendsByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(f => f.User)
            .Include(f => f.Friend)
            .Where(f => f.Status == FriendshipStatus.Accepted && (f.UserId == userId || f.FriendId == userId))
            .ToListAsync();
    }

    public async Task<Friendship?> GetFriendshipAsync(Guid userId, Guid friendId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(f => (f.UserId == userId && f.FriendId == friendId) || (f.UserId == friendId && f.FriendId == userId));
    }

    public async Task<List<Friendship>> GetPendingRequestsAsync(Guid userId)
    {
        return await _dbSet
            .Include(f => f.User)
            .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
            .ToListAsync();
    }
}


