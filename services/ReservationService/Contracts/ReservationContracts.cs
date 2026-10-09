using ReservationService.Domain;
namespace ReservationService.Contracts;
public sealed record HotelDto(Guid HotelUid, string Name, string Country, string City, string Address, int Stars, int Price);
public sealed record HotelPageDto(int Page, int PageSize, long TotalElements, IReadOnlyList<HotelDto> Items);
public sealed record CreateReservationDto(Guid ReservationUid, string Username, Guid PaymentUid, Guid HotelUid, DateOnly StartDate, DateOnly EndDate);
public sealed record ReservationDto(Guid ReservationUid, string Username, Guid PaymentUid, HotelDto Hotel, ReservationStatus Status, DateOnly StartDate, DateOnly EndDate);
public sealed record ErrorDto(string Message);
