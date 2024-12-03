using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.AI.OpenAI;
using ChatApp.Services;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Add OpenAI service
builder.Services.AddScoped<IOpenAIService, OpenAIService>();

// Register CosmosDB service
builder.Services.AddSingleton<ICosmosDbService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new CosmosDbService(configuration);
});

// Register Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // You can specify a specific domain instead of AllowAnyOrigin for security
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// app.UseCors(); // Use the configured CORS policy

// Use CORS policy
app.UseCors("AllowAll");

app.MapControllers(); // Map controller routes

app.Run();
