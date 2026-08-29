using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class FabricRepository : IFabricRepository
{
    private readonly ApplicationDbContext _context;

    public FabricRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Fabric?> GetByIdAsync(int id)
    {
        return await _context.Fabrics.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Fabric?> GetByCodeAsync(string fabricCode)
    {
        return await _context.Fabrics.FirstOrDefaultAsync(f => f.FabricCode == fabricCode);
    }

    public async Task<List<Fabric>> GetAllActiveAsync()
    {
        return await _context.Fabrics
            .Where(f => f.IsActive)
            .ToListAsync();
    }

    public async Task AddAsync(Fabric fabric)
    {
        await _context.Fabrics.AddAsync(fabric);
    }

    public void Update(Fabric fabric)
    {
        _context.Fabrics.Update(fabric);
    }

    public async Task<bool> FabricCodeExistsAsync(string fabricCode)
    {
        return await _context.Fabrics.AnyAsync(f => f.FabricCode == fabricCode);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}