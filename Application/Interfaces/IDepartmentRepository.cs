using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Entities;

namespace Application.Interfaces;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(int id);
    Task<List<Department>> GetAllAsync();
    Task<Department?> GetByTypeAsync(Domain.Enums.DepartmentType type);
    Task AddAsync(Department department);
    void Update(Department department);
    Task SaveChangesAsync();
}
