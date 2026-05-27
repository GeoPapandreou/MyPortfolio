using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Exceptions;
using MyPortfolioAPI.Models;

namespace MyPortfolioAPI.Services;

public interface IAuthApplicationService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);
}

public sealed class AuthApplicationService : IAuthApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public AuthApplicationService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ClientSafeException("Full name, email, and password are required.", StatusCodes.Status400BadRequest);
        }

        var exists = await _userRepository.EmailExistsAsync(email, cancellationToken);
        if (exists)
        {
            throw new ClientSafeException("An account with this email already exists.", StatusCodes.Status409Conflict);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.Add(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ClientSafeException("Email and password are required.", StatusCodes.Status400BadRequest);
        }

        var user = await _userRepository.FindByEmailAsync(email, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new ClientSafeException("The email or password is not correct.", StatusCodes.Status401Unauthorized);
        }

        return BuildResponse(user);
    }

    private AuthResponseDto BuildResponse(User user)
    {
        return new AuthResponseDto
        {
            Token = _tokenService.Generate(user),
            Email = user.Email,
            DisplayName = ResolveDisplayName(user)
        };
    }

    private static string ResolveDisplayName(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName.Trim();
        }

        return user.Email.Split('@')[0];
    }
}
