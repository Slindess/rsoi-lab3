namespace GatewayService.Domain;
public sealed record Hotel(Guid HotelUid, string Name, string Country, string City, string Address, int Stars, int Price);
public sealed record HotelPage(int Page, int PageSize, long TotalElements, IReadOnlyList<Hotel> Items);
public sealed record Loyalty(string Status, int Discount, int ReservationCount);
public sealed record Payment(Guid PaymentUid, string Status, int Price);
public sealed record Reservation(Guid ReservationUid, string Username, Guid PaymentUid, Hotel Hotel, string Status, DateOnly StartDate, DateOnly EndDate);
