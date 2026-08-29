using Application.DTOs.User;

namespace Application.Interfaces;

public interface IUserService
{
    Task<List<UserResponseDto>> GetAllUsersAsync();
    Task<UserResponseDto?> GetUserByIdAsync(int id);
    Task UpdateOwnCredentialsAsync(int userId, UserUpdateDto dto);
    Task AdminUpdateUserAsync(int targetUserId, string newUsername, int? newDepartmentId);
}