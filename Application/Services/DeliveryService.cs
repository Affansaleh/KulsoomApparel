using Application.DTOs.Workflow;
using Application.Interfaces;
using Domain.Enums;

namespace Application.Services;

public class DeliveryService : IDeliveryService
{
    private readonly IArticleRepository _articleRepository;

    public DeliveryService(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task ConfirmDeliveryAsync(DeliveryConfirmDto dto, int updatedByUserId)
    {
        var article = await _articleRepository.GetByIdAsync(dto.ArticleId);
        if (article == null)
            throw new InvalidOperationException("Article not found.");

        var deliveryStatus = article.DepartmentStatuses
            .FirstOrDefault(s => s.Department.Type == DepartmentType.Delivery);

        if (deliveryStatus == null)
            throw new InvalidOperationException("Delivery step not found for this article.");

        if (deliveryStatus.Status != DepartmentStatus.InProcess)
            throw new InvalidOperationException("Delivery work has not been started.");

        article.PackedBy = dto.PackedBy;
        article.CheckedBy = dto.CheckedBy;
        article.NoOfCartons = dto.NoOfCartons;
        article.IsDelivered = true;
        article.DeliveredAt = DateTime.UtcNow;

        deliveryStatus.Note = dto.Note;
        deliveryStatus.Status = DepartmentStatus.Done;
        deliveryStatus.EndedAt = DateTime.UtcNow;
        deliveryStatus.UpdatedByUserId = updatedByUserId;

        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync();
    }
}