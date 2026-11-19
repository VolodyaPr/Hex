using UnityEngine;

public class HexCell : MonoBehaviour
{
    public int Q { get; private set; }
    public int R { get; private set; }

    public Vector2Int Coord => new Vector2Int(Q, R);

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private Color originalColor = Color.white;
    private bool isSelected = false;
    private bool isReachable = false;
    private bool isAttackable = false;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();

        if (meshRenderer.sharedMaterial != null && meshRenderer.sharedMaterial.HasProperty(BaseColor))
        {
            originalColor = meshRenderer.sharedMaterial.GetColor(BaseColor);
        }
    }

    public void Init(int q, int r)
    {
        Q = q;
        R = r;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        meshRenderer.GetPropertyBlock(propBlock);

        if (isSelected)
        {
            propBlock.SetColor(BaseColor, Color.yellow); 
        }
        else if (isReachable)
        {
            propBlock.SetColor(BaseColor, Color.green); 
        }
        else if (isAttackable)
        {
            propBlock.SetColor(BaseColor, Color.red); 
        }
        else
        {
            propBlock.SetColor(BaseColor, originalColor); 
        }

        meshRenderer.SetPropertyBlock(propBlock);
    }



    // Метод для расчета расстояния в гексах до другой клетки
    public int DistanceTo(HexCell other)
    {
        // Переводим текущие координаты (q, r) со смещением в кубические координаты
        int x1 = Q - (R - (R & 1)) / 2;
        int z1 = R;
        int y1 = -x1 - z1;

        int x2 = other.Q - (other.R - (other.R & 1)) / 2;
        int z2 = other.R;
        int y2 = -x2 - z2;

        return Mathf.Max(Mathf.Abs(x1 - x2), Mathf.Abs(y1 - y2), Mathf.Abs(z1 - z2));
    }


    // Методы для подсветки клеток
    public void SetReachable(bool reachable)
    {
        isReachable = reachable;

        meshRenderer.GetPropertyBlock(propBlock);

        if (isReachable)
        {
            propBlock.SetColor(BaseColor, Color.green); 
        }
        else if (!isSelected)
        {
            propBlock.SetColor(BaseColor, originalColor); 
        }

        meshRenderer.SetPropertyBlock(propBlock);
    }

    public void SetAttackable(bool attackable)
    {
        isAttackable = attackable;

        meshRenderer.GetPropertyBlock(propBlock);

        if (isAttackable)
        {
            propBlock.SetColor(BaseColor, Color.red);
        }
        else if (!isSelected && !isReachable)
        {
            propBlock.SetColor(BaseColor, originalColor);
        }

        meshRenderer.SetPropertyBlock(propBlock);
    }
    public Unit OccupiedUnit { get; private set; }

    public void SetUnit(Unit unit)
    {
        OccupiedUnit = unit;
    }

    public bool IsOccupied()
    {
        return OccupiedUnit != null;
    }
}