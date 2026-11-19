using UnityEngine;

public class HexCellData
{
    public int Q { get; }
    public int R { get; }
    public UnitData OccupiedUnit { get; set; }

    public Vector2Int Coord => new Vector2Int(Q, R);
    public bool IsOccupied => OccupiedUnit != null;

    public HexCellData(int q, int r)
    {
        Q = q;
        R = r;
    }

    public int DistanceTo(HexCellData other)
    {
        int x1 = Q - (R - (R & 1)) / 2;
        int z1 = R;
        int y1 = -x1 - z1;

        int x2 = other.Q - (other.R - (other.R & 1)) / 2;
        int z2 = other.R;
        int y2 = -x2 - z2;

        return Mathf.Max(Mathf.Abs(x1 - x2), Mathf.Abs(y1 - y2), Mathf.Abs(z1 - z2));
    }
}