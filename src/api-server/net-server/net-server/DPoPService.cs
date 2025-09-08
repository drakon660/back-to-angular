using System.Security.Claims;
using FastEndpoints;

namespace net_server;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

// 1. DPoP Service Interface
public interface IDPoPService
{
    Task<bool> ValidateDPoPProofAsync(string dPopProof, string httpMethod, string uri, string? accessToken = null);
    Task<string> GenerateDPoPTokenAsync(RSA privateKey, string httpMethod, string uri, string? accessToken = null);
    Task<string> CreateDPoPBoundAccessTokenAsync(string subject, JsonWebKey publicKey);
}

// 2. DPoP Service Implementation
public class DPoPService : IDPoPService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DPoPService> _logger;
    private readonly Dictionary<string, DateTime> _usedJtis = new(); // In production, use distributed cache

    public DPoPService(IConfiguration configuration, ILogger<DPoPService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ValidateDPoPProofAsync(string dPopProof, string httpMethod, string uri, string? accessToken = null)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(dPopProof);

            // 1. Validate JWT header
            if (jsonToken.Header.Typ != "dpop+jwt")
            {
                _logger.LogWarning("Invalid DPoP proof: wrong typ header");
                return false;
            }

            if (jsonToken.Header.Alg != "RS256")
            {
                _logger.LogWarning("Invalid DPoP proof: unsupported algorithm");
                return false;
            }

            // 2. Extract public key from jwk header
            var jwkHeader = jsonToken.Header["jwk"] as JsonElement?;
            if (!jwkHeader.HasValue)
            {
                _logger.LogWarning("Invalid DPoP proof: missing jwk header");
                return false;
            }

            var publicKey = ExtractPublicKeyFromJwk(jwkHeader.Value);
            if (publicKey == null)
            {
                _logger.LogWarning("Invalid DPoP proof: invalid jwk");
                return false;
            }

            // 3. Validate JWT signature
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(publicKey),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            var principal = handler.ValidateToken(dPopProof, validationParameters, out _);

            // 4. Validate claims
            var claims = jsonToken.Claims.ToDictionary(c => c.Type, c => c.Value);

            // Check HTTP method
            if (!claims.ContainsKey("htm") || claims["htm"] != httpMethod)
            {
                _logger.LogWarning("Invalid DPoP proof: HTTP method mismatch");
                return false;
            }

            // Check URI
            if (!claims.ContainsKey("htu") || claims["htu"] != uri)
            {
                _logger.LogWarning("Invalid DPoP proof: URI mismatch");
                return false;
            }

            // Check access token hash if provided
            if (accessToken != null)
            {
                var expectedAth = ComputeAccessTokenHash(accessToken);
                if (!claims.ContainsKey("ath") || claims["ath"] != expectedAth)
                {
                    _logger.LogWarning("Invalid DPoP proof: access token hash mismatch");
                    return false;
                }
            }

            // Check jti for replay protection
            if (claims.ContainsKey("jti"))
            {
                var jti = claims["jti"];
                if (_usedJtis.ContainsKey(jti))
                {
                    _logger.LogWarning("Invalid DPoP proof: jti already used");
                    return false;
                }
                _usedJtis[jti] = DateTime.UtcNow;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating DPoP proof");
            return false;
        }
    }

    public async Task<string> GenerateDPoPTokenAsync(RSA privateKey, string httpMethod, string uri, string? accessToken = null)
    {
        var publicKey = RSA.Create();
        publicKey.ImportRSAPublicKey(privateKey.ExportRSAPublicKey(), out _);

        var jwk = CreateJwkFromRSA(publicKey);

        var header = new JwtHeader(new SigningCredentials(new RsaSecurityKey(privateKey), SecurityAlgorithms.RsaSha256))
        {
            {"typ", "dpop+jwt"},
            {"jwk", jwk}
        };

        var claims = new List<Claim>
        {
            new("htm", httpMethod),
            new("htu", uri),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new("jti", Guid.NewGuid().ToString())
        };

        if (accessToken != null)
        {
            claims.Add(new Claim("ath", ComputeAccessTokenHash(accessToken)));
        }

        var payload = new JwtPayload(claims);
        var token = new JwtSecurityToken(header, payload);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> CreateDPoPBoundAccessTokenAsync(string subject, JsonWebKey publicKey)
    {
        var securityKey = new SymmetricSecurityKey(
            Convert.FromBase64String(_configuration["Authentication:SecretForKey"]));
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("sub", subject),
            new("cnf", JsonSerializer.Serialize(new { jkt = ComputeJwkThumbprint(publicKey) }))
        };

        var token = new JwtSecurityToken(
            _configuration["Authentication:Issuer"],
            _configuration["Authentication:Audience"],
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RSA? ExtractPublicKeyFromJwk(JsonElement jwk)
    {
      try
      {
        var rsa = RSA.Create();
        var n = Base64UrlEncoder.DecodeBytes(jwk.GetProperty("n").GetString());
        var e = Base64UrlEncoder.DecodeBytes(jwk.GetProperty("e").GetString());

        // Option 1: Use ImportParameters (recommended)
        rsa.ImportParameters(new RSAParameters { Modulus = n, Exponent = e });

        // Option 2: If you need to use ImportRSAPublicKey, you'd need to convert to DER format
        // var derBytes = ConvertRSAParametersToDER(n, e);
        // rsa.ImportRSAPublicKey(derBytes, out _);

        return rsa;
      }
      catch
      {
        return null;
      }
    }

    private object CreateJwkFromRSA(RSA rsa)
    {
        var parameters = rsa.ExportParameters(false);
        return new
        {
            kty = "RSA",
            n = Base64UrlEncoder.Encode(parameters.Modulus),
            e = Base64UrlEncoder.Encode(parameters.Exponent)
        };
    }

    private string ComputeAccessTokenHash(string accessToken)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(accessToken));
        return Base64UrlEncoder.Encode(hash);
    }

    private string ComputeJwkThumbprint(JsonWebKey jwk)
    {
        var thumbprint = new
        {
            kty = jwk.Kty,
            n = jwk.N,
            e = jwk.E
        };

        var json = JsonSerializer.Serialize(thumbprint);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Base64UrlEncoder.Encode(hash);
    }
}

