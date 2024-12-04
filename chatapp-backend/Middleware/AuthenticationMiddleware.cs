using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _tenantName;
        private readonly string _policyName;
        private readonly string _clientId;

        public AuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _tenantName = configuration["AzureB2C:TenantName"];
            _policyName = configuration["AzureB2C:PolicyName"];
            _clientId = configuration["AzureB2C:ClientId"];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("No token provided");
                return;
            }

            try
            {
                // Fetch OpenID Connect configuration dynamically
                var metadataAddress = $"https://{_tenantName}.b2clogin.com/{_tenantName}.onmicrosoft.com/v2.0/.well-known/openid-configuration?p={_policyName}";
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataAddress,
                    new OpenIdConnectConfigurationRetriever());
                var openIdConfig = await configManager.GetConfigurationAsync(CancellationToken.None);

                // Set up token validation parameters
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = openIdConfig.Issuer, // Use issuer from metadata
                    ValidateAudience = true,
                    ValidAudience = _clientId,
                    ValidateLifetime = true,
                    IssuerSigningKeys = openIdConfig.SigningKeys, // Use signing keys from metadata
                };

                // Validate the token
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Show items in principal
                foreach (var claim in principal.Claims)
                {
                    Console.WriteLine($"Claim: {claim.Type} - {claim.Value}");
                }

                // Add userId to context
                context.Items["UserId"] = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

                // set UserIds as a array in context.Items if it doesn't exist, else add to array
                if (context.Items["UserIds"] == null)
                {
                    context.Items["UserIds"] = new string[] { context.Items["UserId"]?.ToString() };
                }
                else
                {
                    var userIds = (string[])context.Items["UserIds"];
                    userIds = userIds.Append(context.Items["UserId"]?.ToString()).ToArray();
                    context.Items["UserIds"] = userIds;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync($"Invalid token: {ex.Message}");
            }
        }
    }
}
