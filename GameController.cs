using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameState State { get; private set; }
    public GridManager gridManager;
    public TurnManager turnManager;
    public MinimaxAI ai;
    public bool IsBusy { get; private set; }

    private Dictionary<int, Unit> unitObjects = new Dictionary<int, Unit>();
    private Dictionary<Unit, int> unitIds = new Dictionary<Unit, int>();
    private int nextUnitId = 1;

    void Awake()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        ai = new MinimaxAI(Team.Enemy);
    }

    void Start()
    {
        InitializeStateFromScene();
        if (turnManager != null)
        {
            turnManager.gameController = this;
            turnManager.OnTurnChanged += HandleTurnChanged;

            turnManager.StartCombat();
        }
    }

    void OnDestroy()
    {
        if (turnManager != null) turnManager.OnTurnChanged -= HandleTurnChanged;
    }

    private void HandleTurnChanged(UnitData activeUnit)
    {
        if (State == null) return;
    }

    public void InitializeStateFromScene()
    {
        State = new GameState();
        unitObjects.Clear();
        unitIds.Clear();
        nextUnitId = 1;

        if (gridManager != null && gridManager.hexGrid != null)
        {
            foreach (var kvp in gridManager.hexGrid)
            {
                var cell = kvp.Value;
                State.Grid[kvp.Key] = new HexCellData(cell.Q, cell.R);
            }
        }

        Unit[] existingUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var unit in existingUnits)
        {
            int id = nextUnitId++;
            unitObjects[id] = unit;
            unitIds[unit] = id;

            var unitData = new UnitData
            {
                Id = id,
                Name = unit.gameObject.name,
                Team = unit.Team,
                AttackType = unit.UnitAttackType,
                Health = unit.MaxHealth,
                MaxHealth = unit.MaxHealth,
                MovementRange = unit.MovementRange,
                AttackRange = unit.AttackRange,
                AttackDamage = unit.AttackDamage,
                RemainingMovement = unit.MovementRange,
                HasAttacked = false,
                Initiative = Random.Range(1, 21)
            };

            if (unit.CurrentCell != null && State.Grid.TryGetValue(unit.CurrentCell.Coord, out var cellData))
            {
                unitData.Cell = cellData;
                cellData.OccupiedUnit = unitData;
            }
            State.Units.Add(unitData);
        }
    }

    // Применение одного действия с синхронизацией визуала
    public void ApplyUnitAction(UnitAction action, System.Action onComplete = null)
    {
        if (action == null || action.Unit == null)
        {
            onComplete?.Invoke();
            return;
        }

        var realUnit = State.Units.Find(u => u.Id == action.Unit.Id);
        if (realUnit == null || !realUnit.IsAlive)
        {
            onComplete?.Invoke();
            return;
        }

        HexCellData realMoveTo = action.MoveTo != null &&
            State.Grid.TryGetValue(action.MoveTo.Coord, out var c) ? c : null;
        UnitData realAttack = action.AttackTarget != null
            ? State.Units.Find(u => u.Id == action.AttackTarget.Id) : null;

        var realAction = new UnitAction(realUnit, realMoveTo, realAttack);

        bool willMove = realMoveTo != null && realMoveTo != realUnit.Cell;

        if (willMove && unitObjects.TryGetValue(realUnit.Id, out var unitObj))
        {
            IsBusy = true;
            StartCoroutine(ExecuteMoveThenAttack(realAction, unitObj, () =>
            {
                IsBusy = false;
                onComplete?.Invoke();
            }));
        }
        else
        {
            ExecuteActionLogic(realAction);
            onComplete?.Invoke();
        }
    }

    private void ExecuteActionLogic(UnitAction realAction)
    {
        if (!State.ApplyUnitAction(realAction)) return;

        if (realAction.AttackTarget != null)
        {
            Debug.Log($"<color=orange>[БОЙ]</color> {realAction.Unit.Name} наносит {realAction.Unit.AttackDamage} урона юниту {realAction.AttackTarget.Name}. " +
                      $"Осталось HP: {Mathf.Max(0, realAction.AttackTarget.Health)}");
        }

        if (realAction.AttackTarget != null && !realAction.AttackTarget.IsAlive)
        {
            Debug.Log($"<color=red>[БОЙ]</color> Юнит {realAction.AttackTarget.Name} УНИЧТОЖЕН!");
            if (unitObjects.TryGetValue(realAction.AttackTarget.Id, out var deadObj))
            {
                unitObjects.Remove(realAction.AttackTarget.Id);
                unitIds.Remove(deadObj);
                Destroy(deadObj.gameObject);
            }
        }

        if (State.IsTerminal()) OnGameEnd();
    }

    private System.Collections.IEnumerator ExecuteMoveThenAttack(UnitAction realAction, Unit unitObj, System.Action onComplete)
    {
        var path = State.FindPath(realAction.Unit.Cell, realAction.MoveTo);

        if (path.Count > 1)
        {
            for (int i = 1; i < path.Count; i++)
            {
                HexCell targetHex = gridManager.GetCell(path[i].Coord);
                if (targetHex != null)
                {
                    unitObj.OccupyCell(targetHex);
                    yield return new WaitForSeconds(0.15f);
                }
            }
        }

        ExecuteActionLogic(realAction);

        onComplete?.Invoke();
    }

    public void MoveUnit(Unit unitObj, HexCellData startCell, HexCellData targetCell)
    {
        if (startCell == null || targetCell == null) return;

        var path = State.FindPath(startCell, targetCell);

        if (path.Count > 1)
        {
            StartCoroutine(AnimateMovement(unitObj, path));
        }
    }

    private System.Collections.IEnumerator AnimateMovement(Unit unitObj, List<HexCellData> path)
    {
        for (int i = 1; i < path.Count; i++)
        {
            HexCell targetHex = gridManager.GetCell(path[i].Coord);
            if (targetHex != null)
            {
                unitObj.OccupyCell(targetHex);

                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    public void ExecuteTurn(TurnAction turn)
    {
        if (turn == null) return;
        foreach (var action in turn.Actions)
            ApplyUnitAction(action);
    }

    public UnitData GetUnitDataAtCell(HexCell cell)
    {
        if (cell == null || State == null) return null;
        return State.Grid.TryGetValue(cell.Coord, out var cd) ? cd.OccupiedUnit : null;
    }

    public HexCellData GetCellData(HexCell cell)
    {
        if (cell == null || State == null) return null;
        return State.Grid.TryGetValue(cell.Coord, out var cd) ? cd : null;
    }

    private void OnGameEnd()
    {
        bool playerAlive = State.Units.Exists(u => u.Team == Team.Player && u.IsAlive);
        Debug.Log(playerAlive ? "ПОБЕДА PLAYER!" : "ПОБЕДА ENEMY!");
        Time.timeScale = 0;
    }


}
