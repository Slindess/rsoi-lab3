namespace PaymentService.Domain; public enum PaymentStatus { PAID, CANCELED } public sealed record Payment(Guid PaymentUid, PaymentStatus Status, int Price);
