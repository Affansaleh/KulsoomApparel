using Application.DTOs.Fabric;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class FabricService : IFabricService
{
    private readonly IFabricRepository _fabricRepository;

    public FabricService(IFabricRepository fabricRepository)
    {
        _fabricRepository = fabricRepository;
    }

    public async Task<List<FabricResponseDto>> GetAllAsync()
    {
        var fabrics = await _fabricRepository.GetAllActiveAsync();
        return fabrics.Select(MapToDto).ToList();
    }

    public async Task<FabricResponseDto?> GetByIdAsync(int id)
    {
        var fabric = await _fabricRepository.GetByIdAsync(id);
        return fabric == null ? null : MapToDto(fabric);
    }

    public async Task<FabricResponseDto> CreateAsync(FabricCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Color))
            throw new InvalidOperationException("Fabric color is required.");
        var exists = await _fabricRepository.FabricVariantExistsAsync(dto.FabricCode.Trim(), dto.Color.Trim());
        if (exists)
            throw new InvalidOperationException("This fabric code and color already exist.");

        if (!Enum.TryParse<FabricUnit>(dto.Unit, true, out var unit))
            throw new InvalidOperationException("Unit must be Meter, Yard or Kg.");

        // When another color of the same code exists, common master information stays identical.
        var master = await _fabricRepository.GetByCodeAsync(dto.FabricCode.Trim());
        if (master != null)
        {
            dto.FabricType = master.FabricType;
            dto.InvNum = master.InvNum;
            dto.FabricDate = master.FabricDate;
            dto.Rate = master.Rate;
        }

        var fabric = new Fabric
        {
            FabricCode = dto.FabricCode,
            InvNum = dto.InvNum,
            FabricDate = dto.FabricDate,
            FabricType = dto.FabricType.Trim(),
            Color = dto.Color.Trim(),
            Quantity = dto.Quantity ?? 0,
            AvailableQuantity = dto.Quantity ?? 0,
            Unit = unit,
            Rate = dto.Rate ?? 0,
            TotalAmount = (dto.Quantity ?? 0) * (dto.Rate ?? 0),
            Status = (dto.Quantity ?? 0) > 0 ? FabricStatus.InStock : FabricStatus.OutOfStock,
            IsActive = true
        };

        await _fabricRepository.AddAsync(fabric);
        await _fabricRepository.SaveChangesAsync();

        return MapToDto(fabric);
    }

    public async Task TopUpAsync(FabricTopUpDto dto)
    {
        var fabric = await _fabricRepository.GetByIdAsync(dto.FabricId);
        if (fabric == null)
            throw new InvalidOperationException("Fabric not found.");

        fabric.Quantity += dto.AddedQuantity;
        fabric.AvailableQuantity += dto.AddedQuantity;

        if (dto.NewRate.HasValue)
            fabric.Rate = dto.NewRate.Value;

        fabric.TotalAmount = fabric.Quantity * fabric.Rate;
        fabric.Status = fabric.AvailableQuantity > 0 ? FabricStatus.InStock : FabricStatus.OutOfStock;

        _fabricRepository.Update(fabric);
        await _fabricRepository.SaveChangesAsync();
    }

    public async Task DeductForArticleAsync(int fabricId, decimal quantityUsed)
    {
        var fabric = await _fabricRepository.GetByIdAsync(fabricId);
        if (fabric == null)
            throw new InvalidOperationException("Fabric not found.");

        if (fabric.AvailableQuantity < quantityUsed)
            throw new InvalidOperationException($"Not enough stock for fabric '{fabric.FabricCode}'. Available: {fabric.AvailableQuantity}, Required: {quantityUsed}");

        fabric.AvailableQuantity -= quantityUsed;

        if (fabric.AvailableQuantity <= 0)
        {
            fabric.AvailableQuantity = 0;
            fabric.Status = FabricStatus.OutOfStock;
        }

        _fabricRepository.Update(fabric);
        await _fabricRepository.SaveChangesAsync();   // ← FIXED: SaveChanges added
    }

    public async Task ReturnForArticleAsync(int fabricId, decimal quantity)
    {
        var fabric = await _fabricRepository.GetByIdAsync(fabricId);
        if (fabric == null)
            throw new InvalidOperationException("Fabric not found.");

        fabric.AvailableQuantity += quantity;
        if (fabric.AvailableQuantity > 0)
            fabric.Status = FabricStatus.InStock;

        _fabricRepository.Update(fabric);
        await _fabricRepository.SaveChangesAsync();
    }

    private static FabricResponseDto MapToDto(Fabric f) => new()
    {
        Id = f.Id,
        FabricCode = f.FabricCode,
        InvNum = f.InvNum,
        FabricDate = f.FabricDate,
        FabricType = f.FabricType,
        Color = f.Color,
        Quantity = f.Quantity,
        AvailableQuantity = f.AvailableQuantity,
        Unit = f.Unit.ToString(),
        Rate = f.Rate,
        TotalAmount = f.TotalAmount,
        Status = f.Status.ToString()
    };
}