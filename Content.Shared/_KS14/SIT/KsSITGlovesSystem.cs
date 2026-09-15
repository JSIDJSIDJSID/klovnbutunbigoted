using Content.Shared.Clothing;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;

namespace Content.Shared._KS14.SIT;

/// <summary>
/// KS14 - SIT infiltration gloves. Grants ninja-like abilities to the wearer when toggled,
/// powered by an internal self-recharging battery instead of a ninja suit cell.
/// Anyone can toggle, no suit or role required.
/// </summary>
public sealed partial class KsSITGlovesSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _itemToggleSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KsSITGlovesComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<KsSITGlovesComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<KsSITGlovesComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUnequipped(Entity<KsSITGlovesComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        // ToggleClothing with DisableOnUnequip deactivates first, this is a safety net.
        DisableGloves(ent);
    }

    private void OnToggled(Entity<KsSITGlovesComponent> ent, ref ItemToggledEvent args)
    {
        if ((args.User ?? ent.Comp.User) is not { } user)
            return;

        var message = Loc.GetString(args.Activated ? "ninja-gloves-on" : "ninja-gloves-off");
        _popupSystem.PopupClient(message, user, user);

        if (args.Activated)
            EnableGloves(ent, user);
        else
            DisableGloves(ent);
    }

    private void EnableGloves(Entity<KsSITGlovesComponent> ent, EntityUid user)
    {
        var (uid, comp) = ent;
        comp.User = user;
        Dirty(uid, comp);

        EntityManager.AddComponents(user, comp.Components);

        // Wire the internal battery to abilities that need power.
        if (TryComp<StunProviderComponent>(user, out var stunProvider))
        {
            stunProvider.BatteryUid = uid;
            Dirty(user, stunProvider);
        }

        if (TryComp<BatteryDrainerComponent>(user, out var batteryDrainer))
        {
            batteryDrainer.BatteryUid = uid;
            Dirty(user, batteryDrainer);
        }
    }

    private void DisableGloves(Entity<KsSITGlovesComponent> ent)
    {
        var (uid, comp) = ent;
        if (comp.User is not { } user)
            return;

        comp.User = null;
        Dirty(uid, comp);

        EntityManager.RemoveComponents(user, comp.Components);
    }

    private void OnExamined(Entity<KsSITGlovesComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var on = _itemToggleSystem.IsActivated(ent.Owner) ? "on" : "off";
        args.PushText(Loc.GetString($"ninja-gloves-examine-{on}"));
    }
}
