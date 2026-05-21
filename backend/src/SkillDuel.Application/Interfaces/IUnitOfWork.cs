using System.Threading;
using System.Threading.Tasks;

namespace SkillDuel.Application.Interfaces;

public interface IUnitOfWork : System.IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
