using System.Text.Json;
using Application.DTOs.Article;
using Application.DTOs.Fabric;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationWeb.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ArticleEditModel : PageModel
{
    private readonly IArticleService _articleService;
    private readonly IFabricService _fabricService;
    private readonly IWorkflowService _workflowService;

    public ArticleEditModel(IArticleService articleService, IFabricService fabricService, IWorkflowService workflowService)
    {
        _articleService = articleService;
        _fabricService = fabricService;
        _workflowService = workflowService;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public ArticleUpdateDto Input { get; set; } = new();

    [BindProperty]
    public FabricCreateDto NewFabric { get; set; } = new();

    [BindProperty]
    public string? AltCodesCsv { get; set; }

    // Staged existing-fabric quantity changes: JSON {"fabricId": qty, ...}
    [BindProperty]
    public string? ExistingQtyJson { get; set; }

    // Fabric ids to remove (comma-separated).
    [BindProperty]
    public string? RemovedFabricIdsCsv { get; set; }

    // New fabrics to create + link on Save: JSON array of fabric fields + quantityUsed.
    [BindProperty]
    public string? NewFabricLinksJson { get; set; }

    // Staged NEWLY-added existing-fabric links (not previously linked): JSON {"fabricId": qty, ...}
    [BindProperty]
    public string? AddedFabricLinksJson { get; set; }

    public List<ArticleFabricDto> ExistingFabrics { get; set; } = new();
    public List<FabricResponseDto> Fabrics { get; set; } = new();
    public string ArticleCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public bool CuttingStarted { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var article = await _articleService.GetByIdAsync(Id);
        if (article == null)
            return NotFound();

        Fabrics = await _fabricService.GetAllAsync();
        ExistingFabrics = article.Fabrics;

        ArticleCode = article.ArticleCode;
        OrderDate = article.OrderDate;
        // Cutting "started" = its status is not Pending (InProcess or Done). Detected via the workflow service.
        var statuses = (await _workflowService.GetByArticleAsync(Id)) ?? new List<Application.DTOs.Workflow.ArticleDepartmentStatusDto>();
        CuttingStarted = statuses.Any(s =>
            (s.DepartmentName ?? "").Equals("Cutting", System.StringComparison.OrdinalIgnoreCase)
            && s.Status != "Pending");
        Input.CompanyName = article.CompanyName;
        Input.Color = article.Color;
        Input.DeliveryDate = article.DeliveryDate;
        Input.Season = article.Season;
        Input.PricePerPiece = article.PricePerPiece;
        Input.Quantity = article.Quantity;
        Input.IsPinned = article.IsPinned;
        Input.StitchedBy = article.StitchedBy;
        Input.EmbellishmentEmbroidery = article.EmbellishmentEmbroidery;
        Input.EmbellishmentPrinting = article.EmbellishmentPrinting;
        Input.EmbellishmentHandwork = article.EmbellishmentHandwork;

        AltCodesCsv = string.Join(",", article.AlternateCodes);

        return Page();
    }

    // Single Save handler: builds the complete desired state and submits it as one transaction.
    public async Task<IActionResult> OnPostAsync()
    {
        Fabrics = await _fabricService.GetAllAsync();
        var article = await _articleService.GetByIdAsync(Id);
        if (article == null)
            return NotFound();
        ArticleCode = article.ArticleCode;
        OrderDate = article.OrderDate;

        Input.AlternateCodes = (AltCodesCsv ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        // Desired existing-fabric links: every currently-linked fabric whose id is NOT in the
        // removed list, with the staged quantity, PLUS any newly-added existing-fabric links.
        var removedIds = ParseIds(RemovedFabricIdsCsv ?? "");
        var qtyUpdates = ParseQtyJson(ExistingQtyJson ?? "{}");
        var addedLinks = ParseQtyJson(AddedFabricLinksJson ?? "{}");

        Input.Fabrics = article.Fabrics
            .Where(f => !removedIds.Contains(f.FabricId))
            .Select(f => new ArticleFabricDto
            {
                FabricId = f.FabricId,
                QuantityUsed = qtyUpdates.TryGetValue(f.FabricId, out var q) ? q : f.QuantityUsed
            })
            .ToList();

        // Add staged new existing-fabric links (skip any that are already in the desired list).
        foreach (var kv in addedLinks)
        {
            if (!Input.Fabrics.Any(x => x.FabricId == kv.Key))
                Input.Fabrics.Add(new ArticleFabricDto { FabricId = kv.Key, QuantityUsed = kv.Value });
        }

        // New fabrics to create + link.
        Input.NewFabricLinks = ParseNewFabricLinks(NewFabricLinksJson);

        // Delivery Date must be after today and after the Order Date.
        if (Input.DeliveryDate.Date <= DateTime.Today)
            ModelState.AddModelError(nameof(Input.DeliveryDate), "Delivery date must be after today.");
        else if (Input.DeliveryDate.Date <= OrderDate.Date)
            ModelState.AddModelError(nameof(Input.DeliveryDate), "Delivery date must be after the order date.");

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _articleService.UpdateArticleAsync(Id, Input);
            TempData["SuccessMessage"] = "Article updated successfully.";
            return RedirectToPage("/Admin/ArticleDetail", new { id = Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred: " + ex.Message);
            return Page();
        }
    }

    private static Dictionary<int, decimal> ParseQtyJson(string json)
    {
        var result = new Dictionary<int, decimal>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (int.TryParse(prop.Name, out var fid) && prop.Value.TryGetDecimal(out var qty))
                    result[fid] = qty;
            }
        }
        catch { }
        return result;
    }

    private static List<int> ParseIds(string csv)
    {
        var ids = new List<int>();
        foreach (var part in (csv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id)) ids.Add(id);
        }
        return ids;
    }

    // Parse the staged new-fabric JSON: array of { fabric: { code, invNum, date, type, quantity, unit, rate }, quantityUsed }
    private static List<NewFabricLinkDto> ParseNewFabricLinks(string? json)
    {
        var result = new List<NewFabricLinkDto>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                // The JS sends a nested "fabric" object; fall back to reading fields directly if absent.
                var f = el.TryGetProperty("fabric", out var fab) ? fab : el;

                var fabric = new FabricCreateDto
                {
                    FabricCode = GetStr(f, "code"),
                    InvNum = GetStr(f, "invNum"),
                    FabricDate = GetDate(f, "date"),
                    FabricType = GetStr(f, "type"),
                    Quantity = GetDec(f, "quantity"),
                    Unit = GetStr(f, "unit"),
                    Rate = GetDec(f, "rate")
                };
                var qtyUsed = GetDec(el, "quantityUsed") ?? 0;
                result.Add(new NewFabricLinkDto { Fabric = fabric, QuantityUsed = qtyUsed });
            }
        }
        catch { }
        return result;
    }

    private static string? GetStr(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static decimal? GetDec(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var num)) return num;
        if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), out var s)) return s;
        return null;
    }

    private static DateTime? GetDate(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.TryGetDateTime(out var d) ? d : null;
}
