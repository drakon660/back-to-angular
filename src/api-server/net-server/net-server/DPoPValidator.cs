using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace net_server;

public class DPoPValidator
{
  public static async Task<JsonWebKey?> ValidateDpopProofAsync(string dpopJwt, string htu, string htm)
  {
    var handler = new JwtSecurityTokenHandler();

    var token = handler.ReadJwtToken(dpopJwt);

    // Extract JWK from header
    var jwkJson = token.Header["jwk"]?.ToString();
    if (jwkJson == null)
      return null;

    var jwk = JsonSerializer.Deserialize<JsonWebKey>(jwkJson);

    var validationParams = new TokenValidationParameters
    {
      RequireSignedTokens = true,
      ValidateIssuer = false,
      ValidateAudience = false,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = jwk
    };

    try
    {
      handler.ValidateToken(dpopJwt, validationParams, out _);

      var htmClaim = token.Claims.FirstOrDefault(c => c.Type == "htm")?.Value;
      var htuClaim = token.Claims.FirstOrDefault(c => c.Type == "htu")?.Value;

      if (!string.Equals(htmClaim, htm, StringComparison.OrdinalIgnoreCase))
        return null;

      if (!string.Equals(htuClaim, htu, StringComparison.OrdinalIgnoreCase))
        return null;

      return jwk;
    }
    catch
    {
      return null;
    }
  }
}
