namespace LoyaltyService.Domain; public enum LoyaltyStatus { BRONZE, SILVER, GOLD } public sealed record Loyalty(string Username, int ReservationCount, LoyaltyStatus Status, int Discount);
