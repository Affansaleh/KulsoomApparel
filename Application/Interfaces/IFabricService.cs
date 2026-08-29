using Application.DTOs.Fabric;

namespace Application.Interfaces;

public interface IFabricService
{
    Task<List<FabricResponseDto>> GetAllAsync();
    Task<FabricResponseDto?> GetByIdAsync(int id);
    Task<FabricResponseDto> CreateAsync(FabricCreateDto dto);
    Task TopUpAsync(FabricTopUpDto dto);
    Task DeductForArticleAsync(int fabricId, decimal quantityUsed);
    Task ReturnForArticleAsync(int fabricId, decimal quantity);
}