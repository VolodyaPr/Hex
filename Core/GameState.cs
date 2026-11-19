using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public Dictionary<Vector2Int, HexCellData> Grid = new Dictionary<Vector2Int, HexCellData>();
    public List<UnitData> Units = new List<UnitData>();
    public UnitData ActiveUnit;

    private static readonly Vector2Int[] EvenRowDirections =
    {
        new Vector2Int(+1,  0), new Vector2Int( 0, -1), new Vector2Int(-1, -1),
        new Vector2Int(-1,  0), new Vector2Int(-1, +1), new Vector2Int( 0, +1)
    };

    private static readonly Vector2Int[] OddRowDirections =
    {
        new Vector2Int(+1,  0), new Vector2Int(+1, -1), new Vector2Int( 0, -1),
        new Vector2Int(-1,  0), new Vector2Int( 0, +1), new Vector2Int(+1, +1)
    };

    public GameState Clone()
    {
        var newState = new GameState();
        var cellMap = new Dictionary<HexCellData, HexCellData>(Grid.Count);

        foreach (var kvp in Grid)
        {
            var clonedCell = new HexCellData(kvp.Value.Q, kvp.Value.R);
            newState.Grid[kvp.Key] = clonedCell;
            cellMap[kvp.Value] = clonedCell;
        }

        foreach (var unit in Units)
        {
            if (!unit.IsAlive) continue;
            var clonedUnit = unit.Clone();
            if (unit.Cell != null && cellMap.TryGetValue(unit.Cell, out var clonedCell))
            {
                clonedUnit.Cell = clonedCell;
                clonedCell.OccupiedUnit = clonedUnit;
            }
            newState.Units.Add(clonedUnit);
        }

        if (ActiveUnit != null)
            newState.ActiveUnit = newState.Units.Find(u => u.Id == ActiveUnit.Id);

        return newState;
    }

    

    public Dictionary<HexCellData, int> GetReachableCellsWithCosts(UnitData unit)
    {
        var reachable = new Dictionary<HexCellData, int>();
        if (unit == null || unit.Cell == null || unit.RemainingMovement <= 0)
            return reachable;

        var queue = new Queue<(HexCellData cell, int cost)>();
        queue.Enqueue((unit.Cell, 0));
        reachable[unit.Cell] = 0;

        while (queue.Count > 0)
        {
            var (current, cost) = queue.Dequeue();
            if (cost >= unit.RemainingMovement) continue;

            var directions = (current.R & 1) == 0 ? EvenRowDirections : OddRowDirections;
            foreach (var dir in directions)
            {
                var neighborCoord = current.Coord + dir;
                if (!Grid.TryGetValue(neighborCoord, out var neighbor)) continue;
                if (neighbor.IsOccupied) continue;

                int newCost = cost + 1;
                if (!reachable.ContainsKey(neighbor) || newCost < reachable[neighbor])
                {
                    reachable[neighbor] = newCost;
                    queue.Enqueue((neighbor, newCost));
                }
            }
        }

        reachable.Remove(unit.Cell);
        return reachable;
    }

    public bool ApplyUnitAction(UnitAction action, Dictionary<HexCellData, int> precomputedCosts = null)
    {
        var unit = action.Unit;
        if (unit == null || !unit.IsAlive) return false;

        // if (unit.HasAttacked) return false;

        bool willMove = action.MoveTo != null && action.MoveTo != unit.Cell;
        int moveCost = 0;

        if (willMove)
        {
            if (action.MoveTo.IsOccupied) return false;
            var costs = precomputedCosts ?? GetReachableCellsWithCosts(unit);
            if (!costs.TryGetValue(action.MoveTo, out moveCost)) return false;
        }

        if (action.AttackTarget != null)
        {
            if (unit.HasAttacked || !action.AttackTarget.IsAlive) return false;
            var checkCell = willMove ? action.MoveTo : unit.Cell;
            if (checkCell.DistanceTo(action.AttackTarget.Cell) > unit.AttackRange) return false;
        }

        if (willMove)
        {
            unit.Cell.OccupiedUnit = null;
            unit.Cell = action.MoveTo;
            unit.Cell.OccupiedUnit = unit;
            unit.RemainingMovement -= moveCost;
        }

        if (action.AttackTarget != null)
        {
            action.AttackTarget.Health -= unit.AttackDamage;
            unit.HasAttacked = true;
            unit.RemainingMovement = 0; 

            if (!action.AttackTarget.IsAlive)
            {
                if (action.AttackTarget.Cell != null)
                    action.AttackTarget.Cell.OccupiedUnit = null;
                Units.Remove(action.AttackTarget);
            }
        }

        return true;
    }

    public void ApplyTurn(TurnAction turn)
    {
        if (turn == null) return;
        foreach (var action in turn.Actions)
        {
            var unit = Units.Find(u => u.Id == action.Unit.Id);
            if (unit == null || !unit.IsAlive) continue;

            HexCellData targetCell = action.MoveTo != null && Grid.TryGetValue(action.MoveTo.Coord, out var c) ? c : null;
            UnitData targetEnemy = action.AttackTarget != null ? Units.Find(u => u.Id == action.AttackTarget.Id) : null;

            ApplyUnitAction(new UnitAction(unit, targetCell, targetEnemy));
        }
    }

    public List<UnitAction> GetPossibleUnitActions(UnitData unit, bool allowWait = true)
    {
        var actions = new List<UnitAction>();
        if (!unit.IsAlive) return actions;

        var reachableCosts = GetReachableCellsWithCosts(unit);
        var candidateCells = new List<HexCellData>(reachableCosts.Keys) { unit.Cell };

        foreach (var dest in candidateCells)
        {
            // Движение без атаки
            if (dest != unit.Cell)
                actions.Add(new UnitAction(unit, dest, null));

            // Атака
            if (!unit.HasAttacked)
            {
                foreach (var enemy in Units)
                {
                    if (enemy.IsAlive && unit.IsEnemy(enemy) &&
                        dest.DistanceTo(enemy.Cell) <= unit.AttackRange)
                    {
                        actions.Add(new UnitAction(unit, dest, enemy));
                    }
                }
            }
        }

        // Ожидание только если разрешено
        if (allowWait)
        {
            bool hasWait = actions.Exists(a => a.MoveTo == unit.Cell && a.AttackTarget == null);
            if (!hasWait) actions.Add(new UnitAction(unit, unit.Cell, null));
        }

        return actions;
    }

    public void StartTurnForUnit(UnitData unit)
    {
        ActiveUnit = unit;
        if (unit != null && unit.IsAlive)
        {
            unit.ResetTurnState();
        }
    }

    public bool IsTerminal()
    {
        bool playerAlive = Units.Exists(u => u.Team == Team.Player && u.IsAlive);
        bool enemyAlive = Units.Exists(u => u.Team == Team.Enemy && u.IsAlive);
        return !playerAlive || !enemyAlive;
    }

    public List<HexCellData> FindPath(HexCellData start, HexCellData target)
    {
        var path = new List<HexCellData>();
        if (start == null || target == null) return path;

        if (start == target)
        {
            path.Add(start);
            return path;
        }

        var cameFrom = new Dictionary<HexCellData, HexCellData>();
        var queue = new Queue<HexCellData>();

        queue.Enqueue(start);
        cameFrom[start] = null; 

        bool found = false;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == target)
            {
                found = true;
                break;
            }

            var directions = (current.R & 1) == 0 ? EvenRowDirections : OddRowDirections;

            foreach (var dir in directions)
            {
                var neighborCoord = current.Coord + dir;
                if (!Grid.TryGetValue(neighborCoord, out var neighbor)) continue;

                if (neighbor.IsOccupied && neighbor != target) continue;

                if (!cameFrom.ContainsKey(neighbor))
                {
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (!found) return path; 

        var step = target;
        while (step != null)
        {
            path.Add(step);
            cameFrom.TryGetValue(step, out step);
        }

        path.Reverse();
        return path;
    }

}
