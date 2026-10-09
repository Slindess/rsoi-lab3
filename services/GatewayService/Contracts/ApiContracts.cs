namespace GatewayService.Contracts;
public sealed record CreateReservationRequest(Guid HotelUid, DateOnly StartDate, DateOnly EndDate);
public sealed record PaymentInfo(string Status, int Price);
public sealed record HotelInfo(Guid HotelUid, string Name, string FullAddress, int Stars);
public sealed record ReservationResponse(Guid ReservationUid, HotelInfo Hotel, DateOnly StartDate, DateOnly EndDate, string Status, PaymentInfo? Payment);
public sealed record CreateReservationResponse(Guid ReservationUid, Guid HotelUid, DateOnly StartDate, DateOnly EndDate, int Discount, string Status, PaymentInfo Payment);
public sealed record UserInfoResponse(IReadOnlyList<ReservationResponse> Reservations, object Loyalty);
public sealed record ErrorResponse(string Message);
public sealed record ValidationErrorResponse(string Message, IReadOnlyList<object> Errors);
