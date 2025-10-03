using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace GadgetsInc.Shipping.McpServer;

public class ShippingFunctions
{
    private readonly ShippingService _shippingService;

    public ShippingFunctions(ShippingService shippingService)
    {
        _shippingService = shippingService;
    }

    [KernelFunction("GetShipping")]
    [Description("Get shipping information by shipping ID")]
    public async Task<string> GetShipping(
        [Description("The shipping ID to lookup")] int shippingId)
    {
        var shipping = await _shippingService.GetShippingAsync(shippingId);
        return JsonSerializer.Serialize(shipping, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction("GetPackage")]
    [Description("Get package information by package ID")]
    public async Task<string> GetPackage(
        [Description("The package ID to lookup")] string packageId)
    {
        var package = await _shippingService.GetPackageAsync(packageId);
        return JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction("Search")]
    [Description("Search for packages and shipments")]
    public async Task<string> Search(
        [Description("The search term to use")] string searchTerm)
    {
        var results = await _shippingService.SearchAsync(searchTerm);
        return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
    }
}