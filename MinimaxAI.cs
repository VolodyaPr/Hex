using System.Collections.Generic;
using UnityEngine;

public class MinimaxAI
{
    private readonly Team aiTeam;

    public MinimaxAI(Team aiTeam) { this.aiTeam = aiTeam; }

    public UnitAction FindBestActionForUnit(GameState state, UnitData unit, int depth)
    {
        var possibleActions = state.GetPossibleUnitActions(unit);
        UnitAction bestAction = null;
        int bestScore = int.MinValue;

        foreach (var action in possibleActions)
        {
            var copy = state.Clone();
            var realUnit = copy.Units.Find(u => u.Id == unit.Id);

            HexCellData moveTo = action.MoveTo != null && copy.Grid.TryGetValue(action.MoveTo.Coord, out var c) ? c : null;
            UnitData target = action.AttackTarget != null ? copy.Units.Find(u => u.Id == action.AttackTarget.Id) : null;

            var realAction = new UnitAction(realUnit, moveTo, target);
            copy.ApplyUnitAction(realAction);

            int score = Minimax(copy, depth - 1, false, int.MinValue, int.MaxValue);

            if (score > bestScore)
            {
                bestScore = score;
                bestAction = action;
            }
        }
        return bestAction;
    }

    private int Minimax(GameState state, int depth, bool maximizing, int alpha, int beta)
    {
        if (depth <= 0 || state.IsTerminal()) return Evaluate(state);

        Team currentSimTeam = maximizing ? aiTeam : Opponent(aiTeam);

        var simUnit = state.Units.Find(u => u.Team == currentSimTeam && u.IsAlive);

        if (simUnit == null) return Evaluate(state);

        simUnit.ResetTurnState();

        var actions = state.GetPossibleUnitActions(simUnit);

        if (maximizing)
        {
            int maxEval = int.MinValue;
            foreach (var a in actions)
            {
                var copy = state.Clone();
                var copyUnit = copy.Units.Find(u => u.Id == simUnit.Id);
                HexCellData moveTo = a.MoveTo != null && copy.Grid.TryGetValue(a.MoveTo.Coord, out var c) ? c : null;
                UnitData target = a.AttackTarget != null ? copy.Units.Find(u => u.Id == a.AttackTarget.Id) : null;

                copy.ApplyUnitAction(new UnitAction(copyUnit, moveTo, target));

                int eval = Minimax(copy, depth - 1, false, alpha, beta);
                maxEval = Mathf.Max(maxEval, eval);
                alpha = Mathf.Max(alpha, eval);
                if (beta <= alpha) break;
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var a in actions)
            {
                var copy = state.Clone();
                var copyUnit = copy.Units.Find(u => u.Id == simUnit.Id);
                HexCellData moveTo = a.MoveTo != null && copy.Grid.TryGetValue(a.MoveTo.Coord, out var c) ? c : null;
                UnitData target = a.AttackTarget != null ? copy.Units.Find(u => u.Id == a.AttackTarget.Id) : null;

                copy.ApplyUnitAction(new UnitAction(copyUnit, moveTo, target));

                int eval = Minimax(copy, depth - 1, true, alpha, beta);
                minEval = Mathf.Min(minEval, eval);
                beta = Mathf.Min(beta, eval);
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    private int Evaluate(GameState state)
    {
        int score = 0;
        foreach (var u in state.Units)
        {
            if (!u.IsAlive) continue;

            int unitValue = u.Health + u.AttackDamage * 3; // Ценность юнита

            if (u.Team == aiTeam)
            {
                score += unitValue;

                // Бонус за то что враг в зоне атаки
                foreach (var enemy in state.Units)
                {
                    if (enemy.IsAlive && u.IsEnemy(enemy) && u.Cell.DistanceTo(enemy.Cell) <= u.AttackRange)
                        score += 30;
                }

                // Штраф за расстояние до ближайшего врага
                int minDist = int.MaxValue;
                foreach (var enemy in state.Units)
                {
                    if (enemy.IsAlive && u.IsEnemy(enemy))
                        minDist = Mathf.Min(minDist, u.Cell.DistanceTo(enemy.Cell));
                }
                if (minDist != int.MaxValue)
                    score -= minDist * 5; 
            }
            else
            {
                score -= unitValue;
            }
        }
        return score;
    }

    private Team Opponent(Team t) => t == Team.Player ? Team.Enemy : Team.Player;
}