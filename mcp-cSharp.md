# MCP Server Development in C# - Best Practices Guide

## Overview

This document outlines best practices for implementing Model Context Protocol (MCP) servers using C# and .NET 9, with integration examples using Microsoft Semantic Kernel.

## What is MCP?

Model Context Protocol (MCP) is a standardized protocol that allows AI assistants to securely access and interact with external tools, data sources, and services. MCP servers expose functionality through a well-defined interface that can be consumed by MCP clients.

## Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   MCP Client    │◄──►│   MCP Server    │◄──►│  Business Logic │
│  (AI Assistant) │    │   (Your App)    │    │   & Data Layer  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Core Components

### 1. MCP Server Infrastructure

The MCP server handles protocol communication and function routing:

```csharp
public class McpServer
{
    private readonly ILogger<McpServer> _logger;
    private readonly Kernel _kernel;

    public async Task<McpResponse> HandleMcpRequest(McpRequest request)
    {
        // Handle MCP protocol methods: initialize, tools/list, tools/call
    }
}
```

### 2. Function Plugins with Semantic Kernel

Use Semantic Kernel to define your functions with proper attributes:

```csharp
public class ShippingFunctions
{
    [KernelFunction("GetShipping")]
    [Description("Get shipping information by shipping ID")]
    public async Task<string> GetShipping(
        [Description("The shipping ID to lookup")] int shippingId)
    {
        // Implementation
    }
}
```

### 3. Business Logic Layer

Keep your core business logic separate from MCP concerns:

```csharp
public class ShippingService
{
    public async Task<ShippingInfo?> GetShippingAsync(int shippingId)
    {
        // Core business logic
    }
}
```

## Best Practices

### 1. Project Structure

```
GadgetsInc.Shipping.McpServer/
├── Program.cs                 # Entry point & DI configuration
├── McpServer.cs              # MCP protocol handling
├── ShippingFunctions.cs      # Semantic Kernel functions
├── ShippingService.cs        # Business logic
├── Models/                   # Data models
└── Extensions/               # Extension methods
```

### 2. Dependency Injection Setup

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register services
builder.Services.AddSingleton<McpServer>();
builder.Services.AddSingleton<ShippingService>();
builder.Services.AddTransient<ShippingFunctions>();

var app = builder.Build();
```

### 3. Function Design Principles

#### A. Use Descriptive Attributes

```csharp
[KernelFunction("GetShipping")]
[Description("Get detailed shipping information including tracking, status, and delivery estimates")]
public async Task<string> GetShipping(
    [Description("The unique shipping ID number to lookup")] int shippingId)
```

#### B. Return Structured JSON

```csharp
public async Task<string> GetShipping(int shippingId)
{
    var shipping = await _shippingService.GetShippingAsync(shippingId);
    return JsonSerializer.Serialize(shipping, new JsonSerializerOptions 
    { 
        WriteIndented = true 
    });
}
```

#### C. Implement Proper Error Handling

```csharp
public async Task<string> GetShipping(int shippingId)
{
    try
    {
        var shipping = await _shippingService.GetShippingAsync(shippingId);
        
        if (shipping == null)
        {
            return JsonSerializer.Serialize(new { 
                error = "Shipping ID not found",
                shippingId 
            });
        }
        
        return JsonSerializer.Serialize(shipping);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving shipping info for ID: {ShippingId}", shippingId);
        return JsonSerializer.Serialize(new { 
            error = "Internal server error" 
        });
    }
}
```

### 4. Protocol Implementation

#### A. Handle Core MCP Methods

```csharp
public async Task<McpResponse> HandleMcpRequest(McpRequest request)
{
    return request.Method switch
    {
        "initialize" => HandleInitialize(request),
        "tools/list" => HandleToolsList(),
        "tools/call" => await HandleToolCall(request),
        _ => new McpResponse
        {
            Id = request.Id,
            Error = new McpError
            {
                Code = -32601,
                Message = $"Method not found: {request.Method}"
            }
        }
    };
}
```

#### B. Provide Rich Tool Metadata

```csharp
private McpResponse HandleToolsList()
{
    var tools = new object[]
    {
        new
        {
            name = "GetShipping",
            description = "Get shipping information by shipping ID",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    shippingId = new
                    {
                        type = "integer",
                        description = "The shipping ID to lookup",
                        minimum = 1
                    }
                },
                required = new[] { "shippingId" }
            }
        }
    };

    return new McpResponse { Result = new { tools } };
}
```

### 5. Data Modeling

#### A. Use Strongly Typed Models

```csharp
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
```

#### B. Use Nullable Types Appropriately

```csharp
public class PackageInfo
{
    public string PackageId { get; set; } = string.Empty;
    public List<string> Contents { get; set; } = new();
    public decimal? Insurance { get; set; }  // Optional field
}
```

### 6. Logging and Monitoring

#### A. Structured Logging

```csharp
_logger.LogInformation("Getting shipping info for ID: {ShippingId}", shippingId);
_logger.LogWarning("Shipping ID {ShippingId} not found", shippingId);
_logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
```

#### B. Performance Tracking

```csharp
using var activity = ActivitySource.StartActivity("GetShipping");
activity?.SetTag("shipping.id", shippingId);

