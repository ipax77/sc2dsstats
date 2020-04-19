using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using paxgamelib.Models;

namespace paxgamelib.Data
{
    public class AbilityPool
    {
        public static List<UnitAbility> Abilities = new List<UnitAbility>();
        public static List<IUnitAbility> IAbilities = new List<IUnitAbility>();

        public static Dictionary<UnitRace, List<UnitAbility>> AbilitiesAvailable = new Dictionary<UnitRace, List<UnitAbility>>();
        public static void Init()
        {
            //Upgrades = JsonSerializer.Deserialize<List<Upgrade>>(File.ReadAllText("/data/upgrades.json"));
            Build();
        }

        public static void PoolInit()
        {
            foreach (UnitRace race in Enum.GetValues(typeof(UnitRace)))
            {
                HashSet<UnitAbilities> abilities = new HashSet<UnitAbilities>();
                foreach (Unit unit in UnitPool.Units.Where(x => x.Race == race && x.Cost > 0))
                    foreach (UnitAbility ability in unit.Abilities.Where(x => x.Cost > 0))
                        abilities.Add(ability.Ability);

                AbilitiesAvailable[race] = new List<UnitAbility>();
                AbilitiesAvailable[race].Add(Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.Tier2));
                AbilitiesAvailable[race].Add(Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.Tier3));
                foreach (UnitAbilities unitability in abilities)
                    AbilitiesAvailable[race].Add(AbilityPool.Abilities.Single(x => x.Ability == unitability));
            }
        }


        public static void Build()
        {
            UnitAbility stim = new UnitAbility();
            stim.Ability = UnitAbilities.Stimpack;
            stim.Duration = new TimeSpan(0, 0, 0, 11, 0);
            stim.AttacSpeedModifier = 0.667f;
            stim.Healthmodifier = -10; // -20 for marauder ...
            stim.MoveSpeedModifier = 1.5f;
            stim.Type.Add(UnitAbilityTypes.Attacspeed);
            stim.Type.Add(UnitAbilityTypes.Healthbar);
            stim.Type.Add(UnitAbilityTypes.Speed);
            stim.isActive = false;
            stim.Tier = 1;
            stim.Triggers.Add(UnitAbilityTrigger.FightStart);
            stim.Cost = 100;

            UnitAbility shield = new UnitAbility();
            shield.Ability = UnitAbilities.CombatShield;
            shield.Type.Add(UnitAbilityTypes.Healthpoints);
            shield.Type.Add(UnitAbilityTypes.Image);
            shield.Healthmodifier = 10;
            shield.Tier = 1;
            shield.Image = "images/pax_marine_shield_tiny.png";
            shield.isActive = false;
            shield.Triggers.Add(UnitAbilityTrigger.Always);
            shield.Cost = 50;

            UnitAbility z1 = new UnitAbility();
            z1.Ability = UnitAbilities.MetabolicBoost;
            z1.Type.Add(UnitAbilityTypes.Speed);
            z1.MoveSpeedModifier = 1.6f;
            z1.Tier = 1;
            z1.isActive = false;
            z1.Triggers.Add(UnitAbilityTrigger.Always);
            z1.Cost = 125;

            UnitAbility z2 = new UnitAbility();
            z2.Ability = UnitAbilities.AdrenalGlands;
            z2.Type.Add(UnitAbilityTypes.Attacspeed);
            z2.AttacSpeedModifier = 0.84339f;
            z2.Tier = 3;
            z2.isActive = false;
            z2.Triggers.Add(UnitAbilityTrigger.Always);
            z2.Cost = 75;

            UnitAbility z3 = new UnitAbility();
            z3.Ability = UnitAbilities.Regeneration;
            z3.Type.Add(UnitAbilityTypes.Healthpoints);
            z3.Regeneration = 0.38f;
            z3.isActive = false;
            z3.Tier = 0;
            z3.Triggers.Add(UnitAbilityTrigger.Always);
            z3.Cost = 0;

            UnitAbility p = new UnitAbility();
            p.Ability = UnitAbilities.ShieldRegeneration;
            p.Type.Add(UnitAbilityTypes.Shieldpoints);
            p.Regeneration = 2;
            p.Cooldown = new TimeSpan(0, 0, 10) / paxgame.Battlefieldmodifier;
            p.isActive = false;
            p.Tier = 0;
            p.Triggers.Add(UnitAbilityTrigger.Always);
            p.Cost = 0;

            UnitAbility s = new UnitAbility();
            s.Ability = UnitAbilities.Suicide;
            s.Type.Add(UnitAbilityTypes.Attacdamage);
            s.AttacDamageModifier = 0;
            s.isActive = false;
            s.Tier = 0;
            s.Triggers.Add(UnitAbilityTrigger.FightEnd);
            s.Cost = 0;

            UnitAbility s1 = new UnitAbility();
            s1.Ability = UnitAbilities.SuicideOnDeath;
            s1.Type.Add(UnitAbilityTypes.Suicide);
            s1.isActive = false;
            s1.Tier = 0;
            s1.Triggers.Add(UnitAbilityTrigger.OnDeath);
            s1.Cost = 0;

            UnitAbility c = new UnitAbility();
            c.Ability = UnitAbilities.ConcussiveShells;
            c.Type.Add(UnitAbilityTypes.Speed);
            c.MoveSpeedModifier = 0.5f;
            c.isActive = false;
            c.Tier = 1;
            c.Triggers.Add(UnitAbilityTrigger.FightStart);
            c.Cost = 25;
            c.CastOnEnemy = true;

            UnitAbility ConcussiveShellsModifier = new UnitAbility();
            ConcussiveShellsModifier.Ability = UnitAbilities.ConcussiveShellsModifier;
            ConcussiveShellsModifier.Type.Add(UnitAbilityTypes.Speed);
            ConcussiveShellsModifier.MoveSpeedModifier = 0.5f;
            ConcussiveShellsModifier.isActive = false;
            ConcussiveShellsModifier.Tier = 0;
            ConcussiveShellsModifier.Triggers.Add(UnitAbilityTrigger.Always);
            ConcussiveShellsModifier.Cost = 0;
            ConcussiveShellsModifier.Duration = new TimeSpan(0, 0, 0, 1, 78);

            UnitAbility k = new UnitAbility();
            k.Ability = UnitAbilities.KD8Charge;
            k.Type.Add(UnitAbilityTypes.Pos);
            k.PosModifier = 2;
            k.Tier = 1;
            k.Triggers.Add(UnitAbilityTrigger.FightStart);
            k.Cost = 0;
            k.Cooldown = new TimeSpan(0, 0, 0, 14);
            k.isActive = false;

            UnitAbility k1 = new UnitAbility();
            k1.Ability = UnitAbilities.KnockBack;
            k1.Type.Add(UnitAbilityTypes.Pos);
            k1.PosModifier = 1 / paxgame.Battlefieldmodifier;
            k1.Radius = 2 / (paxgame.Battlefieldmodifier / 2);
            k1.Tier = 1;
            k1.Triggers.Add(UnitAbilityTrigger.FightEnd);
            k1.Cost = 0;
            k1.isActive = false;

            UnitAbility k2 = new UnitAbility();
            k2.Ability = UnitAbilities.Explode;
            k2.Type.Add(UnitAbilityTypes.Suicide);
            k2.Tier = 1;
            k2.Triggers.Add(UnitAbilityTrigger.Always);
            k2.Cost = 0;
            k2.isActive = false;

            UnitAbility blink = new UnitAbility();
            blink.Ability = UnitAbilities.Blink;
            blink.Type.Add(UnitAbilityTypes.Pos);
            blink.isActive = false;
            blink.Tier = 1;
            blink.Triggers.Add(UnitAbilityTrigger.FightStart);
            blink.Cooldown = new TimeSpan(0, 0, 0, 7);
            blink.Cost = 125;

            UnitAbility PsionicTransfer = new UnitAbility();
            PsionicTransfer.Ability = UnitAbilities.PsionicTransfer;
            PsionicTransfer.Type.Add(UnitAbilityTypes.Pos);
            PsionicTransfer.Duration = new TimeSpan(0, 0, 0, 7, 0) / paxgame.Battlefieldmodifier;
            PsionicTransfer.isActive = false;
            PsionicTransfer.Tier = 1;
            PsionicTransfer.Triggers.Add(UnitAbilityTrigger.FightStart);
            PsionicTransfer.Cooldown = new TimeSpan(0, 0, 11);
            PsionicTransfer.Cost = 0;

            UnitAbility ResonatingGlaives = new UnitAbility();
            ResonatingGlaives.Ability = UnitAbilities.ResonatingGlaives;
            ResonatingGlaives.AttacSpeedModifier = 0.5f;
            ResonatingGlaives.Type.Add(UnitAbilityTypes.Attacspeed);
            ResonatingGlaives.isActive = false;
            ResonatingGlaives.Tier = 1;
            ResonatingGlaives.Triggers.Add(UnitAbilityTrigger.Always);
            ResonatingGlaives.Cost = 150;

            UnitAbility EnergyRegeneration = new UnitAbility();
            EnergyRegeneration.Ability = UnitAbilities.EnergyRegeneration;
            EnergyRegeneration.Type.Add(UnitAbilityTypes.Energy);
            EnergyRegeneration.Regeneration = 0.7875f;
            EnergyRegeneration.isActive = false;
            EnergyRegeneration.Tier = 0;
            EnergyRegeneration.Triggers.Add(UnitAbilityTrigger.Always);
            EnergyRegeneration.Cost = 0;

            UnitAbility GuardianShield = new UnitAbility();
            GuardianShield.Ability = UnitAbilities.GuardianShield;
            GuardianShield.Type.Add(UnitAbilityTypes.Mitigation);
            GuardianShield.Type.Add(UnitAbilityTypes.Beacon);
            GuardianShield.Tier = 1;
            GuardianShield.Duration = new TimeSpan(0, 0, 0, 11, 0);
            GuardianShield.Triggers.Add(UnitAbilityTrigger.EnemyInVision);
            GuardianShield.Triggers.Add(UnitAbilityTrigger.OnDeath);
            GuardianShield.Radius = 4.5f;
            GuardianShield.Cost = 0;
            GuardianShield.EnergyCost = 75;
            GuardianShield.isActive = false;

            UnitAbility GuardianShieldMitigation = new UnitAbility();
            GuardianShieldMitigation.Ability = UnitAbilities.GuardianShieldMitigation;
            GuardianShieldMitigation.Type.Add(UnitAbilityTypes.Mitigation);
            GuardianShieldMitigation.Type.Add(UnitAbilityTypes.Aura);
            GuardianShieldMitigation.Tier = 0;
            GuardianShieldMitigation.Radius = 4.5f;
            GuardianShieldMitigation.Triggers.Add(UnitAbilityTrigger.OnHitStart);
            GuardianShieldMitigation.isActive = false;

            UnitAbility GuardianShieldAttacModifier = new UnitAbility();
            GuardianShieldAttacModifier.Ability = UnitAbilities.GuardianShieldAttacModifier;
            GuardianShieldAttacModifier.Type.Add(UnitAbilityTypes.Attacdamage);
            GuardianShieldAttacModifier.Type.Add(UnitAbilityTypes.Aura);
            GuardianShieldAttacModifier.Tier = 0;
            GuardianShieldAttacModifier.Triggers.Add(UnitAbilityTrigger.FightStart);
            GuardianShieldAttacModifier.Triggers.Add(UnitAbilityTrigger.FightEnd);
            GuardianShieldAttacModifier.AttacDamageModifier = -2;
            GuardianShieldAttacModifier.isActive = false;

            UnitAbility Haluzination = new UnitAbility();
            Haluzination.Ability = UnitAbilities.Haluzination;
            Haluzination.Type.Add(UnitAbilityTypes.Decoy);
            Haluzination.Cooldown = new TimeSpan(0, 0, 0, 11);
            Haluzination.Tier = 0;
            Haluzination.Triggers.Add(UnitAbilityTrigger.EnemyInVision);
            Haluzination.Cost = 0;
            Haluzination.EnergyCost = 75;
            Haluzination.isActive = false;

            UnitAbility Charge = new UnitAbility();
            Charge.Ability = UnitAbilities.Charge;
            Charge.Type.Add(UnitAbilityTypes.Speed);
            Charge.Type.Add(UnitAbilityTypes.Attacdamage);
            Charge.Tier = 1;
            Charge.Duration = new TimeSpan(0, 0, 0, 2, 500);
            Charge.Triggers.Add(UnitAbilityTrigger.EnemyInVision);
            Charge.Radius = 4;
            Charge.MoveSpeedModifier = 9.1f;
            Charge.Cooldown = new TimeSpan(0, 0, 7);
            Charge.Cost = 50;
            Charge.Tandem = new List<UnitAbilities>();
            Charge.Tandem.Add(UnitAbilities.ChargeBase);
            Charge.isActive = false;

            UnitAbility ChargeImpact = new UnitAbility();
            ChargeImpact.Ability = UnitAbilities.SunderingImpact;
            ChargeImpact.Type.Add(UnitAbilityTypes.Attacdamage);
            ChargeImpact.Tier = 1;
            ChargeImpact.AttacDamageModifier = 8;
            ChargeImpact.Triggers.Add(UnitAbilityTrigger.FightStart);
            ChargeImpact.Cost = 50;
            ChargeImpact.isActive = false;


            UnitAbility ChargeBase = new UnitAbility();
            ChargeBase.Ability = UnitAbilities.ChargeBase;
            ChargeBase.Type.Add(UnitAbilityTypes.Speed);
            ChargeBase.MoveSpeedModifier = 1.31f;
            ChargeBase.Tier = 1;
            ChargeBase.isActive = false;
            ChargeBase.Triggers.Add(UnitAbilityTrigger.Always);
            ChargeBase.Cost = 0;

            UnitAbility LifeTime = new UnitAbility();
            LifeTime.Ability = UnitAbilities.LifeTime;
            LifeTime.Type.Add(UnitAbilityTypes.Suicide);
            LifeTime.Tier = 0;
            LifeTime.Cost = 0;
            LifeTime.Duration = new TimeSpan(0, 0, 0, 43) / paxgame.Battlefieldmodifier;
            LifeTime.Triggers.Add(UnitAbilityTrigger.Always);
            LifeTime.isActive = false;

            UnitAbility Transfusion = new UnitAbility();
            Transfusion.Ability = UnitAbilities.Transfusion;
            Transfusion.Type.Add(UnitAbilityTypes.Healthbar);
            Transfusion.Tier = 0;
            Transfusion.Cost = 0;
            Transfusion.EnergyCost = 50;
            Transfusion.Cooldown = new TimeSpan(0, 0, 0, 1);
            Transfusion.CastOnEnemy = true;
            Transfusion.Healthmodifier = 75;
            Transfusion.Radius = 7;
            Transfusion.Triggers.Add(UnitAbilityTrigger.AllyInRange);
            Transfusion.isActive = false;

            UnitAbility TransfusionRegeneration = new UnitAbility();
            TransfusionRegeneration.Ability = UnitAbilities.TransfusionRegeneration;
            TransfusionRegeneration.Type.Add(UnitAbilityTypes.Healthpoints);
            TransfusionRegeneration.Duration = new TimeSpan(0, 0, 0, 7);
            TransfusionRegeneration.Regeneration = 50 / ((float)TransfusionRegeneration.Duration.TotalSeconds * paxgame.Battlefieldmodifier);
            TransfusionRegeneration.isActive = false;
            TransfusionRegeneration.Tier = 0;
            TransfusionRegeneration.Cost = 0;
            TransfusionRegeneration.Triggers.Add(UnitAbilityTrigger.Always);
            TransfusionRegeneration.isActive = false;

            UnitAbility CentrifugalHooks = new UnitAbility();
            CentrifugalHooks.Ability = UnitAbilities.CentrifugalHooks;
            CentrifugalHooks.Type.Add(UnitAbilityTypes.Healthbar);
            CentrifugalHooks.Type.Add(UnitAbilityTypes.Speed);
            CentrifugalHooks.Healthmodifier = 5;
            CentrifugalHooks.MoveSpeedModifier = 1.18f;
            CentrifugalHooks.Type.Add(UnitAbilityTypes.Healthpoints);
            CentrifugalHooks.Type.Add(UnitAbilityTypes.Speed);
            CentrifugalHooks.isActive = false;
            CentrifugalHooks.Tier = 1;
            CentrifugalHooks.Triggers.Add(UnitAbilityTrigger.Always);
            CentrifugalHooks.Cost = 125;

            UnitAbility tier2 = new UnitAbility();
            tier2.Ability = UnitAbilities.Tier2;
            tier2.Cost = 250;
            tier2.Tier = 1;
            tier2.Triggers.Add(UnitAbilityTrigger.Never);

            UnitAbility tier3 = new UnitAbility();
            tier3.Ability = UnitAbilities.Tier3;
            tier3.Cost = 450;
            tier3.Tier = 2;
            tier3.Triggers.Add(UnitAbilityTrigger.Never);


            Abilities.Add(stim);
            Abilities.Add(shield);
            Abilities.Add(z1);
            Abilities.Add(z2);
            Abilities.Add(z3);
            Abilities.Add(p);
            Abilities.Add(s);
            Abilities.Add(s1);
            Abilities.Add(c);
            Abilities.Add(ConcussiveShellsModifier);
            Abilities.Add(k);
            Abilities.Add(k1);
            Abilities.Add(k2);
            Abilities.Add(blink);
            Abilities.Add(PsionicTransfer);
            Abilities.Add(ResonatingGlaives);
            Abilities.Add(EnergyRegeneration);
            Abilities.Add(GuardianShield);
            Abilities.Add(GuardianShieldMitigation);
            Abilities.Add(GuardianShieldAttacModifier);
            Abilities.Add(Haluzination);
            Abilities.Add(LifeTime);
            Abilities.Add(Transfusion);
            Abilities.Add(TransfusionRegeneration);
            Abilities.Add(CentrifugalHooks);
            Abilities.Add(Charge);
            Abilities.Add(ChargeBase);
            Abilities.Add(ChargeImpact);
            Abilities.Add(tier2);
            Abilities.Add(tier3);

            IAbilities.AddRange(Abilities);

        }

        public static UnitAbility Map (string abString)
        {
            UnitAbility myability = null;

            UnitAbilities myabilities = UnitAbilities.AdrenalGlands;

            if (abString == "AdeptPiercingAttack")
                myabilities = UnitAbilities.ResonatingGlaives;
            else if (abString == "BlinkTech")
                myabilities = UnitAbilities.Blink;
            else if (abString == "Stimpack")
                myabilities = UnitAbilities.Stimpack;
            else if (abString == "ShieldWall")
                myabilities = UnitAbilities.CombatShield;
            else if (abString == "zerglingmovementspeed")
                myabilities = UnitAbilities.MetabolicBoost;
            else if (abString == "zerglingattackspeed")
                myabilities = UnitAbilities.AdrenalGlands;
            else if (abString == "CentrificalHooks")
                myabilities = UnitAbilities.CentrifugalHooks;
            else if (abString == "PunisherGrenades")
                myabilities = UnitAbilities.ConcussiveShells;
            else if (abString == "Charge")
                myabilities = UnitAbilities.Charge;
            else if (abString == "Tier2")
                myabilities = UnitAbilities.Tier2;
            else if (abString == "Tier3")
                myabilities = UnitAbilities.Tier3;
            else
                return null;

            myability = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == myabilities).DeepCopy();

            return myability;
        }
    }
}
