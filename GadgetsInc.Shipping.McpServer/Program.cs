using GadgetsInc.Shipping.McpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register services
builder.Services.AddSingleton<McpServer>();
builder.Services.AddSingleton<ShippingService>();

// Register Semantic Kernel functions
builder.Services.AddTransient<ShippingFunctions>();

var app = builder.Build();

// Get the MCP server and start it
var mcpServer = app.Services.GetRequiredService<McpServer>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting GadgetsInc Shipping MCP Server...");

await mcpServer.StartAsync();

logger.LogInformation("MCP Server started successfully. Listening for requests...");

// Keep the application running
await app.RunAsync();
