using System.Threading;
using System.Threading.Tasks;
using SkillDuel.Application.Common;
using SkillDuel.Application.DTOs.Auth;
using SkillDuel.Application.Interfaces;
using SkillDuel.Domain.Entities;
using BCrypt.Net;

namespace SkillDuel.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtProvider _jwtProvider;

    public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork, IJwtProvider jwtProvider)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtProvider = jwtProvider;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            return ApiResponse<AuthResponse>.FailureResult("Bu e-posta adresi zaten kullanımda.");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtProvider.Generate(user);
        var refreshToken = _jwtProvider.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = System.DateTime.UtcNow.AddDays(7);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            token,
            refreshToken,
            user.EloRating));
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiResponse<AuthResponse>.FailureResult("Geçersiz e-posta veya şifre.");
        }

        if (user.IsBanned)
        {
            if (user.BanExpiresAt.HasValue && user.BanExpiresAt.Value <= System.DateTime.UtcNow)
            {
                user.IsBanned = false;
                user.BanExpiresAt = null;
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var banMessage = user.BanExpiresAt.HasValue 
                    ? $"Hesabınız {user.BanExpiresAt.Value:dd.MM.yyyy HH:mm} tarihine kadar yasaklandı."
                    : "Hesabınız kalıcı olarak yasaklandı.";
                return ApiResponse<AuthResponse>.FailureResult(banMessage);
            }
        }

        var token = _jwtProvider.Generate(user);
        var refreshToken = _jwtProvider.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = System.DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            token,
            refreshToken,
            user.EloRating));
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var principal = _jwtProvider.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            return ApiResponse<AuthResponse>.FailureResult("Geçersiz token.");
        }

        var userIdStr = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !System.Guid.TryParse(userIdStr, out System.Guid userId))
        {
            return ApiResponse<AuthResponse>.FailureResult("Token içerisinde kullanıcı bilgisi bulunamadı.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= System.DateTime.UtcNow)
        {
            return ApiResponse<AuthResponse>.FailureResult("Geçersiz veya süresi dolmuş yenileme jetonu.");
        }

        var newAccessToken = _jwtProvider.Generate(user);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = System.DateTime.UtcNow.AddDays(7);
        
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            newAccessToken,
            newRefreshToken,
            user.EloRating));
    }
}
