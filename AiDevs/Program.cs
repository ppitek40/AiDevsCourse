using AiDevs.Core.Interfaces;
using AiDevs.Infrastructure.Services;
using AiDevs.Solutions.Task01;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register HttpClient for OpenRouter and general use
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>();
builder.Services.AddHttpClient();

// Register all task solutions
builder.Services.AddTransient<ITaskSolution, Task01Solution>();
// Add more task solutions here as you implement them:
// builder.Services.AddTransient<ITaskSolution, Task02Solution>();
// builder.Services.AddTransient<ITaskSolution, Task03Solution>();
// ... etc

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
