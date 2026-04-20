namespace Streamon.Tests.Fixtures;

public record OrderAddress(string Street, string City, string ZipCode);

public record OrderItem(string Name, int Quantity, decimal UnitPrice);
