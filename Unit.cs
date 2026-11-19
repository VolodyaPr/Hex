using UnityEngine;

public enum Team { Player, Enemy }
public enum AttackType { Melee, Ranged }

public class Unit : MonoBehaviour
{
    [Header("Основные характеристики")]
    [SerializeField] private string unitName = "Warrior";
    [SerializeField] private Team unitTeam = Team.Player;

    [Header("Боевые параметры")]
    [SerializeField] private AttackType attackType = AttackType.Melee;
    [SerializeField] private int movementRange = 3;
    [SerializeField] private int attackRange = 1;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int initiative = 10;

    public string UnitName => unitName;
    public Team Team => unitTeam;
    public AttackType UnitAttackType => attackType;
    public int MovementRange => movementRange;
    public int AttackRange => attackRange;
    public int AttackDamage => attackDamage;
    public int MaxHealth => maxHealth;
    public int Initiative => initiative;

    public HexCell CurrentCell { get; private set; }

    [SerializeField] private float yOffset = 0.4f;

    public void OccupyCell(HexCell targetCell)
    {
        if (CurrentCell != null) CurrentCell.SetUnit(null);
        CurrentCell = targetCell;
        targetCell.SetUnit(this);
        transform.position = targetCell.transform.position + Vector3.up * yOffset;
    }
}