using Application.DTOs.Article;
using Application.Interfaces;
using ClosedXML.Excel;
using Domain.Enums;

namespace Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly IArticleRepository _articleRepository;

    private static readonly string[] AllColumns =
    {
        "Basic",
        "Fabric",
        "Pricing",
        "Department",
        "Size Breakdown"
    };

    public ExportService(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<byte[]> ExportArticlesToExcelAsync(
        ArticleExportRequestDto request)
    {
        if (request.ArticleIds == null ||
            !request.ArticleIds.Any())
        {
            throw new InvalidOperationException(
                "No articles selected for export.");
        }

        var columns =
            request.Columns != null &&
            request.Columns.Any()
                ? request.Columns
                    .Where(column =>
                        AllColumns.Contains(column))
                    .ToList()
                : AllColumns.ToList();

        if (!columns.Any())
        {
            throw new InvalidOperationException(
                "No valid columns selected for export.");
        }

        var articles =
            await _articleRepository
                .GetAllActiveWithDetailsAsync();

        var selectedArticles =
            articles
                .Where(article =>
                    request.ArticleIds.Contains(article.Id))
                .OrderBy(article => article.OrderDate)
                .ToList();

        if (!selectedArticles.Any())
        {
            throw new InvalidOperationException(
                "No selected articles were found.");
        }

        var includeBasic =
            columns.Contains("Basic");

        var includeFabric =
            columns.Contains("Fabric");

        var includePricing =
            columns.Contains("Pricing");

        var includeDepartments =
            columns.Contains("Department");

        var includeSizeBreakdown =
            columns.Contains("Size Breakdown");

        var sizeLabels =
            includeSizeBreakdown
                ? selectedArticles
                    .SelectMany(article =>
                        article.SizeBreakdowns.Concat(article.CuttingSizeBreakdowns.Select(x => new Domain.Entities.ArticleSizeBreakdown
                        { SizeLabel = x.SizeLabel, OrderIndex = x.OrderIndex, Quantity = x.Quantity })))
                    .OrderBy(size =>
                        size.OrderIndex)
                    .ThenBy(size =>
                        size.SizeLabel)
                    .Select(size =>
                        size.SizeLabel)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : new List<string>();

        using var workbook = new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("Articles");

        var headers =
            new List<string>();

        if (includeBasic)
        {
            headers.AddRange(
                new[]
                {
                    "Article Code",
                    "Company Name",
                    "Colour",
                    "Season",
                    "Order Date",
                    "Delivery Date",
                    "Quantity",
                    "Status"
                });
        }

        if (includeFabric)
        {
            headers.AddRange(
                new[]
                {
                    "Fabric Codes",
                    "Fabric Types",
                    "Fabric Quantities Used"
                });
        }

        if (includePricing)
        {
            headers.AddRange(
                new[]
                {
                    "Price Per Piece",
                    "Total Price"
                });
        }

        if (includeDepartments)
        {
            headers.AddRange(
                new[]
                {
                    "Department Status Summary",
                    "Total Loss (pieces)"
                });
        }

        if (includeSizeBreakdown)
        {
            foreach (var sizeLabel in sizeLabels)
            {
                headers.Add($"Cutting {sizeLabel}");
                headers.Add($"A-Grade {sizeLabel}");
            }

            headers.Add("A-Grade Total");
            headers.Add("B-Grade Quantity");
            headers.Add("Total Quality Output");
            headers.Add("Loss Before Quality");
            headers.Add("C-Grade / Quality Loss");
        }

        for (var index = 0;
             index < headers.Count;
             index++)
        {
            worksheet.Cell(1, index + 1)
                .Value = headers[index];
        }

        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.Row(1).Style.Fill.BackgroundColor =
            XLColor.LightBlue;

        var rowNumber = 2;

        foreach (var article in selectedArticles)
        {
            var columnNumber = 1;

            if (includeBasic)
            {
                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.ArticleCode;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.CompanyName;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.Color ?? string.Empty;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.Season ?? string.Empty;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.OrderDate
                        .ToString("yyyy-MM-dd");

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.DeliveryDate
                        .ToString("yyyy-MM-dd");

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.Quantity ?? 0;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.IsDelivered
                        ? "Delivered"
                        : "In Progress";
            }

            if (includeFabric)
            {
                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = string.Join(
                        ", ",
                        article.FabricLinks
                            .Select(link =>
                                link.Fabric?.FabricCode ?? string.Empty));

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = string.Join(
                        ", ",
                        article.FabricLinks
                            .Select(link =>
                                link.Fabric?.FabricType ?? string.Empty));

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = string.Join(
                        ", ",
                        article.FabricLinks
                            .Select(link =>
                                link.QuantityUsed.ToString()));
            }

            if (includePricing)
            {
                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.PricePerPiece
                        ?.ToString()
                        ?? string.Empty;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = article.PriceTotal
                        ?.ToString()
                        ?? string.Empty;
            }

            if (includeDepartments)
            {
                var departmentSummary =
                    string.Join(
                        "; ",
                        article.DepartmentStatuses
                            .OrderBy(status =>
                                status.SequenceNumber)
                            .Select(status =>
                                $"{status.Department.Name}: {status.Status}"));

                var totalDepartmentLoss =
                    article.DepartmentStatuses
                        .Sum(status =>
                            status.LossQuantity ?? 0);

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = departmentSummary;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = totalDepartmentLoss;
            }

            if (includeSizeBreakdown)
            {
                foreach (var sizeLabel in sizeLabels)
                {
                    var sizeEntry =
                        article.SizeBreakdowns
                            .FirstOrDefault(size =>
                                string.Equals(
                                    size.SizeLabel,
                                    sizeLabel,
                                    StringComparison.OrdinalIgnoreCase));

                    var cuttingEntry = article.CuttingSizeBreakdowns.FirstOrDefault(size =>
                        string.Equals(size.SizeLabel, sizeLabel, StringComparison.OrdinalIgnoreCase));
                    worksheet.Cell(rowNumber, columnNumber++).Value = cuttingEntry?.Quantity ?? 0;
                    worksheet.Cell(rowNumber, columnNumber++).Value = sizeEntry?.Quantity ?? 0;
                }

                var aGradeTotal =
                    article.SizeBreakdowns
                        .Sum(size =>
                            size.Quantity);

                var bGradeTotal =
                    article.BGradeQuantity ?? 0;

                var totalQualityOutput =
                    aGradeTotal + bGradeTotal;

                var qualityStatus =
                    article.DepartmentStatuses
                        .FirstOrDefault(status =>
                            status.Department.Type ==
                            DepartmentType.QualityAndPacking);

                var qualityLoss =
                    qualityStatus?.LossQuantity ?? 0;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = aGradeTotal;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = bGradeTotal;

                worksheet.Cell(rowNumber, columnNumber++)
                    .Value = totalQualityOutput;

                var cuttingTotal = article.CuttingSizeBreakdowns.Sum(x => x.Quantity);
                var qualityInput = qualityStatus?.InputQuantity ?? cuttingTotal;
                var preQualityLoss = Math.Max(0, cuttingTotal - qualityInput);
                var cGradeLoss = Math.Max(0, qualityInput - aGradeTotal - bGradeTotal);
                worksheet.Cell(rowNumber, columnNumber++).Value = preQualityLoss;
                worksheet.Cell(rowNumber, columnNumber++).Value = cGradeLoss;
            }

            rowNumber++;
        }

        for (var index = 1;
             index <= headers.Count;
             index++)
        {
            worksheet.Column(index)
                .AdjustToContents();

            if (worksheet.Column(index).Width > 50)
            {
                worksheet.Column(index).Width = 50;
            }
        }

        worksheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}