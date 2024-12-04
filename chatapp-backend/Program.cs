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
using ChatApp.Middleware;

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

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .WithOrigins("http://localhost:5500", "https://localhost:5500")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://LucasChatApp.b2clogin.com/LucasChatApp.onmicrosoft.com/v2.0/";
        options.Audience = "5d52d5ac-a767-4449-9270-deb5a0c3a961"; 
        options.RequireHttpsMetadata = true; 
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS before other middleware
app.UseCors("AllowAll");

// Authentication & Authorization
app.UseMiddleware<AuthenticationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
