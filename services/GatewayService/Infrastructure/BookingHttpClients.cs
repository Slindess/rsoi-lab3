using System.Net;
using System.Net.Http.Json;
using GatewayService.Application;
using GatewayService.Domain;

namespace GatewayService.Infrastructure;

public sealed class BookingHttpClients(HttpClient http, IConfiguration configuration, InMemoryCircuitBreaker circuitBreaker) : IBookingClients
{
    private const string ReservationService = "Reservation Service";
    private const string PaymentService = "Payment Service";
    private const string LoyaltyService = "Loyalty Service";
    private string ReservationUrl => configuration["Services:Reservation"] ?? "http://localhost:8070";
    private string PaymentUrl => configuration["Services:Payment"] ?? "http://localhost:8060";
    private string LoyaltyUrl => configuration["Services:Loyalty"] ?? "http://localhost:8050";

    private Task<T> ReadAsync<T>(string service, Func<Task<T>> request) => circuitBreaker.ExecuteAsync(service, request);
    private static async Task<T> JsonAsync<T>(HttpResponseMessage response, CancellationToken ct) { response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<T>(ct))!; }
    private static async Task<HttpResponseMessage> WriteAsync(string service, Func<Task<HttpResponseMessage>> request)
    {
        try
        {
            var response = await request();
            if ((int)response.StatusCode >= 500) throw new DownstreamUnavailableException(service);
            return response;
        }
        catch (DownstreamUnavailableException) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException) { throw new DownstreamUnavailableException(service, exception); }
    }

    public Task<HotelPage> GetHotelsAsync(int page, int size, CancellationToken ct) => ReadAsync(ReservationService, async () => await JsonAsync<HotelPage>(await http.GetAsync($"{ReservationUrl}/internal/hotels?page={page}&size={size}", ct), ct));
    public Task<Hotel?> GetHotelAsync(Guid uid, CancellationToken ct) => ReadAsync(ReservationService, async () => { var response = await http.GetAsync($"{ReservationUrl}/internal/hotels/{uid}", ct); return response.StatusCode == HttpStatusCode.NotFound ? null : await JsonAsync<Hotel>(response, ct); });
    public Task<Loyalty> GetLoyaltyAsync(string username, CancellationToken ct) => ReadAsync(LoyaltyService, async () => await JsonAsync<Loyalty>(await http.GetAsync($"{LoyaltyUrl}/internal/loyalty/{Uri.EscapeDataString(username)}", ct), ct));
    public async Task<Loyalty> ChangeLoyaltyAsync(string username, bool increment, CancellationToken ct) { var url = $"{LoyaltyUrl}/internal/loyalty/{Uri.EscapeDataString(username)}/reservations"; var response = await WriteAsync(LoyaltyService, () => increment ? http.PostAsync(url, null, ct) : http.DeleteAsync(url, ct)); return await JsonAsync<Loyalty>(response, ct); }
    public async Task<Payment> CreatePaymentAsync(int price, CancellationToken ct) { var response = await WriteAsync(PaymentService, () => http.PostAsJsonAsync($"{PaymentUrl}/internal/payments", new { price }, ct)); return await JsonAsync<Payment>(response, ct); }
    public Task<Payment?> GetPaymentAsync(Guid uid, CancellationToken ct) => ReadAsync(PaymentService, async () => { var response = await http.GetAsync($"{PaymentUrl}/internal/payments/{uid}", ct); return response.StatusCode == HttpStatusCode.NotFound ? null : await JsonAsync<Payment>(response, ct); });
    public async Task CancelPaymentAsync(Guid uid, CancellationToken ct) { var response = await WriteAsync(PaymentService, () => http.DeleteAsync($"{PaymentUrl}/internal/payments/{uid}", ct)); response.EnsureSuccessStatusCode(); }
    public Task<IReadOnlyList<Reservation>> GetReservationsAsync(string username, CancellationToken ct) => ReadAsync(ReservationService, async () => await JsonAsync<IReadOnlyList<Reservation>>(await http.GetAsync($"{ReservationUrl}/internal/reservations?username={Uri.EscapeDataString(username)}", ct), ct));
    public Task<Reservation?> GetReservationAsync(Guid uid, CancellationToken ct) => ReadAsync(ReservationService, async () => { var response = await http.GetAsync($"{ReservationUrl}/internal/reservations/{uid}", ct); return response.StatusCode == HttpStatusCode.NotFound ? null : await JsonAsync<Reservation>(response, ct); });
    public async Task<Reservation> CreateReservationAsync(Guid uid, string username, Guid paymentUid, Guid hotelUid, DateOnly startDate, DateOnly endDate, CancellationToken ct) { var response = await WriteAsync(ReservationService, () => http.PostAsJsonAsync($"{ReservationUrl}/internal/reservations", new { reservationUid = uid, username, paymentUid, hotelUid, startDate, endDate }, ct)); return await JsonAsync<Reservation>(response, ct); }
    public async Task<bool> CancelReservationAsync(Guid uid, string username, CancellationToken ct) { var response = await WriteAsync(ReservationService, () => http.DeleteAsync($"{ReservationUrl}/internal/reservations/{uid}?username={Uri.EscapeDataString(username)}", ct)); return response.IsSuccessStatusCode; }
    public async Task RollbackReservationAsync(Guid uid, CancellationToken ct) { var response = await WriteAsync(ReservationService, () => http.DeleteAsync($"{ReservationUrl}/internal/reservations/{uid}/rollback", ct)); response.EnsureSuccessStatusCode(); }
}
