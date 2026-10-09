namespace ReservationService.Domain;
public sealed record Hotel(int Id, Guid HotelUid, string Name, string Country, string City, string Address, int Stars, int Price);
public enum ReservationStatus { PAID, CANCELED }
public sealed record Reservation(Guid ReservationUid, string Username, Guid PaymentUid, int HotelId, ReservationStatus Status, DateOnly StartDate, DateOnly EndDate);
