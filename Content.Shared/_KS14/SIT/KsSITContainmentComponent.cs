using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared._KS14.SIT;

/// <summary>
/// Silent morgue replacement for the SIT containment unit.
/// Tracks mob/soul contents for visuals like <see cref="Morgue"/> does,
/// but never plays the soul-beep sound. Also applies forced sleep on insert
/// and cryogenically chills occupants (see <see cref="KsSITContainmentSystem"/>).
/// Cold damage immunity while inside comes from ContainerTemperature on the prototype,
/// same pattern as cryo pods.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KsSITContainmentComponent : Component
{
    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan NextCheck = TimeSpan.Zero;

    /// <summary>
    /// Body temperature occupants are chilled towards. Defaults to space coldness.
    /// </summary>
    [DataField]
    public float TargetTemperature = Atmospherics.TCMB;

    /// <summary>
    /// Exponential cooling rate per second towards <see cref="TargetTemperature"/>.
    /// </summary>
    [DataField]
    public float CoolingRate = 2f;

    /// <summary>
    /// Body temperature occupants are rewarmed to on exit,
    /// so they don't take cold damage afterwards.
    /// </summary>
    [DataField]
    public float RewarmTemperature = 310.15f;
}
