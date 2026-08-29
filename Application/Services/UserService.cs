using Application.DTOs.User;
using Application.Interfaces;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserResponseDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();

        return users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            Username = u.Username,
            Role = u.Role.ToString(),
            DepartmentId = u.DepartmentId,
            DepartmentName = u.Department?.Name,
            DepartmentOrderIndex = u.Department?.OrderIndex,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList();
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;

        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role.ToString(),
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            DepartmentOrderIndex = user.Department?.OrderIndex,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task UpdateOwnCredentialsAsync(int userId, UserUpdateDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        if (!string.IsNullOrWhiteSpace(dto.NewUsername) && dto.NewUsername != user.Username)
        {
            var exists = await _userRepository.UsernameExistsAsync(dto.NewUsername);
            if (exists)
                throw new InvalidOperationException("This username is already taken.");

            user.Username = dto.NewUsername;
        }

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();
    }

    public async Task AdminUpdateUserAsync(int targetUserId, string newUsername, int? newDepartmentId)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        if (!string.IsNullOrWhiteSpace(newUsername) && newUsername != user.Username)
        {
            var exists = await _userRepository.UsernameExistsAsync(newUsername);
            if (exists)
                throw new InvalidOperationException("This username is already taken.");
            user.Username = newUsername;
        }

        if (newDepartmentId.HasValue && newDepartmentId.Value != user.DepartmentId)
            user.DepartmentId = newDepartmentId;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();
    }
}