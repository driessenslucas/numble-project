// using Microsoft.AspNetCore.Builder;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Configuration;
// using Azure.Identity;
// using Azure.Security.KeyVault.Secrets;
// using Azure.AI.OpenAI;
// using ChatApp.Controllers;
// using ChatApp.Models;
// using ChatApp.Services;
// using chatapp.Services;

// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
// builder.Services.AddControllers();

// builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
// builder.Services.AddScoped<IOpenAIService, OpenAIService>();

// // Register Swagger
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();


// builder.services.AddSingleton<ICosmosDbService>(sp =>
// {
//     var cosmosClient = new CosmosClient(Configuration["CosmosDb:ConnectionString"]);
//     return new CosmosDbService(cosmosClient, "ChatDatabase", "ChatContainer");
// });

// // Add CORS if needed
// builder.Services.AddCors(options =>
// {
//     options.AddDefaultPolicy(policy =>
//     {
//         policy.AllowAnyOrigin()
//               .AllowAnyMethod()
//               .AllowAnyHeader();
//     });
// });

// var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();
// app.UseCors(); // Use CORS policy

// app.MapControllers(); // Map controller routes

// app.Run();

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

// Register Cosmos DB service
builder.Services.AddSingleton<ICosmosDbService>(sp =>
{
    var cosmosClient = new CosmosClient(builder.Configuration["CosmosDb:ConnectionString"]);
    return new CosmosDbService(cosmosClient, "ChatDatabase", "ChatContainer");
});

// Add authentication for Azure B2C
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

builder.Services.AddAuthorization(); // Add authorization policies if needed

// Register Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
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

app.UseCors(); // Use the configured CORS policy

app.UseAuthentication(); // Add authentication middleware
app.UseAuthorization(); // Add authorization middleware

app.MapControllers(); // Map controller routes

app.Run();
