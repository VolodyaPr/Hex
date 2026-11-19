using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Настройки сетки")]
    [SerializeField] private GameObject hexPrefab;
    [SerializeField] private int gridWidth = 12;
    [SerializeField] private int gridHeight = 9;
    [SerializeField] private float offsetX = 2.0f;
    [SerializeField] private float offsetZ = 2.0f;
    [SerializeField] private float hexRadius = 0.9f;

    [Header("Настройки юнитов")]
    [SerializeField] private List<UnitSpawnData> unitsToSpawn = new List<UnitSpawnData>();

    [Header("Ссылки")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private GameController gameController;

    public Dictionary<Vector2Int, HexCell> hexGrid = new Dictionary<Vector2Int, HexCell>();

    [System.Serializable]
    public struct UnitSpawnData
    {
        public GameObject unitPrefab;
        public Vector2Int spawnCoord;
    }

    private UnitData selectedUnitData;
    private HexCell selectedHexCell;
    private HashSet<HexCellData> reachableData = new HashSet<HexCellData>();
    private HashSet<HexCell> highlightedCells = new HashSet<HexCell>();

    void Awake()
    {
        GenerateGrid();
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
    }

    void OnDestroy()
    {
        if (turnManager != null) turnManager.OnTurnChanged -= HandleTurnChanged;
    }

    void Start()
    {
        if (turnManager != null) turnManager.OnTurnChanged += HandleTurnChanged;

        if (gameController == null)
        {
            Debug.LogError("GameController не найден на сцене! Добавьте пустой объект со скриптом GameController.");
        }
    }

    private void HandleTurnChanged(UnitData activeUnit)
    {
        ClearHighlight();
        Deselect();
    }

    public void ForceSelectCell(HexCell cell) => HandleClick(cell);

    void GenerateGrid()
    {
        float horizontalSpacing = hexRadius * 1.7320508f;
        float verticalSpacing = hexRadius * 1.5f;

        for (int r = 0; r < gridHeight; r++)
        {
            for (int q = 0; q < gridWidth; q++)
            {
                float xPos = (q * horizontalSpacing) + ((r % 2) * (horizontalSpacing / 2.0f)) + offsetX;
                float zPos = r * verticalSpacing + offsetZ;
                Vector3 spawnPos = new Vector3(xPos, 0, zPos);

                if (hexPrefab != null)
                {
                    GameObject hexObj = Instantiate(hexPrefab, spawnPos, Quaternion.identity, transform);
                    hexObj.name = $"Hex_{q}_{r}";
                    HexCell cell = hexObj.GetComponent<HexCell>() ?? hexObj.AddComponent<HexCell>();
                    cell.Init(q, r);
                    hexGrid[new Vector2Int(q, r)] = cell;
                }
            }
        }
        SpawnInitialUnits();
    }

    void SpawnInitialUnits()
    {
        foreach (var spawnData in unitsToSpawn)
        {
            if (spawnData.unitPrefab != null && hexGrid.ContainsKey(spawnData.spawnCoord))
            {
                HexCell targetCell = hexGrid[spawnData.spawnCoord];
                GameObject unitObj = Instantiate(spawnData.unitPrefab);
                Unit unitComponent = unitObj.GetComponent<Unit>();

                if (unitComponent != null)
                    unitComponent.OccupyCell(targetCell);
            }
        }
    }

    public HexCell GetCell(Vector2Int coord) =>
        hexGrid.TryGetValue(coord, out var cell) ? cell : null;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (gameController != null && gameController.IsBusy) return;

            if (turnManager == null || turnManager.ActiveUnit == null || turnManager.ActiveUnit.Team != Team.Player) return;

            if (gameController == null || gameController.State == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                HexCell clicked = hit.collider.GetComponent<HexCell>();
                if (clicked == null)
                {
                    Unit unit = hit.collider.GetComponent<Unit>();
                    if (unit != null) clicked = unit.CurrentCell;
                }

                if (clicked != null) HandleClick(clicked);
            }
        }
    }

    private void HandleClick(HexCell cell)
    {
        if (gameController == null || gameController.State == null) return;
        if (turnManager == null || turnManager.ActiveUnit == null || turnManager.ActiveUnit.Team != Team.Player) return;

        var clickedUnit = gameController.GetUnitDataAtCell(cell);
        var clickedCellData = gameController.GetCellData(cell);

        if (clickedCellData == null) return;

        if (selectedUnitData != null && clickedUnit != null && selectedUnitData.IsEnemy(clickedUnit))
        {
            TryAttack(clickedUnit, clickedCellData);
            return;
        }

        if (selectedUnitData != null && reachableData.Contains(clickedCellData))
        {
            TryMove(clickedCellData);
            return;
        }

        if (clickedUnit != null)
        {
            if (clickedUnit.Id != turnManager.ActiveUnit.Id)
            {
                Debug.Log("Сейчас ход другого юнита!");
                return;
            }

            if (selectedHexCell == cell) { Deselect(); return; }
            Deselect();
            selectedUnitData = clickedUnit;
            selectedHexCell = cell;
            cell.SetSelected(true);
            HighlightReachable(selectedUnitData);
            return;
        }

        Deselect();
    }

    private void HighlightReachable(UnitData unit)
    {
        ClearHighlight();
        var costs = gameController.State.GetReachableCellsWithCosts(unit);
        foreach (var cellData in costs.Keys)
        {
            reachableData.Add(cellData);
            var hex = GetCell(cellData.Coord);
            if (hex != null) { hex.SetReachable(true); highlightedCells.Add(hex); }
        }

        foreach (var enemy in gameController.State.Units)
        {
            if (!enemy.IsAlive || !unit.IsEnemy(enemy)) continue;
            bool canAttack = unit.Cell.DistanceTo(enemy.Cell) <= unit.AttackRange
                || AnyReachableInRange(unit, enemy);
            if (canAttack)
            {
                var hex = GetCell(enemy.Cell.Coord);
                if (hex != null) { hex.SetAttackable(true); highlightedCells.Add(hex); }
            }
        }
    }

    private bool AnyReachableInRange(UnitData unit, UnitData enemy)
    {
        foreach (var c in reachableData)
            if (c.DistanceTo(enemy.Cell) <= unit.AttackRange) return true;
        return false;
    }

    private void TryMove(HexCellData dest)
    {
        var unitToMove = selectedUnitData;
        var action = new UnitAction(unitToMove, dest, null);

        ClearHighlight();
        if (selectedHexCell != null) selectedHexCell.SetSelected(false);
        selectedHexCell = null;
        selectedUnitData = null;

        gameController.ApplyUnitAction(action, () =>
        {
            if (unitToMove != null && unitToMove.IsAlive && unitToMove.RemainingMovement > 0)
            {
                selectedUnitData = unitToMove;
                selectedHexCell = GetCell(unitToMove.Cell.Coord);
                if (selectedHexCell != null) selectedHexCell.SetSelected(true);
                HighlightReachable(unitToMove);
            }
        });
    }

    private void TryAttack(UnitData target, HexCellData targetCellData)
    {
        if (selectedUnitData == null || selectedUnitData.Cell == null) return;

        HexCellData bestPos = null;
        if (selectedUnitData.Cell.DistanceTo(targetCellData) <= selectedUnitData.AttackRange)
        {
            bestPos = selectedUnitData.Cell;
        }
        else
        {
            int minCost = int.MaxValue;
            foreach (var kvp in gameController.State.GetReachableCellsWithCosts(selectedUnitData))
            {
                if (kvp.Key.DistanceTo(targetCellData) <= selectedUnitData.AttackRange && kvp.Value < minCost)
                {
                    minCost = kvp.Value;
                    bestPos = kvp.Key;
                }
            }
        }

        if (bestPos == null) { Deselect(); return; }

        var action = new UnitAction(selectedUnitData, bestPos, target);
        Deselect();
        gameController.ApplyUnitAction(action);
    }

    private void Deselect()
    {
        if (selectedHexCell != null) selectedHexCell.SetSelected(false);
        selectedHexCell = null;
        selectedUnitData = null;
        ClearHighlight();
    }

    private void ClearHighlight()
    {
        reachableData.Clear();
        foreach (var cell in highlightedCells)
            if (cell != null) { cell.SetReachable(false); cell.SetAttackable(false); }
        highlightedCells.Clear();
    }
}