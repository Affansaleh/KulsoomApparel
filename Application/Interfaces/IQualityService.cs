using Application.DTOs.Workflow;

namespace Application.Interfaces;

public interface IQualityService
{
    Task SubmitGradesAsync(QualityGradeEntryDto dto, int updatedByUserId);
}