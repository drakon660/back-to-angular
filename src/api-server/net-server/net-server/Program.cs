using System.Net;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using net_server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();
//   .AddJwtBearer(options =>
// {
//   options.TokenValidationParameters = new()
//   {
//     ValidateIssuer = true,
//     ValidateAudience = true,
//     ValidateIssuerSigningKey = true,
//     ValidIssuer = builder.Configuration["Authentication:Issuer"],
//     ValidAudience = builder.Configuration["Authentication:Audience"],
//     IssuerSigningKey = new SymmetricSecurityKey(
//       Convert.FromBase64String(
//         builder.Configuration["Authentication:SecretForKey"]
//         ?? throw new KeyNotFoundException("SecretForKey not found or invalid")))
//   };
// });

builder.Services.AddAuthenticationCookie(validFor: TimeSpan.FromSeconds(30), options =>
{
  options.Cookie.Name = "__Host-spa";
  options.LoginPath = "/login"; // not used in API-style, but must be set
  options.Cookie.HttpOnly = true;
  options.Cookie.SameSite = SameSiteMode.Lax; // or `None` for cross-origin
  options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
  options.Events.OnRedirectToLogin = ctx =>
  {
    ctx.Response.StatusCode = 401;
    return Task.CompletedTask;
  };
});

builder.Services.AddAuthorization(options =>
{
  options.DefaultPolicy = new AuthorizationPolicyBuilder()
    //.AddAuthenticationSchemes("Bearer", "Cookies")
    .AddAuthenticationSchemes("Cookies")
    .RequireAuthenticatedUser()
    .Build();
});

// builder.Services.AddSession(options =>
// {
//   options.Cookie.Name = ".AdventureWorks.Session";
//   options.IdleTimeout = TimeSpan.FromSeconds(10);
//   options.Cookie.HttpOnly = true;
//   options.Cookie.IsEssential = true;
// });

builder.Services.AddDistributedMemoryCache();
//builder.Services.AddScoped<IDPoPService, DPoPService>();
builder.Services.AddSingleton<IDistributedCache, MemorySessionStore>();
builder.Services.AddSingleton<ITicketStore, CookieStore>();
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
  .Configure<ITicketStore>((x,y)=>x.SessionStore = y);

builder.Services.AddCors(options =>
  {
    options.AddPolicy("AllowAngular", policy =>
    {
      policy.WithOrigins("http://localhost:4200") // 👈 Must be explicit, no wildcard
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // 👈 Required for cookie auth
    });
  })
  .AddFastEndpoints()
  .SwaggerDocument();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
//app.UseSession();

//app.UseMiddleware<DPoPMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints()
  .UseSwaggerGen();

app.Run();
