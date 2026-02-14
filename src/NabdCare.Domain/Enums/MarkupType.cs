using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
[ExportTsEnum]
public enum MarkupType
{
    None,
    Percentage
}