using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Entities;

namespace Application.Interfaces;

public interface IFabricRepository
{
    Task<Fabric?> GetByIdAsync(int id);
    Task<Fabric?> GetByCodeAsync(string fabricCode);
    Task<List<Fabric>> GetAllActiveAsync();
    Task AddAsync(Fabric fabric);
    void Update(Fabric fabric);
    Task<bool> FabricVariantExistsAsync(string fabricCode, string color);
    Task SaveChangesAsync();
}