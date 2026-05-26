namespace MangoTaika.Services;

public interface IMobilePaymentGateway
{
    Task<MobilePaymentResult> CreateRequestAsync(MobilePaymentRequest request, CancellationToken cancellationToken = default);
}

public sealed record MobilePaymentRequest(
    string OperationType,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    decimal Amount,
    string Currency,
    string InternalReference,
    string? CustomerReference);

public sealed record MobilePaymentResult(
    bool IsAutomatic,
    bool IsAccepted,
    string Status,
    string? ProviderReference,
    string Message);

public sealed class ManualMobilePaymentGateway(IConfiguration configuration) : IMobilePaymentGateway
{
    public Task<MobilePaymentResult> CreateRequestAsync(MobilePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var provider = configuration["MobilePayment:Provider"] ?? "Manual";
        if (!string.Equals(provider, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new MobilePaymentResult(
                IsAutomatic: false,
                IsAccepted: false,
                Status: "ProviderNotConfigured",
                ProviderReference: null,
                Message: $"Le fournisseur de paiement mobile '{provider}' n'est pas encore connecte. Configurez une implementation IMobilePaymentGateway dediee."));
        }

        return Task.FromResult(new MobilePaymentResult(
            IsAutomatic: false,
            IsAccepted: true,
            Status: string.IsNullOrWhiteSpace(request.CustomerReference) ? "ReferenceAttendue" : "ReferenceDeclaree",
            ProviderReference: request.CustomerReference,
            Message: string.IsNullOrWhiteSpace(request.CustomerReference)
                ? "Paiement mobile manuel : la reference sera rapprochee par l'equipe finance."
                : "Reference paiement enregistree pour rapprochement."));
    }
}
