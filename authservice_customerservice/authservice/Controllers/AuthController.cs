using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

internal record LoginResponse(Guid UserGuid, string Username, string Email);

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ILogger<AuthController> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration config) : ControllerBase
{
    [HttpGet("test1")]
    public IActionResult Test1() => Ok("Hello from AuthController");

    [HttpPost("createtoken/method1")]
    public async Task<IActionResult> CreateTokenMethod1([FromBody] CustomerDTO customerDto)
    {
        var customerServiceUrl = config["CustomerServiceUrl"]
            ?? throw new InvalidOperationException("CustomerServiceUrl is not configured.");

        var httpClient = httpClientFactory.CreateClient();

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync($"{customerServiceUrl}/api/customers/login", customerDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reach CustomerService at {CustomerServiceUrl}.", customerServiceUrl);
            return StatusCode(503, "CustomerService is unavailable.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Login failed for {Email} — CustomerService returned {StatusCode}.",
                customerDto.Email, (int)response.StatusCode);
            return Unauthorized();
        }

        var userInfo = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (userInfo == null)
        {
            logger.LogError("CustomerService returned success but response body was empty for {Email}.", customerDto.Email);
            return StatusCode(500, "Unexpected response from CustomerService.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userInfo.Username),
            new(ClaimTypes.Email, userInfo.Email),
            new("UserGuid", userInfo.UserGuid.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(3),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        logger.LogInformation("JWT issued for {Email}.", customerDto.Email);
        return Ok(tokenString);
    }

    // Test endpoint: manually validates a Bearer token without relying on [Authorize].
    // Useful for teaching how JWT validation works under the hood.
    [HttpGet("testbasicauth")]
    public IActionResult TestBasicAuth()
    {
        if (IsRequestAuthenticated(out _))
            return Ok("YES - Authenticated!");

        return Unauthorized("NO - NOT Authenticated");
    }

    private bool IsRequestAuthenticated(out ClaimsPrincipal? principal)
    {
        principal = null;

        try
        {
            var authHeader = Request.Headers.Authorization.ToString();

            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return false;

            var token = authHeader["Bearer ".Length..].Trim();
            var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}