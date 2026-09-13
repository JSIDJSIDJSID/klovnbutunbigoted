using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameStates;

namespace Content.Shared._KS14.SIT;

/// <summary>
/// Status effect component marking an occupant as cryogenically protected:
/// immune to Cold damage while inside the SIT containment unit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KsSITCryoProtectionStatusEffectComponent : Component
{
    [DataField(required: true)]
    public DamageModifierSet Modifiers = default!;
}
