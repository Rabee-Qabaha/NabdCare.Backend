using System.ComponentModel;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
[ExportTsEnum]
public enum Currency
{
    [Description("United States Dollar")]
    USD,
    [Description("Jordanian Dinar")]
    JOD,
    [Description("Euro")]
    EUR,
    [Description("Israeli New Shekel")]
    ILS,
    [Description("Saudi Riyal")]
    SAR,
    [Description("Kuwaiti Dinar")]
    KWD,
    [Description("Qatari Riyal")]
    QAR,
    [Description("United Arab Emirates Dirham")]
    AED
}