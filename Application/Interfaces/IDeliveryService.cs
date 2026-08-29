using Application.DTOs.Workflow;

namespace Application.Interfaces;

public interface IDeliveryService
{
    Task ConfirmDeliveryAsync(DeliveryConfirmDto dto, int updatedByUserId);
}