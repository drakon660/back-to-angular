using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;

namespace net_server;

public class AuthEndpoint : Endpoint<LoginRequest, LoginResponse>
{
  public override void Configure()
  {
    Post("/api/sign-in");
    AllowAnonymous();
  }

  public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
  {
    if (req.Username == "admin@com" && req.Password == "123")
    {
      await CookieAuth.SignInAsync(u =>
      {
        // u.Roles.Add("Admin");
        // u.Permissions.AddRange(["Create_Item", "Delete_Item"]);
        u.Claims.Add(new("firstname", "Kevin"));
        u.Claims.Add(new("lastname", "Dockx"));

        //indexer based claim setting
        u[ClaimTypes.Email] = "kevin.dockx@gmail.com";
      });
    }
  }
}

public class AuthEndpointWithToken : Endpoint<LoginRequest, LoginResponse>
{
  private readonly IConfiguration _configuration;

  public AuthEndpointWithToken(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  public override void Configure()
  {
    Post("/api/sign-in-token");
    AllowAnonymous();
  }

  public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
  {
    if (req.Username == "admin@com" && req.Password == "123")
    {
      var securityKey = new SymmetricSecurityKey(
        Convert.FromBase64String(_configuration["Authentication:SecretForKey"]
                                 ?? throw new KeyNotFoundException("SecretForKey not found or invalid")));
      var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

      var claimsForToken = new List<Claim>
      {
        new("sub", "1234"),
        new("firstname", "Kevin"),
        new("lastname", "Dockx"),
        new("email", "kevin.dockx@gmail.com")
      };

      var jwtSecurityToken = new JwtSecurityToken(
        _configuration["Authentication:Issuer"],
        _configuration["Authentication:Audience"],
        claimsForToken,
        DateTime.UtcNow,
        DateTime.UtcNow.AddHours(1),
        signingCredentials);

      var tokenToReturn = new JwtSecurityTokenHandler()
        .WriteToken(jwtSecurityToken);

      await SendAsync(new LoginResponse { Token = tokenToReturn }, 200, ct);
    }
  }
}

public class LogOutAuthEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Post("/api/sign-out");
    AllowAnonymous();
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
      await CookieAuth.SignOutAsync();
  }
}

public class SessionRemoveEndpoint : Endpoint<CookieId>
{
  private readonly ITicketStore _store;

  public SessionRemoveEndpoint(ITicketStore store)
  {
    _store = store;
  }

  public override void Configure()
  {
    Post("/api/remove-session");
    AllowAnonymous();
  }

  public override async Task HandleAsync(CookieId cookieId, CancellationToken ct)
  {
    await _store.RemoveAsync(cookieId.Id, ct);
  }
}

public class Me : EndpointWithoutRequest<UserResponse>
{
  public override void Configure()
  {
    Get("/api/me");
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
     var email = HttpContext.User.FindFirst(x=>x.Type == ClaimTypes.Email)?.Value;
     var firstName = HttpContext.User.FindFirst(x=>x.Type == "firstname")?.Value;
     var lastName = HttpContext.User.FindFirst(x=>x.Type == "lastname")?.Value;

     var user = new UserDto(email, firstName, lastName);

     await SendOkAsync(new UserResponse(user), ct);
  }
}

public class CookieExpireEndpoint : EndpointWithoutRequest
{
  private readonly ILogger<CookieExpireEndpoint> _logger;
  private readonly ITicketStore _store;

  public CookieExpireEndpoint(ILogger<CookieExpireEndpoint> logger, ITicketStore store)
  {
    _logger = logger;
    _store = store;
  }

  public override void Configure()
  {
    Get("/api/notifications/sse");
    AllowAnonymous();
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    await SendEventStreamAsync("cookieExpired", GetDataStream(ct), ct);
  }

  private async IAsyncEnumerable<object> GetDataStream([EnumeratorCancellation] CancellationToken ct)
  {
    while (!ct.IsCancellationRequested)
    {
      await Task.Delay(1000, ct);
      var user = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;

      bool isExpired = true;

      _logger.LogInformation("data stream");

      if (user is not null)
      {
        var ticket = await _store.RetrieveAsync(user, ct);
        _logger.LogInformation("has user");
        if (ticket?.Properties.ExpiresUtc.HasValue == true)
        {
          isExpired = ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow;
          _logger.LogInformation("ticket expired {isExpired}", isExpired);
        }
      }

      yield return new { CookieExpired = isExpired };
    }
  }
}

public record CookieId(string Id);

public record LoginRequest(string Username, string Password);

public class LoginResponse
{
  public string Token { get; set; }
}

public record UserResponse(UserDto user);

public record UserDto(string Email, string FirstName, string LastName);
