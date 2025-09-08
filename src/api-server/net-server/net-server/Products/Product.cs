namespace net_server.Products;

public record Product(
  int Id,
  string Description,
  string Name,
  string ImageName,
  string Category,
  double Price,
  double Discount
);
