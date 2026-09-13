using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._KS14.SIT;

public sealed partial class KsSITCryoProtectionStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KsSITCryoProtectionStatusEffectComponent, StatusEffectRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    private void OnDamageModify(Entity<KsSITCryoProtectionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, ent.Comp.Modifiers);
    }
}
