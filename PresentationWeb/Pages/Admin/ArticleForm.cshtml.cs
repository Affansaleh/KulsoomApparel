using Application.DTOs.Article;
using Application.DTOs.Fabric;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ArticleFormModel : PageModel
{
    private readonly IArticleService _articleService;
    private readonly IFabricService _fabricService;
    private readonly IDepartmentRepository _departmentRepository;

    public ArticleFormModel(IArticleService articleService, IFabricService fabricService, IDepartmentRepository departmentRepository)
    {
        _articleService = articleService;
        _fabricService = fabricService;
        _departmentRepository = departmentRepository;
    }

    public List<FabricResponseDto> Fabrics { get; set; } = new();
    public List<Department> Departments { get; set; } = new();

    [BindProperty]
    public ArticleCreateDto Input { get; set; } = new();

    // Staged new-fabric creations (JSON array of fabric fields + quantityUsed).
    [BindProperty]
    public string? NewFabricLinksJson { get; set; }

    // Staged existing-fabric links (JSON {"fabricId": qty}).
    [BindProperty]
    public string? FabricLinksJson { get; set; }

    public async Task OnGetAsync()
    {
        Fabrics = await _fabricService.GetAllAsync();
        Departments = await _departmentRepository.GetAllAsync();

        // Order Date defaults to today on the add form; user can change it.
        if (Input.OrderDate == default)
            Input.OrderDate = DateTime.Today;
        // Delivery Date defaults to today too; user must set it in the future (validated below).
        if (Input.DeliveryDate == default)
            Input.DeliveryDate = DateTime.Today;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Fabrics = await _fabricService.GetAllAsync();
        Departments = await _departmentRepository.GetAllAsync();

        // Build staged fabrics + new fabrics into the DTO.
        Input.Fabrics = ParseQtyJson(FabricLinksJson ?? "{}")
            .Select(kv => new ArticleFabricDto { FabricId = kv.Key, QuantityUsed = kv.Value })
            .ToList();
        Input.NewFabricLinks = ParseNewFabricLinks(NewFabricLinksJson);

        // Quantity: optional, but if provided it must be > 0.
        if (Input.Quantity.HasValue && Input.Quantity.Value <= 0)
            ModelState.AddModelError(nameof(Input.Quantity), "Quantity must be greater than zero if provided.");

        // Delivery Date must be after today and after the Order Date.
        if (Input.DeliveryDate.Date <= DateTime.Today)
            ModelState.AddModelError(nameof(Input.DeliveryDate), "Delivery date must be after today.");
        else if (Input.DeliveryDate.Date <= Input.OrderDate.Date)
            ModelState.AddModelError(nameof(Input.DeliveryDate), "Delivery date must be after the order date.");

        if (!ModelState.IsValid)
            return Page();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = int.TryParse(userIdClaim, out var id) ? id : 0;

        try
        {
            var created = await _articleService.CreateAsync(Input, userId);
            TempData["SuccessMessage"] = "Article created successfully.";
            return RedirectToPage("/Admin/ArticleDetail", new { id = created.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    // Inline "Add New Fabric" — creates a new fabric (stock NOT deducted here; the
    // deduction happens once when the article is saved via CreateAsync → DeductForArticleAsync).
    public async Task<IActionResult> OnPostAddFabricAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFabric.FabricCode) || string.IsNullOrWhiteSpace(NewFabric.FabricType))
            return new JsonResult(new { success = false, message = "Fabric code and type are required." });

        try
        {
            var created = await _fabricService.CreateAsync(NewFabric);
            return new JsonResult(new
            {
                success = true,
                id = created.Id,
                fabricCode = created.FabricCode,
                unit = created.Unit,
                availableQuantity = created.AvailableQuantity
            });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    private static Dictionary<int, decimal> ParseQtyJson(string json)
    {
        var result = new Dictionary<int, decimal>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (int.TryParse(prop.Name, out var fid) && prop.Value.TryGetDecimal(out var qty))
                    result[fid] = qty;
            }
        }
        catch { }
        return result;
    }

    private static List<NewFabricLinkDto> ParseNewFabricLinks(string? json)
    {
        var result = new List<NewFabricLinkDto>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
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

    private static string? GetStr(System.Text.Json.JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.ValueKind == System.Text.Json.JsonValueKind.String ? p.GetString() : null;

    private static decimal? GetDec(System.Text.Json.JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return null;
        if (p.ValueKind == System.Text.Json.JsonValueKind.Number && p.TryGetDecimal(out var num)) return num;
        if (p.ValueKind == System.Text.Json.JsonValueKind.String && decimal.TryParse(p.GetString(), out var s)) return s;
        return null;
    }

    private static DateTime? GetDate(System.Text.Json.JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.TryGetDateTime(out var d) ? d : null;

    [BindProperty]
    public FabricCreateDto NewFabric { get; set; } = new();
}
