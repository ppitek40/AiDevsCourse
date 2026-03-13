using AiDevs.Core.Interfaces;
using AiDevs.Infrastructure.Services;
using AiDevs.Solutions.Task01;
using AiDevs.Solutions.Task02;
using AiDevs.Solutions.Task03;
using AiDevs.Solutions.Task04;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register HttpClient for OpenRouter and general use
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>();
builder.Services.AddHttpClient<IAiDevsApiService, AiDevsApiService>();
builder.Services.AddHttpClient();

builder.Services.AddTransient<IAgentSessionService, AgentSessionService>();
builder.Services.AddTransient<IToolsService, ToolsService>();
builder.Services.AddTransient<ILlmExecutionService, LlmExecutionService>();

// Register Task03 services
builder.Services.AddSingleton<IConversationMemoryService, ConversationMemoryService>();
builder.Services.AddSingleton<IProxyEventAggregator, ProxyEventAggregator>();

// Register function handlers for Task02
builder.Services.AddTransient<GetPersonLocationsFunction>();
builder.Services.AddTransient<GetAccessLevelFunction>();

// Register function handlers for Task03
builder.Services.AddTransient<CheckPackageFunction>();
builder.Services.AddTransient<RedirectPackageFunction>();

// Register function handlers for Task04
builder.Services.AddTransient<FetchDocumentFunction>();

// Register all task solutions
builder.Services.AddTransient<ITaskSolution, Task01Solution>();
builder.Services.AddTransient<ITaskSolution, Task02Solution>();
builder.Services.AddTransient<ITaskSolution, Task03Solution>();
builder.Services.AddTransient<ITaskSolution, Task04Solution>();
// Add more task solutions here as you implement them:
// ... etc

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");
app.UseAuthorization();
app.MapControllers();

app.Run();
