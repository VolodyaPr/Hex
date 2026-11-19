using System.Collections.Generic;
using UnityEngine;

public class MinimaxAI
{
    private readonly Team aiTeam;

    public MinimaxAI(Team aiTeam) { this.aiTeam = aiTeam; }

    public UnitAction FindBestActionForUnit(GameState state, UnitData unit, int depth)
    {
        var possibleActions = state.GetPossibleUnitActions(unit, false);

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

            // Приоритет атаки: 
            if (action.AttackTarget != null)
            {
                score += 500;
            }

            string actionName = action.AttackTarget != null ? $"Атака {action.AttackTarget.Name}" :
                               (action.MoveTo != unit.Cell ? "Движение" : "Ожидание");
            // Debug.Log($"[AI] Действие: {actionName} | Оценка: {score}");

            if (score > bestScore)
            {
                bestScore = score;
                bestAction = action;
            }
        }

        if (bestAction != null)
        {
            string bestName = bestAction.AttackTarget != null ? $"Атака {bestAction.AttackTarget.Name}" :
                             (bestAction.MoveTo != unit.Cell ? "Движение" : "Ожидание");
            Debug.Log($"<color=red>[AI] ВЫБРАНО: {bestName} (Оценка: {bestScore})</color>");
        }

        return bestAction;
    }

    private int Minimax(GameState state, int depth, bool maximizing, int alpha, int beta)
    {
        if (depth <= 0 || state.IsTerminal()) return Evaluate(state);

        Team currentSimTeam = maximizing ? aiTeam : Opponent(aiTeam);

        UnitData simUnit = GetMostThreateningUnit(state, currentSimTeam);
        if (simUnit == null) return Evaluate(state);

        simUnit.ResetTurnState();

        var actions = state.GetPossibleUnitActions(simUnit, false);

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

    private UnitData GetMostThreateningUnit(GameState state, Team team)
    {
        UnitData bestUnit = null;
        int minDistanceToEnemy = int.MaxValue;

        foreach (var u in state.Units)
        {
            if (u.Team != team || !u.IsAlive) continue;

            foreach (var enemy in state.Units)
            {
                if (enemy.IsAlive && u.IsEnemy(enemy))
                {
                    int dist = u.Cell.DistanceTo(enemy.Cell);
                    if (dist < minDistanceToEnemy)
                    {
                        minDistanceToEnemy = dist;
                        bestUnit = u;
                    }
                }
            }
        }

        if (bestUnit == null)
            bestUnit = state.Units.Find(u => u.Team == team && u.IsAlive);

        return bestUnit;
    }

    private int Evaluate(GameState state)
    {
        int score = 0;
        foreach (var u in state.Units)
        {
            if (!u.IsAlive) continue;

            int unitValue = u.Health + u.AttackDamage * 2;

            if (u.Team == aiTeam)
            {
                score += unitValue;

                // Бонус за то, что враг в зоне атаки
                foreach (var enemy in state.Units)
                {
                    if (enemy.IsAlive && u.IsEnemy(enemy) && u.Cell.DistanceTo(enemy.Cell) <= u.AttackRange)
                        score += 150;
                }

                // Заставляем ИИ двигаться если он дажеко
                int minDist = int.MaxValue;
                foreach (var enemy in state.Units)
                {
                    if (enemy.IsAlive && u.IsEnemy(enemy))
                        minDist = Mathf.Min(minDist, u.Cell.DistanceTo(enemy.Cell));
                }

                if (minDist != int.MaxValue)
                {
                    score -= minDist * 5; // -5 очков за гекс
                }
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
