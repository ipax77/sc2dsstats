using System.Numerics;

namespace sc2dsstats.lib.Models
{
    public class UnitModelBase
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Race { get; set; }

        public UnitModelBase()
        {

        }

        public UnitModelBase(string name, string race)
        {
            Name = name;
            Race = race;
        }

    }

    public class UnitModelCount : UnitModelBase
    {
        public int Count { get; set; } = 0;

        public UnitModelCount(UnitModelBase unit, int count)
        {
            ID = unit.ID;
            Name = unit.Name;
            Race = unit.Race;
            Count = count;
        }
    }

    public class UnitModel : UnitModelBase
    {
        public Vector2 Pos { get; set; } = Vector2.Zero;
        public Vector2 RotatePos { get; set; } = Vector2.Zero;
        //public virtual ICollection<UnitAttribute> Attributes { get; set; }
        //public virtual ICollection<BonusDamage> BonusDamages { get; set; }
        //public virtual ICollection<AreaDamage> AreaDamages { get; set; }
    }

    public class BonusDamage
    {
        public int ID { get; set; }
        public virtual UnitAttribute Attribute { get; set; }
        public float Damage { get; set; }
        public float Modifier { get; set; }
    }

    public class AreaDamage
    {
        public int ID { get; set; }
        public float Distance1 { get; set; } = 0.4687f;
        public float Distance2 { get; set; } = 0.7812f;
        public float Distance3 { get; set; } = 1.25f;
        public bool FriendlyFire = false;
    }

    public class UnitAttribute
    {
        public string Attribute { get; set; }
    }
}
