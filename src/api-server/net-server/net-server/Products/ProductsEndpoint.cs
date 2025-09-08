using FastEndpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace net_server.Products;

[Authorize]
public class ProductsEndpoint : EndpointWithoutRequest<IEnumerable<Product>>
{
  private static List<Product> _robotParts =
  [
    new(1, "A robot head with an unusually large eye and teloscpic neck -- excellent for exploring high spaces.",
      "Large Cyclops", "robot-parts/head-big-eye.png", "Heads", 1220.5, 0.2),
    new(17, "A spring base - great for reaching high places.", "Spring Base", "robot-parts/base-spring.png", "Bases",
      1190.5, 0),
    new(6, "An articulated arm with a claw -- great for reaching around corners or working in tight spaces.",
      "Articulated Arm", "robot-parts/arm-articulated-claw.png", "Arms", 275, 0),
    new(2, "A friendly robot head with two eyes and a smile -- great for domestic use.", "Friendly Bot",
      "robot-parts/head-friendly.png", "Heads", 945.0, 0.2),
    new(3,
      "A large three-eyed head with a shredder for a mouth -- great for crushing light medals or shredding documents.",
      "Shredder", "robot-parts/head-shredder.png", "Heads", 1275.5, 0),
    new(16,
      "A single-wheeled base with an accelerometer capable of higher speeds and navigating rougher terrain than the two-wheeled variety.",
      "Single Wheeled Base", "robot-parts/base-single-wheel.png", "Bases", 1190.5, 0.1),
    new(13, "A simple torso with a pouch for carrying items.", "Pouch Torso", "robot-parts/torso-pouch.png", "Torsos",
      785, 0),
    new(7,
      "An arm with two independent claws -- great when you need an extra hand. Need four hands? Equip your bot with two of these arms.",
      "Two Clawed Arm", "robot-parts/arm-dual-claw.png", "Arms", 285, 0),
    new(4, "A simple single-eyed head -- simple and inexpensive.", "Small Cyclops", "robot-parts/head-single-eye.png",
      "Heads", 750.0, 0),
    new(9, "An arm with a propeller -- good for propulsion or as a cooling fan.", "Propeller Arm",
      "robot-parts/arm-propeller.png", "Arms", 230, 0.1),
    new(15, "A rocket base capable of high speed, controlled flight.", "Rocket Base", "robot-parts/base-rocket.png",
      "Bases", 1520.5, 0),
    new(10, "A short and stubby arm with a claw -- simple, but cheap.", "Stubby Claw Arm",
      "robot-parts/arm-stubby-claw.png", "Arms", 125, 0),
    new(11, "A torso that can bend slightly at the waist and equiped with a heat guage.", "Flexible Gauged Torso",
      "robot-parts/torso-flexible-gauged.png", "Torsos", 1575, 0),
    new(14, "A two wheeled base with an accelerometer for stability.", "Double Wheeled Base",
      "robot-parts/base-double-wheel.png", "Bases", 895, 0),
    new(5, "A robot head with three oscillating eyes -- excellent for surveillance.", "Surveillance",
      "robot-parts/head-surveillance.png", "Heads", 1255.5, 0),
    new(8, "A telescoping arm with a grabber.", "Grabber Arm", "robot-parts/arm-grabber.png", "Arms", 205.5, 0),
    new(12, "A less flexible torso with a battery gauge.", "Gauged Torso", "robot-parts/torso-gauged.png", "Torsos",
      1385, 0),
    new(18, "An inexpensive three-wheeled base. only capable of slow speeds and can only function on smooth surfaces.",
      "Triple Wheeled Base", "robot-parts/base-triple-wheel.png", "Bases", 700.5, 0)
  ];

  public override void Configure()
  {
    Get("/api/products");
    //Policy(x=>x.RequireAuthenticatedUser());
    //AuthSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var user = HttpContext.User;
    await SendAsync(_robotParts, cancellation: ct);
  }
}
