using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ERP.API.Models; 

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IDbConnection db, IConfiguration config, ILogger<AuthController> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try 
        {
            var user = await _db.QuerySingleOrDefaultAsync<User>(
                @"SELECT * FROM users WHERE username = @Username AND is_active = true",
                new { request.Username }
            );

            if (user == null)
            {
                _logger.LogWarning("Login failed for username {Username}: user not found", request.Username);
                return Unauthorized(new { error = "Invalid credentials" });
            }

            // Generate token
            var token = GenerateJwtToken(user);

            // Update last login
            await _db.ExecuteAsync(
                @"UPDATE users SET date_updated = @Now WHERE user_id = @UserId",
                new { Now = DateTime.UtcNow, UserId = user.UserId }
            );

            return Ok(new { token = token, userId = user.UserId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config["JwtSettings:SecretKey"] ?? "YOUR_DEFAULT_SECRET_KEY_MIN_16_CHARS");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
}
