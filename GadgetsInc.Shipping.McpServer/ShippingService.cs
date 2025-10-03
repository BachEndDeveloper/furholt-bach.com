using Microsoft.Extensions.Logging;

namespace GadgetsInc.Shipping.McpServer;

public class ShippingService
{
    private readonly ILogger<ShippingService> _logger;
    private readonly List<ShippingInfo> _shippingData;
    private readonly List<PackageInfo> _packageData;

    public ShippingService(ILogger<ShippingService> logger)
    {
        _logger = logger;
        _shippingData = InitializeShippingData();
        _packageData = InitializePackageData();
    }

    public async Task<ShippingInfo?> GetShippingAsync(int shippingId)
    {
        _logger.LogInformation("Getting shipping info for ID: {ShippingId}", shippingId);
        
        // Simulate async operation
        await Task.Delay(50);
        
        var shipping = _shippingData.FirstOrDefault(s => s.ShippingId == shippingId);
        
        if (shipping == null)
        {
            _logger.LogWarning("Shipping ID {ShippingId} not found", shippingId);
            return null;
        }

        return shipping;
    }

    public async Task<PackageInfo?> GetPackageAsync(string packageId)
    {
        _logger.LogInformation("Getting package info for ID: {PackageId}", packageId);
        
        // Simulate async operation
        await Task.Delay(50);
        
        var package = _packageData.FirstOrDefault(p => p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
        
        if (package == null)
        {
            _logger.LogWarning("Package ID {PackageId} not found", packageId);
            return null;
        }

        return package;
    }

    public async Task<SearchResults> SearchAsync(string searchTerm)
    {
        _logger.LogInformation("Searching for: {SearchTerm}", searchTerm);
        
        // Simulate async operation
        await Task.Delay(100);
        
        var searchResults = new SearchResults();
        
        // Search in shipping data
        var matchingShipments = _shippingData.Where(s =>
            s.Destination.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            s.Origin.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            s.Carrier.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            s.Status.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        // Search in package data
        var matchingPackages = _packageData.Where(p =>
            p.PackageId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            p.Contents.Any(c => c.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        searchResults.Shipments = matchingShipments;
        searchResults.Packages = matchingPackages;
        searchResults.TotalResults = matchingShipments.Count + matchingPackages.Count;

        _logger.LogInformation("Found {Count} results for search term: {SearchTerm}", searchResults.TotalResults, searchTerm);
        
        return searchResults;
    }

    private List<ShippingInfo> InitializeShippingData()
    {
        return new List<ShippingInfo>
        {
            new()
            {
                ShippingId = 12345,
                TrackingNumber = "TRK001234567",
                Origin = "New York, NY",
                Destination = "Los Angeles, CA",
                Carrier = "FedEx",
                Status = "In Transit",
                EstimatedDelivery = DateTime.Now.AddDays(2),
                ActualDelivery = null,
                Weight = 2.5,
                Dimensions = "12x8x6 inches"
            },
            new()
            {
                ShippingId = 12346,
                TrackingNumber = "TRK001234568",
                Origin = "Chicago, IL",
                Destination = "Miami, FL",
                Carrier = "UPS",
                Status = "Delivered",
                EstimatedDelivery = DateTime.Now.AddDays(-1),
                ActualDelivery = DateTime.Now.AddDays(-1),
                Weight = 1.2,
                Dimensions = "8x6x4 inches"
            },
            new()
            {
                ShippingId = 12347,
                TrackingNumber = "TRK001234569",
                Origin = "Seattle, WA",
                Destination = "Boston, MA",
                Carrier = "DHL",
                Status = "Processing",
                EstimatedDelivery = DateTime.Now.AddDays(3),
                ActualDelivery = null,
                Weight = 5.8,
                Dimensions = "16x12x10 inches"
            }
        };
    }

    private List<PackageInfo> InitializePackageData()
    {
        return new List<PackageInfo>
        {
            new()
            {
                PackageId = "PKG001",
                ShippingId = 12345,
                Description = "Electronics Package",
                Contents = new List<string> { "Laptop Computer", "Power Adapter", "Mouse" },
                Value = 1200.00m,
                Weight = 2.5,
                Fragile = true,
                Insurance = 1500.00m
            },
            new()
            {
                PackageId = "PKG002",
                ShippingId = 12346,
                Description = "Clothing Package",
                Contents = new List<string> { "T-Shirts", "Jeans", "Sneakers" },
                Value = 150.00m,
                Weight = 1.2,
                Fragile = false,
                Insurance = 200.00m
            },
            new()
            {
                PackageId = "PKG003",
                ShippingId = 12347,
                Description = "Books and Documents",
                Contents = new List<string> { "Technical Manuals", "Certificates", "Software Licenses" },
                Value = 500.00m,
                Weight = 5.8,
                Fragile = false,
                Insurance = 600.00m
            }
        };
    }
}

// Data Models
public class ShippingInfo
{
    public int ShippingId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EstimatedDelivery { get; set; }
    public DateTime? ActualDelivery { get; set; }
    public double Weight { get; set; }
    public string Dimensions { get; set; } = string.Empty;
}

public class PackageInfo
{
    public string PackageId { get; set; } = string.Empty;
    public int ShippingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Contents { get; set; } = new();
    public decimal Value { get; set; }
    public double Weight { get; set; }
    public bool Fragile { get; set; }
    public decimal Insurance { get; set; }
}

public class SearchResults
{
    public List<ShippingInfo> Shipments { get; set; } = new();
    public List<PackageInfo> Packages { get; set; } = new();
    public int TotalResults { get; set; }
}