using ReservationService.Contracts;
using ReservationService.Domain;
namespace ReservationService.Application;
public interface IReservationRepository
{
    Task<(IReadOnlyList<Hotel> Items, long Total)> GetHotelsAsync(int page, int size, CancellationToken ct);
    Task<Hotel?> GetHotelAsync(Guid uid, CancellationToken ct);
    Task<IReadOnlyList<(Reservation Reservation, Hotel Hotel)>> GetReservationsAsync(string username, CancellationToken ct);
    Task<(Reservation Reservation, Hotel Hotel)?> GetReservationAsync(Guid uid, CancellationToken ct);
    Task CreateAsync(Reservation reservation, CancellationToken ct);
    Task<bool> CancelAsync(Guid uid, string username, CancellationToken ct);
    Task<bool> DeleteAsync(Guid uid, CancellationToken ct);
}
public sealed class ReservationApplication(IReservationRepository repository)
{
    private static HotelDto Map(Hotel h) => new(h.HotelUid, h.Name, h.Country, h.City, h.Address, h.Stars, h.Price);
    private static ReservationDto Map((Reservation Reservation, Hotel Hotel) v) => new(v.Reservation.ReservationUid, v.Reservation.Username, v.Reservation.PaymentUid, Map(v.Hotel), v.Reservation.Status, v.Reservation.StartDate, v.Reservation.EndDate);
    public async Task<HotelPageDto> GetHotelsAsync(int page, int size, CancellationToken ct) { var x = await repository.GetHotelsAsync(page, size, ct); return new(page, x.Items.Count, x.Total, x.Items.Select(Map).ToArray()); }
    public async Task<HotelDto?> GetHotelAsync(Guid uid, CancellationToken ct) => await repository.GetHotelAsync(uid, ct) is { } h ? Map(h) : null;
    public async Task<IReadOnlyList<ReservationDto>> GetAllAsync(string username, CancellationToken ct) => (await repository.GetReservationsAsync(username, ct)).Select(Map).ToArray();
    public async Task<ReservationDto?> GetAsync(Guid uid, CancellationToken ct) => await repository.GetReservationAsync(uid, ct) is { } v ? Map(v) : null;
    public async Task<ReservationDto> CreateAsync(CreateReservationDto dto, CancellationToken ct) { var hotel = await repository.GetHotelAsync(dto.HotelUid, ct) ?? throw new KeyNotFoundException("Hotel not found"); var e = new Reservation(dto.ReservationUid, dto.Username, dto.PaymentUid, hotel.Id, ReservationStatus.PAID, dto.StartDate, dto.EndDate); await repository.CreateAsync(e, ct); return Map((await repository.GetReservationAsync(e.ReservationUid, ct))!.Value); }
    public Task<bool> CancelAsync(Guid uid, string username, CancellationToken ct) => repository.CancelAsync(uid, username, ct);
    public Task<bool> DeleteAsync(Guid uid, CancellationToken ct) => repository.DeleteAsync(uid, ct);
}
