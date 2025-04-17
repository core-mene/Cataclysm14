using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Company;

/// <summary>
/// Prototype for a company that can be assigned to players.
/// </summary>
[Prototype("company")]
public sealed class CompanyPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the company.
    /// </summary>
    [DataField("name", required: true)]
    public string Name { get; private set; } = default!;

    /// <summary>
    /// The color used to display the company name in examine text.
    /// </summary>
    [DataField("color")]
    public Color Color { get; private set; } = Color.Yellow;
} 