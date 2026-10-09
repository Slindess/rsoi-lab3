using PaymentService.Domain;
namespace PaymentService.Contracts; public sealed record CreatePaymentDto(int Price); public sealed record PaymentDto(Guid PaymentUid, PaymentStatus Status, int Price); public sealed record ErrorDto(string Message);