var stopwatch = Stopwatch.StartNew();
var result = await _shippingService.GetShippingAsync(shippingId);
stopwatch.Stop();

_logger.LogInformation("GetShipping completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
```

### 7. Testing

#### A. Unit Testing Functions

```csharp
[Test]
public async Task GetShipping_ValidId_ReturnsShippingInfo()
{
    // Arrange
    var mockService = new Mock<IShippingService>();
    var expectedShipping = new ShippingInfo { ShippingId = 123 };
    mockService.Setup(s => s.GetShippingAsync(123))
               .ReturnsAsync(expectedShipping);
    
    var functions = new ShippingFunctions(mockService.Object);
    
    // Act
    var result = await functions.GetShipping(123);
    
    // Assert
    var shipping = JsonSerializer.Deserialize<ShippingInfo>(result);
    Assert.AreEqual(123, shipping.ShippingId);
}
```

#### B. Integration Testing

```csharp
[Test]
public async Task McpServer_HandleToolCall_ExecutesCorrectly()
{
    // Test the full MCP protocol flow
    var request = new McpRequest
    {
        Method = "tools/call",
        Params = new ToolCallRequest 
        { 
            Name = "GetShipping", 
            Arguments = new Dictionary<string, object> { ["shippingId"] = 123 }
        }
    };
    
    var response = await _mcpServer.HandleMcpRequest(request);
    
    Assert.IsNotNull(response.Result);
    Assert.IsNull(response.Error);
}
```

## Security Considerations

### 1. Input Validation

```csharp
public async Task<string> GetShipping(int shippingId)
{
    if (shippingId <= 0)
    {
        return JsonSerializer.Serialize(new { 
            error = "Shipping ID must be a positive integer" 
        });
    }
    
    // Continue processing...
}
```

### 2. Rate Limiting

```csharp
[RateLimiting("shipping-api")]
public async Task<string> GetShipping(int shippingId)
{
    // Implementation
}
```

### 3. Authentication (if needed)

```csharp
public class McpServer
{
    private async Task<bool> ValidateApiKey(string apiKey)
    {
        // Validate the API key
        return await _authService.ValidateKeyAsync(apiKey);
    }
}
```

## Performance Optimization

### 1. Async/Await Patterns

```csharp
public async Task<SearchResults> SearchAsync(string searchTerm)
{
    var shipmentTask = SearchShipmentsAsync(searchTerm);
    var packageTask = SearchPackagesAsync(searchTerm);
    
    await Task.WhenAll(shipmentTask, packageTask);
    
    return new SearchResults
    {
        Shipments = await shipmentTask,
        Packages = await packageTask
    };
}
```

### 2. Caching

```csharp
[Cached(Duration = 300)] // 5 minutes
public async Task<string> GetShipping(int shippingId)
{
    // Expensive operation
}
```

### 3. Connection Pooling

```csharp
builder.Services.AddDbContext<ShippingDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.CommandTimeout(30)));
```

## Recommended NuGet Packages

```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.65.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.9" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.9" />
<PackageReference Include="System.Text.Json" Version="9.0.9" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.9" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.9" />
```

## Alternative Frameworks

While this guide focuses on Semantic Kernel, consider these alternatives:

1. **Direct Protocol Implementation** - For minimal overhead
2. **ASP.NET Core Minimal APIs** - For HTTP-based MCP servers
3. **Orleans** - For distributed scenarios
4. **MediatR** - For command/query separation

## Deployment Considerations

### 1. Container Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["GadgetsInc.Shipping.McpServer.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GadgetsInc.Shipping.McpServer.dll"]
```

### 2. Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<ShippingHealthCheck>("shipping")
    .AddDbContextCheck<ShippingDbContext>();
```

## Conclusion

Building MCP servers in C# with .NET 9 and Semantic Kernel provides a robust, scalable foundation for creating AI-powered integrations. Key success factors include:

- Clean separation of concerns
- Comprehensive error handling
- Rich function metadata
- Thorough testing
- Security-first design
- Performance optimization

This approach enables you to build production-ready MCP servers that can reliably serve AI assistants while maintaining the flexibility to evolve with changing requirements.