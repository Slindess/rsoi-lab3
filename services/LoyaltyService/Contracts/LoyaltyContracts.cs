using LoyaltyService.Domain;
namespace LoyaltyService.Contracts; public sealed record LoyaltyDto(LoyaltyStatus Status, int Discount, int ReservationCount);
