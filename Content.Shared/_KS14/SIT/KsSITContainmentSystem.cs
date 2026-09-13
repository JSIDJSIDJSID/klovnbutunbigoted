using Content.Shared.Bed.Sleep;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Morgue;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._KS14.SIT;

public sealed partial class KsSITContainmentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;
    [Dependency] private StatusEffectsSystem _statusEffectsSystem = default!;

    public static readonly EntProtoId CryoProtectionEffect = "KsSITStatusEffectCryoProtection";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KsSITContainmentComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<KsSITContainmentComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<KsSITContainmentComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<KsSITContainmentComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<KsSITContainmentComponent, StorageAfterCloseEvent>(OnClosed);
        SubscribeLocalEvent<KsSITContainmentComponent, StorageAfterOpenEvent>(OnOpened);
    }

    private void OnMapInit(Entity<KsSITContainmentComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextCheck = _timing.CurTime + ent.Comp.CheckInterval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Server-only periodic refresh, same as MorgueSystem but with no soul-beep sound.
        // Catches death / mind detach / deletion while inside.
        if (_timing.ApplyingState)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<KsSITContainmentComponent, EntityStorageComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var containment, out var storage, out var appearance))
        {
            if (curTime < containment.NextCheck)
                continue;

            containment.NextCheck += containment.CheckInterval;
            CheckContents(uid, storage, appearance);
        }

        ChillOccupants(frameTime);
    }

    /// <summary>
    /// Exponentially chills every mob inside a containment unit towards its target temperature.
    /// Cold damage immunity comes from the cryo protection status effect applied on insert.
    /// </summary>
    private void ChillOccupants(float frameTime)
    {
        var query = EntityQueryEnumerator<KsSITContainmentComponent, EntityStorageComponent>();
        while (query.MoveNext(out _, out var containment, out var storage))
        {
            foreach (var contained in storage.Contents.ContainedEntities)
            {
                if (!HasComp<MobStateComponent>(contained))
                    continue;

                if (!TryComp<TemperatureComponent>(contained, out var temperature))
                    continue;

                var current = temperature.CurrentTemperature;
                var target = containment.TargetTemperature;
                if (MathF.Abs(current - target) < 0.1f)
                    continue;

                // Frame-rate independent exponential approach.
                var factor = 1f - MathF.Exp(-containment.CoolingRate * frameTime);
                var heatCapacity = _temperature.GetHeatCapacity(contained, temperature);
                var heat = (target - current) * heatCapacity * factor;
                _temperature.ChangeHeat(contained, heat, ignoreHeatResistance: true, temperature);
            }
        }
    }

    private void OnInserted(Entity<KsSITContainmentComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != SharedEntityStorageSystem.ContainerName)
            return;

        if (!HasComp<MobStateComponent>(args.Entity))
            return;

        _statusEffectsSystem.TrySetStatusEffectDuration(args.Entity, SleepingSystem.StatusEffectForcedSleeping);
        _statusEffectsSystem.TrySetStatusEffectDuration(args.Entity, CryoProtectionEffect);
        CheckContents(ent.Owner);
    }

    private void OnRemoved(Entity<KsSITContainmentComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != SharedEntityStorageSystem.ContainerName)
            return;

        if (!HasComp<MobStateComponent>(args.Entity))
            return;

        _statusEffectsSystem.TryRemoveStatusEffect(args.Entity, SleepingSystem.StatusEffectForcedSleeping);
        _statusEffectsSystem.TryRemoveStatusEffect(args.Entity, CryoProtectionEffect);

        // Rewarm so they don't immediately take cold damage outside.
        if (TryComp<TemperatureComponent>(args.Entity, out var temperature))
        {
            var heatCapacity = _temperature.GetHeatCapacity(args.Entity, temperature);
            var heat = (ent.Comp.RewarmTemperature - temperature.CurrentTemperature) * heatCapacity;
            _temperature.ChangeHeat(args.Entity, heat, ignoreHeatResistance: true, temperature);
        }

        CheckContents(ent.Owner);
    }

    private void OnExamine(Entity<KsSITContainmentComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        _appearance.TryGetData<MorgueContents>(ent.Owner, MorgueVisuals.Contents, out var contents);

        var text = contents switch
        {
            MorgueContents.HasSoul => "morgue-entity-storage-component-on-examine-details-body-has-soul",
            MorgueContents.HasContents => "morgue-entity-storage-component-on-examine-details-has-contents",
            MorgueContents.HasMob => "morgue-entity-storage-component-on-examine-details-body-has-no-soul",
            _ => "morgue-entity-storage-component-on-examine-details-empty"
        };

        args.PushMarkup(Loc.GetString(text));
    }

    private void OnClosed(Entity<KsSITContainmentComponent> ent, ref StorageAfterCloseEvent args)
    {
        CheckContents(ent.Owner);
    }

    private void OnOpened(Entity<KsSITContainmentComponent> ent, ref StorageAfterOpenEvent args)
    {
        CheckContents(ent.Owner);
    }

    /// <summary>
    /// Updates appearance data, same visuals as a morgue but with no soul-beep sound.
    /// </summary>
    public void CheckContents(EntityUid uid, EntityStorageComponent? storage = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref storage, ref appearance))
            return;

        if (storage.Contents.ContainedEntities.Count == 0)
        {
            _appearance.SetData(uid, MorgueVisuals.Contents, MorgueContents.Empty, appearance);
            return;
        }

        var hasMob = false;

        foreach (var contained in storage.Contents.ContainedEntities)
        {
            if (!hasMob && HasComp<MobStateComponent>(contained))
                hasMob = true;

            if (HasComp<ActorComponent>(contained))
            {
                _appearance.SetData(uid, MorgueVisuals.Contents, MorgueContents.HasSoul, appearance);
                return;
            }
        }

        _appearance.SetData(uid, MorgueVisuals.Contents, hasMob ? MorgueContents.HasMob : MorgueContents.HasContents, appearance);
    }
}