// 3. DPoP Middleware
public class DPoPMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<DPoPMiddleware> _logger;

  public DPoPMiddleware(RequestDelegate next, ILogger<DPoPMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    // Check if endpoint requires DPoP
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
      var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
      if (authHeader?.StartsWith("DPoP ") == true)
      {
        var accessToken = authHeader.Substring(5);
        var dPopProof = context.Request.Headers["DPoP"].FirstOrDefault();

        if (string.IsNullOrEmpty(dPopProof))
        {
          context.Response.StatusCode = 400;
          await context.Response.WriteAsync("DPoP proof required");
          return;
        }

        var httpMethod = context.Request.Method;
        var uri = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";

        // Get scoped service from the current request scope
        var dPopService = context.RequestServices.GetRequiredService<IDPoPService>();

        var isValid = await dPopService.ValidateDPoPProofAsync(dPopProof, httpMethod, uri, accessToken);
        if (!isValid)
        {
          context.Response.StatusCode = 401;
          await context.Response.WriteAsync("Invalid DPoP proof");
          return;
        }

        context.Request.Headers["Authorization"] = $"Bearer {accessToken}";

        // var handler = new JwtSecurityTokenHandler();
        // try
        // {
        //   var jwtToken = handler.ReadJwtToken(accessToken);
        //   var claims = jwtToken.Claims.ToList();
        //   var identity = new ClaimsIdentity(claims, "DPoP");
        //   context.User = new ClaimsPrincipal(identity);
        // }
        // catch (Exception ex)
        // {
        //   _logger.LogError(ex, "Failed to parse JWT token");
        //   context.Response.StatusCode = 401;
        //   await context.Response.WriteAsync("Invalid access token");
        //   return;
        // }
      }
    }

    await _next(context);
  }
}

// 4. Updated Auth Endpoint with DPoP support
// public class AuthEndpointWithDPoP : Endpoint<LoginRequest, DPopLoginResponse>
// {
//     private readonly IDPoPService _dPopService;
//     private readonly ILogger<AuthEndpointWithDPoP> _logger;
//
//     public AuthEndpointWithDPoP(IDPoPService dPopService, ILogger<AuthEndpointWithDPoP> logger)
//     {
//         _dPopService = dPopService;
//         _logger = logger;
//     }
//
//     public override void Configure()
//     {
//         Post("/api/sign-in-dpop");
//         AllowAnonymous();
//     }
//
//     public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
//     {
//         try
//         {
//             // Validate DPoP proof
//             var dPopProof = HttpContext.Request.Headers["DPoP"].FirstOrDefault();
//             if (string.IsNullOrEmpty(dPopProof))
//             {
//                 //await SendAsync(new { Message = "DPoP proof required" }, 400, ct);
//                 return;
//             }
//
//             var httpMethod = HttpContext.Request.Method;
//             var uri = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
//
//             var isValidProof = await _dPopService.ValidateDPoPProofAsync(dPopProof, httpMethod, uri);
//             if (!isValidProof)
//             {
//                 //await SendAsync(new { Message = "Invalid DPoP proof" }, 401, ct);
//                 return;
//             }
//
//             // Validate credentials (implement your user validation logic)
//             if (req.Username != "admin@com" || req.Password != "123")
//             {
//                 //await SendAsync(new { Message = "Invalid credentials" }, 401, ct);
//                 return;
//             }
//
//             // Extract public key from DPoP proof to bind to access token
//             var handler = new JwtSecurityTokenHandler();
//             var jsonToken = handler.ReadJwtToken(dPopProof);
//             var jwkHeader = jsonToken.Header["jwk"] as JsonElement?;
//
//             if (!jwkHeader.HasValue)
//             {
//                 //await SendAsync(new { Message = "Invalid DPoP proof: missing jwk" }, 400, ct);
//                 return;
//             }
//
//             var publicKey = new JsonWebKey(jwkHeader.Value.GetRawText());
//             var accessToken = await _dPopService.CreateDPoPBoundAccessTokenAsync("user123", publicKey);
//
//             await SendAsync(new DPopLoginResponse
//             {
//                 Token = accessToken,
//                 TokenType = "DPoP"
//             }, 200, ct);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error during DPoP authentication");
//             //await SendAsync(new { Message = "Internal server error" }, 500, ct);
//         }
//     }
// }

// 5. Updated Login Response
public class DPopLoginResponse
{
    public string Token { get; set; }
    public string TokenType { get; set; } = "Bearer";
}
