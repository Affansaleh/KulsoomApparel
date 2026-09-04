using Application.DTOs.Fabric;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationWeb.Pages.Admin;

[Authorize(Roles = "Admin")]
public class FabricsModel : PageModel
{
    private readonly IFabricService _fabricService;

    public FabricsModel(IFabricService fabricService)
    {
        _fabricService = fabricService;
    }

    public List<FabricResponseDto> AllFabrics { get; set; } = new();
    public List<FabricResponseDto> InStockFabrics { get; set; } = new();
    public decimal TotalAvailableStockValue { get; set; }

    [BindProperty]
    public FabricCreateDto AddFabric { get; set; } = new();

    [BindProperty]
    public FabricTopUpDto TopUp { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadFabricsAsync();
    }

    private async Task LoadFabricsAsync()
    {
        AllFabrics = await _fabricService.GetAllAsync();
        InStockFabrics = AllFabrics.Where(f => f.Status == "InStock").ToList();
        TotalAvailableStockValue = AllFabrics.Sum(f => f.AvailableQuantity * f.Rate);
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadFabricsAsync();
            return RedirectToPage("/Admin/FabricForm");
        }

        try
        {
            await _fabricService.CreateAsync(AddFabric);
            TempData["SuccessMessage"] = $"Fabric '{AddFabric.FabricCode}' created successfully.";
            return RedirectToPage("/Admin/FabricForm");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadFabricsAsync();
            return RedirectToPage("/Admin/FabricForm");
        }
    }

    public async Task<IActionResult> OnPostTopUpAsync()
    {
        if (TopUp.AddedQuantity <= 0)
        {
            await LoadFabricsAsync();
            ModelState.AddModelError(string.Empty, "Additional quantity must be greater than zero.");
            return RedirectToPage("/Admin/FabricForm");
        }

        try
        {
            TopUp.TopUpDate = DateTime.UtcNow;
            await _fabricService.TopUpAsync(TopUp);
            TempData["SuccessMessage"] = "Fabric topped up successfully.";
            return RedirectToPage("/Admin/FabricForm");
        }
        catch (InvalidOperationException ex)
        {
            await LoadFabricsAsync();
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToPage("/Admin/FabricForm");
        }
    }
}
