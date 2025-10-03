using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GadgetsInc.Shipping.McpServer;

public class McpServer
{
    private readonly ILogger<McpServer> _logger;
    private readonly Kernel _kernel;
    private readonly ShippingFunctions _shippingFunctions;

    public McpServer(ILogger<McpServer> logger, ShippingFunctions shippingFunctions)
    {
        _logger = logger;
        _shippingFunctions = shippingFunctions;
        
        // Create kernel with Semantic Kernel
        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(configure => configure.AddConsole());
        
        // Register the shipping functions as plugins
        builder.Plugins.AddFromObject(_shippingFunctions, "ShippingPlugin");
        
        _kernel = builder.Build();
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("MCP Server initialized with Semantic Kernel");
        
        // In a real MCP server, this would listen on stdio or a network port
        // For this example, we'll demonstrate the capability with test calls
        await DemonstrateCapabilities();
    }

    private async Task DemonstrateCapabilities()
    {
        _logger.LogInformation("Demonstrating MCP Server capabilities...");

        try
        {
            // Test GetShipping function
            var getShippingResult = await _kernel.InvokeAsync("ShippingPlugin", "GetShipping", 
                new KernelArguments { ["shippingId"] = 12345 });
            _logger.LogInformation("GetShipping(12345): {Result}", getShippingResult.GetValue<string>());

            // Test GetPackage function
            var getPackageResult = await _kernel.InvokeAsync("ShippingPlugin", "GetPackage", 
                new KernelArguments { ["packageId"] = "PKG001" });
            _logger.LogInformation("GetPackage(PKG001): {Result}", getPackageResult.GetValue<string>());

            // Test Search function
            var searchResult = await _kernel.InvokeAsync("ShippingPlugin", "Search", 
                new KernelArguments { ["searchTerm"] = "electronics" });
            _logger.LogInformation("Search(electronics): {Result}", searchResult.GetValue<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demonstrating capabilities");
        }
    }

    public async Task<McpResponse> HandleMcpRequest(McpRequest request)
    {
        try
        {
            _logger.LogInformation("Handling MCP request: {Method}", request.Method);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = "Internal error"
                }
            };
        }
    }

    private McpResponse HandleInitialize(McpRequest request)
    {
        return new McpResponse
        {
            Id = request.Id,
            Result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    tools = new { }
                },
                serverInfo = new
                {
                    name = "GadgetsInc.Shipping.McpServer",
                    version = "1.0.0"
                }
            }
        };
    }

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
                            description = "The shipping ID to lookup"
                        }
                    },
                    required = new[] { "shippingId" }
                }
            },
            new
            {
                name = "GetPackage",
                description = "Get package information by package ID",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        packageId = new
                        {
                            type = "string",
                            description = "The package ID to lookup"
                        }
                    },
                    required = new[] { "packageId" }
                }
            },
            new
            {
                name = "Search",
                description = "Search for packages and shipments",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        searchTerm = new
                        {
                            type = "string",
                            description = "The search term to use"
                        }
                    },
                    required = new[] { "searchTerm" }
                }
            }
        };

        return new McpResponse
        {
            Result = new { tools }
        };
    }

    private async Task<McpResponse> HandleToolCall(McpRequest request)
    {
        var toolCall = JsonSerializer.Deserialize<ToolCallRequest>(request.Params?.ToString() ?? "{}");
        
        if (toolCall == null || string.IsNullOrEmpty(toolCall.Name))
        {
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32602,
                    Message = "Invalid tool call parameters"
                }
            };
        }

        try
        {
            var kernelArguments = new KernelArguments();
            
            if (toolCall.Arguments != null)
            {
                foreach (var arg in toolCall.Arguments)
                {
                    kernelArguments[arg.Key] = arg.Value;
                }
            }

            var result = await _kernel.InvokeAsync("ShippingPlugin", toolCall.Name, kernelArguments);
            
            return new McpResponse
            {
                Id = request.Id,
                Result = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = result.GetValue<string>()
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolCall.Name);
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = $"Error executing tool: {ex.Message}"
                }
            };
        }
    }
}

// MCP Protocol Models
public class McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object? Id { get; set; }
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object? Id { get; set; }
    
    [JsonPropertyName("result")]
    public object? Result { get; set; }
    
    [JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

public class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

public class ToolCallRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; set; }
}