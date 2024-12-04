using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _tenantName = "LucasChatApp"; // Azure B2C tenant name
        private readonly string _policyName = "B2C_1_chatflow"; // B2C user flow/policy name
        private readonly string _clientId = "5d52d5ac-a767-4449-9270-deb5a0c3a961"; // Application ID

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
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

                /*
                Claim: exp - 1733302461
                Claim: nbf - 1733298861
                Claim: ver - 1.0
                Claim: iss - https://lucaschatapp.b2clogin.com/abb758de-a690-45be-b2c0-06a712c73151/v2.0/
                Claim: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier - 1d00c458-02d5-4adf-a07a-b5901b523115
                Claim: aud - 5d52d5ac-a767-4449-9270-deb5a0c3a961
                Claim: nonce - defaultNonce
                Claim: iat - 1733298861
                Claim: auth_time - 1733298861
                Claim: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname - lucas
                Claim: name - unknown
                Claim: emails - driessenslucas@gmail.com
                Claim: tfp - B2C_1_chatflow
                */

                // Add claims to context for use in controllers
                // context.Items["UserId"] = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

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
