using FastEndpoints;

namespace net_server.Profiles;

public class Endpoint : EndpointWithoutRequest<IEnumerable<Profile>>
{
  static List<Profile> _profiles = new List<Profile>
  {
    new Profile(
      Id: 1,
      Name: "Junior Engineer",
      Description: "A cost effective option for simple tasks. Our junior engineers are fully capable of general bot assembly.",
      ImageName: "profile-images/junior.png",
      Category: "Junior",
      Price: 120,
      Discount: 0
    ),
    new Profile(
      Id: 2,
      Name: "Associate Engineer",
      Description: "Associate engineers can help with both assembly and troubleshooting simple issues with connectivity and functionalty.",
      ImageName: "profile-images/associate.png",
      Category: "Associate",
      Price: 180,
      Discount: 0
    ),
    new Profile(
      Id: 3,
      Name: "Senior Engineer",
      Description: "Senior engineers are capable and efficient at programming individual bots and troubleshooting complex issues with complicated bots and systems.",
      ImageName: "profile-images/senior.png",
      Category: "Senior",
      Price: 225,
      Discount: 0
    ),
    new Profile(
      Id: 4,
      Name: "Chief Engineer",
      Description: "Our chief engineers will help you create an entire plan for creating an efficient bot workforce and assist in programming them to work within complex systems.",
      ImageName: "profile-images/chief.png",
      Category: "Chief",
      Price: 350,
      Discount: 0
    )
  };

  public override void Configure()
  {
    Get("/api/profiles");
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    await SendAsync(_profiles, cancellation: ct);
  }
}

public record Profile(
  int Id,
  string Name,
  string Description,
  string ImageName,
  string Category,
  decimal Price,
  decimal Discount
);
