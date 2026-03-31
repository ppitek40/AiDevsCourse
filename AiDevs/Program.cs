using AiDevs.Core.Interfaces;
using AiDevs.Infrastructure.Services;
using AiDevs.Solutions.Task01;
using AiDevs.Solutions.Task02;
using AiDevs.Solutions.Task03;
using AiDevs.Solutions.Task04;
using AiDevs.Solutions.Task05;
using AiDevs.Solutions.Task06;
using AiDevs.Solutions.Task07;
using AiDevs.Solutions.Task08;
using AiDevs.Solutions.Task09;
using AiDevs.Solutions.Task10;
using AiDevs.Solutions.Task11;
using AiDevs.Solutions.Task12;
using AiDevs.Solutions.Task13;
using AiDevs.Solutions.Task14;
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

// Register Task03 services
builder.Services.AddSingleton<IConversationMemoryService, ConversationMemoryService>();
builder.Services.AddSingleton<IProxyEventAggregator, ProxyEventAggregator>();

// Register function handlers for Task02
builder.Services.AddTransient<GetPersonLocationsFunction>();
builder.Services.AddTransient<GetAccessLevelFunction>();
builder.Services.AddTransient<GetCoordinatesOfTheCityFunction>();

// Register function handlers for Task03
builder.Services.AddTransient<CheckPackageFunction>();
builder.Services.AddTransient<RedirectPackageFunction>();

// Register function handlers for Task04
builder.Services.AddTransient<FetchDocumentFunction>();

// Register function handlers for Task05
builder.Services.AddTransient<RailwayApiFunction>();

// Register function handlers for Task06
builder.Services.AddTransient<VerifyPromptFunction>();

// Register function handlers for Task07
builder.Services.AddTransient<RotateFunction>();
builder.Services.AddTransient<GetElectricityDiagramFunction>();

// Register function handlers for Task08
builder.Services.AddTransient<LogReaderFunction>();
builder.Services.AddTransient<SendLogsFunction>();
builder.Services.AddTransient<LoadLogChunkFunction>();
builder.Services.AddTransient<TokenCounterFunction>();

// Register function handlers for Task09
builder.Services.AddTransient<SearchEmailFunction>();
builder.Services.AddTransient<MailboxVerifyFunction>();

// Register function handlers for Task10
builder.Services.AddTransient<ReadDroneImageFunction>();
builder.Services.AddTransient<SendDroneInstructionsFunction>();

// Register function handlers for Task11
builder.Services.AddTransient<IOperatorNotesClassifier, OperatorNotesClassifier>();
builder.Services.AddTransient<ISensorAnomalies, SensorAnomalies>();

// Register function handlers for Task12
builder.Services.AddTransient<ExecuteCommandFunction>();

// Register function handlers for Task13
builder.Services.AddTransient<SendCommandFunction>();

// Register function handlers for Task14
builder.Services.AddSingleton<IItemCityDataService, ItemCityDataService>();
builder.Services.AddTransient<GetCitiesForItemCode>();
builder.Services.AddTransient<SearchItemsFunction>();

// Register all task solutions
builder.Services.AddTransient<ITaskSolution, Task01Solution>();
builder.Services.AddTransient<ITaskSolution, Task02Solution>();
builder.Services.AddTransient<ITaskSolution, Task03Solution>();
builder.Services.AddTransient<ITaskSolution, Task04Solution>();
builder.Services.AddTransient<ITaskSolution, Task05Solution>();
builder.Services.AddTransient<ITaskSolution, Task06Solution>();
builder.Services.AddTransient<ITaskSolution, Task07Solution>();
builder.Services.AddTransient<ITaskSolution, Task08Solution>();
builder.Services.AddTransient<ITaskSolution, Task09Solution>();
builder.Services.AddTransient<ITaskSolution, Task10Solution>();
builder.Services.AddTransient<ITaskSolution, Task11Solution>();
builder.Services.AddTransient<ITaskSolution, Task12Solution>();
builder.Services.AddTransient<ITaskSolution, Task13Solution>();
builder.Services.AddTransient<ITaskSolution, Task14Solution>();

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
