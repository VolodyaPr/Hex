using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public event Action<UnitData> OnTurnChanged;

    public GameController gameController;
    public UnitData ActiveUnit { get; private set; }

    private Queue<UnitData> initiativeQueue = new Queue<UnitData>();
    private int roundCount = 0;
    private bool isProcessingTurn = false;

    public void StartCombat()
    {
        roundCount = 1;
        BuildInitiativeQueue();
        NextTurn();
    }

    // Собираем всех живых юнитов и сортируем по инициативе
    private void BuildInitiativeQueue()
    {
        initiativeQueue.Clear();
        var allUnits = gameController.State.Units.Where(u => u.IsAlive).ToList();

        allUnits.Sort((a, b) =>
        {
            int initCompare = b.Initiative.CompareTo(a.Initiative);
            if (initCompare != 0) return initCompare;
            if (a.Team != b.Team) return a.Team == Team.Player ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });

        foreach (var unit in allUnits)
        {
            initiativeQueue.Enqueue(unit);
        }

        Debug.Log($"--- НАЧАЛО РАУНДА {roundCount} ---");
        Debug.Log("Порядок ходов: " + string.Join(" -> ", allUnits.Select(u => $"{u.Name} (Иниц: {u.Initiative})")));
    }

    public void NextTurn()
    {
        if (isProcessingTurn) return;

        // Если очередь пуста начинается новый раунд
        if (initiativeQueue.Count == 0)
        {
            roundCount++;
            BuildInitiativeQueue();
        }

        ActiveUnit = initiativeQueue.Dequeue();

        if (ActiveUnit == null || !ActiveUnit.IsAlive)
        {
            NextTurn();
            return;
        }

        gameController.State.StartTurnForUnit(ActiveUnit);

        Debug.Log($"Ход юнита: {ActiveUnit.Name} ({ActiveUnit.Team})");

        OnTurnChanged?.Invoke(ActiveUnit);

        if (ActiveUnit.Team == Team.Enemy)
        {
            isProcessingTurn = true;
            Invoke(nameof(RunAITurn), 0.5f); 
        }
    }

    private void RunAITurn()
    {
        if (gameController == null || gameController.ai == null) return;

        var bestAction = gameController.ai.FindBestActionForUnit(gameController.State, ActiveUnit, depth: 3);

        if (bestAction != null)
        {
            gameController.ApplyUnitAction(bestAction, () =>
            {
                isProcessingTurn = false;
                if (!gameController.State.IsTerminal())
                {
                    NextTurn();
                }
            });
        }
        else
        {
            isProcessingTurn = false;
            if (!gameController.State.IsTerminal())
            {
                NextTurn();
            }
        }
    }

    public void EndPlayerTurn()
    {
        if (ActiveUnit != null && ActiveUnit.Team == Team.Player && !isProcessingTurn)
        {
            NextTurn();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && ActiveUnit != null && ActiveUnit.Team == Team.Player)
        {
            EndPlayerTurn();
        }
    }
}