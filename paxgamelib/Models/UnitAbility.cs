using paxgamelib.Data;
using paxgamelib.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace paxgamelib.Models
{
    [Serializable]
    public class UnitAbility : IUnitAbility
    {
        public UnitAbilities Ability { get; set; }
        public DateTime Activated { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public TimeSpan Cooldown { get; set; } = TimeSpan.Zero;
        public ICollection<UnitAbilityTypes> Type { get; set; } = new Collection<UnitAbilityTypes>();
        public float Regeneration { get; set; }
        public bool isActive { get; set; } = false;
        public float AttacSpeedModifier { get; set; } = 0;
        public float AttacDamageModifier { get; set; } = 0;
        public float Healthmodifier { get; set; } = 0;
        public float MoveSpeedModifier { get; set; } = 0;
        public float PosModifier { get; set; } = 0;
        public float Radius { get; set; } = 0;
        public Vector2 TargetPos { get; set; } = Vector2.Zero;
        public int Tier { get; set; } = 0;
        public string Image { get; set; }
        //public UnitAbilityTrigger Trigger { get; set; } = UnitAbilityTrigger.Always;
        public List<UnitAbilityTrigger> Triggers { get; set; } = new List<UnitAbilityTrigger>();
        public float EnergyCost { get; set; } = 0;
        public int Cost { get; set; } = 0;
        public bool CastOnEnemy { get; set; } = false;
        public Unit Beacon { get; set; }
        public bool Deactivated { get; set; } = false;
        public string Desc { get; set; } = "";
        public List<UnitAbilities> Tandem { get; set; }

        public UnitAbility()
        {

        }

        public object Shallowcopy()
        {
            return this.MemberwiseClone();
        }

        public UnitAbility DeepCopy()
        {
            UnitAbility deepcopy = new UnitAbility();
            deepcopy.Ability = Ability;
            deepcopy.Activated = Activated;
            deepcopy.Duration = Duration;
            deepcopy.Cooldown = Cooldown;
            deepcopy.Type = new List<UnitAbilityTypes>(Type);
            deepcopy.Regeneration = Regeneration;
            deepcopy.isActive = isActive;
            deepcopy.AttacSpeedModifier = AttacSpeedModifier;
            deepcopy.AttacDamageModifier = AttacDamageModifier;
            deepcopy.Healthmodifier = Healthmodifier;
            deepcopy.MoveSpeedModifier = MoveSpeedModifier;
            deepcopy.PosModifier = PosModifier;
            deepcopy.Radius = Radius;
            deepcopy.TargetPos = TargetPos;
            deepcopy.Tier = Tier;
            deepcopy.Image = Image;
            deepcopy.Triggers = Triggers;
            deepcopy.EnergyCost = EnergyCost;
            deepcopy.Cost = Cost;
            deepcopy.CastOnEnemy = CastOnEnemy;
            if (Beacon != null)
                deepcopy.Beacon = Beacon.DeepCopy();
            deepcopy.Deactivated = Deactivated;
            deepcopy.Desc = Desc;
            if (Tandem != null)
                deepcopy.Tandem = new List<UnitAbilities>(Tandem);
            return deepcopy;
        }

        public void Activate(Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies, List<Unit> allies = null)
        {
            if (Ability == UnitAbilities.Stimpack)
                AbilityDeActivateService.ActivateStimpack(this, unit, battlefield);

            else if (Ability == UnitAbilities.Suicide)
                AbilityDeActivateService.ActivateSuicide(this, unit);

            else if (Ability == UnitAbilities.SuicideOnDeath)
                AbilityDeActivateService.ActivateSuicideOnDeath(this, unit, battlefield, enemies);

            else if (Ability == UnitAbilities.ConcussiveShells)
                AbilityDeActivateService.ActivateConcussiveShells(unit);

            else if (Ability == UnitAbilities.KD8Charge)
                AbilityDeActivateService.ActivateKD8Charge(this, unit, vs, battlefield, enemies);

            else if (Ability == UnitAbilities.KnockBack)
                AbilityDeActivateService.ActivateKnockBack(this, unit, vs, battlefield, enemies);

            else if (Ability == UnitAbilities.Blink)
                AbilityDeActivateService.ActivateBlink(this, unit, vs, battlefield, enemies);

            else if (Ability == UnitAbilities.PsionicTransfer)
                AbilityDeActivateService.ActivatePsionicTransfer(this, unit, vs, battlefield, enemies);

            else if (Ability == UnitAbilities.ResonatingGlaives)
                AbilityDeActivateService.ActivateResonatingGlaives(this, unit, vs, battlefield, enemies);
 
            else if (Ability == UnitAbilities.GuardianShield)
                AbilityDeActivateService.ActivateGuardianShield(this, unit, vs, battlefield, enemies);

            else if (Ability == UnitAbilities.GuardianShieldMitigation)
                AbilityDeActivateService.ActivateGuardianShieldMitigation(this, unit, vs, battlefield, enemies);

            else if (Ability == UnitAbilities.GuardianShieldAttacModifier)
                AbilityDeActivateService.ActivateGuardianShieldAttacModifier(this, unit, vs, battlefield, enemies);

            else if (Ability == UnitAbilities.Haluzination)
                AbilityDeActivateService.ActivateHaluzination(this, unit, vs, battlefield, enemies);
           
            else if (Ability == UnitAbilities.Transfusion)
                AbilityDeActivateService.ActivateTransfusion(this, unit, vs, battlefield, enemies, allies);

            else if (Ability == UnitAbilities.Charge)
                AbilityDeActivateService.ActivateCharge(this, unit, vs, battlefield);

            else if (Ability == UnitAbilities.SunderingImpact)
                AbilityDeActivateService.ActivateSunderingImpact(this, unit, vs, battlefield);

            // ??
            this.Duration = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == this.Ability).Duration;
        }

        public void Deactivate(Unit unit)
        {
            if (Ability == UnitAbilities.Stimpack)
                AbilityDeActivateService.DeactivateStimpack(this, unit);

            else if (Ability == UnitAbilities.Suicide)
                AbilityDeActivateService.DeactivateSuicide(this, unit);

            else if (Ability == UnitAbilities.ConcussiveShellsModifier)
                AbilityDeActivateService.DeactivateConcussiveShellsModifier(this, unit);

            else if (Ability == UnitAbilities.PsionicTransfer)
                AbilityDeActivateService.DeactivatePsionicTransfer(this, unit);

            else if (Ability == UnitAbilities.GuardianShield)
            {
                AbilityDeActivateService.DeactivateGuardianShield(this, unit);
            }

            else if (Ability == UnitAbilities.GuardianShieldMitigation)
            {
                AbilityDeActivateService.DeactivateGuardianShieldMitigation(this, unit);
            }

            else if (Ability == UnitAbilities.GuardianShieldAttacModifier)
            {
                AbilityDeActivateService.DeactivateGuardianShieldAttacModifier(this, unit);
            }

            else if (Ability == UnitAbilities.LifeTime)
            {
                AbilityDeActivateService.DeactivateLifeTime(this, unit);
            }

            else if (Ability == UnitAbilities.Charge)
            {
                AbilityDeActivateService.DeactivateCharge(this, unit);
            }

            else if (Ability == UnitAbilities.SunderingImpact)
            {
                AbilityDeActivateService.DeactivateSunderingImpact(this, unit);
            }

            // remove if not in init unit
            this.Cooldown = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == this.Ability).Cooldown;
            if (UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name) != null && UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Abilities.SingleOrDefault(y => y.Ability == this.Ability) == null)
                unit.Abilities.Remove(this);

        }

    }



    public enum UnitAbilities
    {
        Stimpack,
        CombatShield,
        ConcussiveShells,
        ConcussiveShellsModifier,
        AdrenalGlands,
        MetabolicBoost,
        Regeneration,
        ShieldRegeneration,
        Suicide,
        SuicideOnDeath,
        KD8Charge,
        KnockBack,
        Explode,
        Blink,
        PsionicTransfer,
        ResonatingGlaives,
        EnergyRegeneration,
        GuardianShield,
        GuardianShieldMitigation,
        GuardianShieldAttacModifier,
        Haluzination,
        LifeTime,
        Transfusion,
        TransfusionRegeneration,
        CentrifugalHooks,
        Charge,
        ChargeBase,
        SunderingImpact,
        Tier2,
        Tier3,
    }

    public enum UnitAbilityTypes
    {
        Attacspeed,
        Attacdamage,
        Attacrange,
        Armor,
        Healthbar,
        Healthpoints,
        Shieldbar,
        Shieldpoints,
        Speed,
        Suicide,
        Visionrange,
        Pos,
        Fight,
        Energy,
        Mitigation,
        Beacon,
        Aura,
        Decoy,
        Image,

    }

    public enum UnitAbilityTrigger
    {
        Act,
        EnemyInVision,
        FightStart,
        FightEnd,
        OnDeath,
        OutOfCombat,
        AllyInVision,
        TeammateInVision,
        Always,
        AllyInRange,
        OnHitStart,
        OnHitEnd,
        Never,
    }
}
