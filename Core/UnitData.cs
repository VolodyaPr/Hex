public class UnitData
{
    public int Id;
    public string Name;
    public Team Team;
    public AttackType AttackType;

    public int Health;
    public int MaxHealth;
    public int MovementRange;
    public int AttackRange;
    public int AttackDamage;
    public int Initiative; // Чем выше, тем раньше ходит

    public int RemainingMovement;
    public bool HasAttacked;
    public HexCellData Cell;

    public bool IsAlive => Health > 0;

    public bool IsEnemy(UnitData other)
    {
        return other != null && other.Team != Team;
    }

    public void ResetTurnState()
    {
        RemainingMovement = MovementRange;
        HasAttacked = false;
    }

    public UnitData Clone()
    {
        return new UnitData
        {
            Id = Id,
            Name = Name,
            Team = Team,
            AttackType = AttackType,
            Health = Health,
            MaxHealth = MaxHealth,
            MovementRange = MovementRange,
            AttackRange = AttackRange,
            AttackDamage = AttackDamage,
            RemainingMovement = RemainingMovement,
            HasAttacked = HasAttacked,
            Initiative = Initiative,
            Cell = null // Привязывается в GameState.Clone()
        };
    }
}