using System.Collections.Generic;

public class UnitAction
{
    public UnitData Unit;
    public HexCellData MoveTo;     
    public UnitData AttackTarget;   

    public UnitAction(UnitData unit, HexCellData moveTo, UnitData attackTarget = null)
    {
        Unit = unit;
        MoveTo = moveTo;
        AttackTarget = attackTarget;
    }
}

public class TurnAction
{
    public List<UnitAction> Actions;

    public TurnAction(List<UnitAction> actions)
    {
        Actions = actions;
    }
}
