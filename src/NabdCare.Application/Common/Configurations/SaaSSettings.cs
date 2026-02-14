using NabdCare.Domain.Enums;

namespace NabdCare.Application.Common.Configurations;

public class SaaSSettings
{
    public const string SectionName = "SaaSSettings";
    public Currency FunctionalCurrency { get; set; } = Currency.USD;
}
