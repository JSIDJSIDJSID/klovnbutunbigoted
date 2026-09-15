using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._KS14.SIT;

/// <summary>
/// KS14 - SIT infiltration gloves. Grants ninja-like abilities to the wearer when toggled,
/// powered by an internal self-recharging battery instead of a ninja suit cell.
/// Requires <c>ItemToggleComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(KsSITGlovesSystem))]
public sealed partial class KsSITGlovesComponent : Component
{
    /// <summary>
    /// Entity wearing the gloves with abilities active, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// Ability components granted to the wearer while toggled on.
    /// BatteryUid on StunProvider/BatteryDrainer is wired to the gloves entity itself.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
