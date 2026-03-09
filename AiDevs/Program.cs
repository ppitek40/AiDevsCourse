using AiDevs.Core.Interfaces;
using AiDevs.Infrastructure.Services;
using AiDevs.Solutions.Task01;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HttpClient for OpenRouter
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
