using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<List<User>> GetAllAsync();
    Task<List<User>> GetByDepartmentAsync(int departmentId);
    Task AddAsync(User user);
    void Update(User user);
    Task<bool> UsernameExistsAsync(string username);
    Task SaveChangesAsync();
}