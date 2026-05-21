using System.Threading;
using System.Threading.Tasks;
using SkillDuel.Application.Interfaces;
using SkillDuel.Infrastructure.Data;

namespace SkillDuel.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SkillDuelDbContext _context;

    public UnitOfWork(SkillDuelDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
