using GatewayService.Contracts;
using GatewayService.Domain;
using GatewayService.Infrastructure;

namespace GatewayService.Application;

public interface IBookingClients
{
    Task<HotelPage> GetHotelsAsync(int page, int size, CancellationToken ct);
    Task<Hotel?> GetHotelAsync(Guid uid, CancellationToken ct);
    Task<Loyalty> GetLoyaltyAsync(string username, CancellationToken ct);
    Task<Loyalty> ChangeLoyaltyAsync(string username, bool increment, CancellationToken ct);
    Task<Payment> CreatePaymentAsync(int price, CancellationToken ct);
    Task<Payment?> GetPaymentAsync(Guid uid, CancellationToken ct);
    Task CancelPaymentAsync(Guid uid, CancellationToken ct);
    Task<IReadOnlyList<Reservation>> GetReservationsAsync(string username, CancellationToken ct);
    Task<Reservation?> GetReservationAsync(Guid uid, CancellationToken ct);
    Task<Reservation> CreateReservationAsync(Guid uid, string username, Guid paymentUid, Guid hotelUid, DateOnly startDate, DateOnly endDate, CancellationToken ct);
    Task<bool> CancelReservationAsync(Guid uid, string username, CancellationToken ct);
    Task RollbackReservationAsync(Guid uid, CancellationToken ct);
}

public sealed class BookingApplication(IBookingClients clients, LoyaltyRetryQueue retryQueue)
{
    public Task<HotelPage> HotelsAsync(int page, int size, CancellationToken ct) => clients.GetHotelsAsync(page, size, ct);
    public Task<Loyalty> LoyaltyAsync(string username, CancellationToken ct) => clients.GetLoyaltyAsync(username, ct);

    public async Task<CreateReservationResponse> CreateAsync(string username, CreateReservationRequest request, CancellationToken ct)
    {
        if (request.EndDate <= request.StartDate) throw new ArgumentException("endDate must be later than startDate");
        var hotel = await clients.GetHotelAsync(request.HotelUid, ct) ?? throw new KeyNotFoundException("Hotel not found");
        var loyalty = await clients.GetLoyaltyAsync(username, ct);
        var nights = request.EndDate.DayNumber - request.StartDate.DayNumber;
        var payment = await clients.CreatePaymentAsync(hotel.Price * nights * (100 - loyalty.Discount) / 100, ct);
        var reservationUid = Guid.NewGuid();
        var reservationCreated = false;
        try
        {
            await clients.CreateReservationAsync(reservationUid, username, payment.PaymentUid, hotel.HotelUid, request.StartDate, request.EndDate, ct);
            reservationCreated = true;
            await clients.ChangeLoyaltyAsync(username, true, ct);
        }
        catch
        {
            if (reservationCreated) await TryCompensationAsync(() => clients.RollbackReservationAsync(reservationUid, CancellationToken.None));
            await TryCompensationAsync(() => clients.CancelPaymentAsync(payment.PaymentUid, CancellationToken.None));
            throw;
        }
        return new(reservationUid, hotel.HotelUid, request.StartDate, request.EndDate, loyalty.Discount, "PAID", new(payment.Status, payment.Price));
    }

    public async Task<IReadOnlyList<ReservationResponse>> AllAsync(string username, CancellationToken ct)
    {
        var reservations = await clients.GetReservationsAsync(username, ct);
        return (await Task.WhenAll(reservations.Select(x => MapAsync(x, ct)))).ToArray();
    }

    public async Task<ReservationResponse?> OneAsync(string username, Guid uid, CancellationToken ct)
    {
        var reservation = await clients.GetReservationAsync(uid, ct);
        return reservation is null || reservation.Username != username ? null : await MapAsync(reservation, ct);
    }

    public async Task<bool> CancelAsync(string username, Guid uid, CancellationToken ct)
    {
        var reservation = await clients.GetReservationAsync(uid, ct);
        if (reservation is null || reservation.Username != username) return false;
        await clients.CancelPaymentAsync(reservation.PaymentUid, ct);
        if (!await clients.CancelReservationAsync(uid, username, ct)) return false;
        try { await clients.ChangeLoyaltyAsync(username, false, ct); }
        catch (DownstreamUnavailableException exception) when (exception.ServiceName == "Loyalty Service")
        {
            await retryQueue.EnqueueAsync(new(username), CancellationToken.None);
        }
        return true;
    }

    public async Task<UserInfoResponse> MeAsync(string username, CancellationToken ct)
    {
        var reservations = await AllAsync(username, ct);
        object loyalty;
        try { loyalty = await LoyaltyAsync(username, ct); }
        catch (DownstreamUnavailableException exception) when (exception.ServiceName == "Loyalty Service") { loyalty = new { }; }
        return new(reservations, loyalty);
    }

    private async Task<ReservationResponse> MapAsync(Reservation reservation, CancellationToken ct)
    {
        PaymentInfo? payment = null;
        try
        {
            var value = await clients.GetPaymentAsync(reservation.PaymentUid, ct);
            if (value is not null) payment = new(value.Status, value.Price);
        }
        catch (DownstreamUnavailableException exception) when (exception.ServiceName == "Payment Service") { }
        return new(reservation.ReservationUid, new(reservation.Hotel.HotelUid, reservation.Hotel.Name,
            $"{reservation.Hotel.Country}, {reservation.Hotel.City}, {reservation.Hotel.Address}", reservation.Hotel.Stars),
            reservation.StartDate, reservation.EndDate, reservation.Status, payment);
    }

    private static async Task TryCompensationAsync(Func<Task> action) { try { await action(); } catch { } }
}
