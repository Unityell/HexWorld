using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexCreator : BaseSignal
{
    private Transform HexParent;
    private List<Hex> AllHexes = new List<Hex>();

    public void Refresh() => AllHexes.Clear();

    private bool FindHexParent()
    {
        var PoolParent = GameObject.FindGameObjectWithTag("Pool");
        if (PoolParent == null) return false;
        HexParent = PoolParent.transform.Find("Hex")?.transform;
        return HexParent != null;
    }

    private void ConnectNeighborHexes(Hex Hex)
    {
        HashSet<Hex> Neighbors = new HashSet<Hex>();
        foreach (Vector3Int Direction in HexLibrary.GetHexDirections())
        {
            Vector3Int NeighborCoords = Hex.Position + Direction;
            var NearHexes = AllHexes
                .FindAll(X => X.Position.x == NeighborCoords.x && X.Position.z == NeighborCoords.z);

            if (NearHexes != null)
                Neighbors.UnionWith(NearHexes);
        }

        Hex.SetNeighborHexes(Neighbors);

        foreach (Hex Neighbor in Hex.ConnectedHexes)
        {
            HashSet<Hex> NeighborConnections = new HashSet<Hex>(Neighbor.ConnectedHexes) { Hex };
            Neighbor.SetNeighborHexes(NeighborConnections);
        }
    }

    public GameObject CreateHexNeighbor(Hex HitHex, RaycastHit Hit, GameObject HexPrefab, List<HexPaintOption> HexPaintOptions, int TargetHeight, int TargetLength)
    {
        if (!FindHexParent()) return null;

        float Angle = Vector3.Angle(Hit.normal, Vector3.up);
        bool ClickedOnTop = Angle < 30f;

        Vector3 NewRealPosition;
        Vector3Int NewCoords;

        if (ClickedOnTop)
        {
            NewRealPosition = HitHex.transform.position + Vector3.up * TargetHeight;
            NewCoords = new Vector3Int(HitHex.Position.x, HitHex.Position.y + TargetHeight, HitHex.Position.z);
        }
        else
        {
            Vector3Int ClosestDirection = HexLibrary.GetHexDirections()
                .OrderBy(D =>
                    Vector3.Distance(
                        Hit.point,
                        HitHex.transform.position + DirectionToWorldOffset(D)
                    )
                )
                .First();

            NewCoords = HitHex.Position + ClosestDirection;
            NewRealPosition = HitHex.transform.position + DirectionToWorldOffset(ClosestDirection);
        }

        if (!IsValidHexPosition(NewCoords, TargetHeight, TargetLength))
            return null;

        GameObject NewHex = Instantiate(HexPrefab, HexParent.transform);
        NewHex.transform.position = NewRealPosition;

        MeshRenderer SelectedHex = null;
        foreach (var Option in HexPaintOptions)
        {
            if (Option.IsSelected && Option.HexPrefab != null)
            {
                SelectedHex = Option.HexPrefab;
                break;
            }
        }

        Hex NewHexComponent = NewHex.GetComponent<Hex>();
        NewHexComponent.Initialization(new HashSet<object> { NewCoords });
        NewHexComponent.SetHeight(TargetHeight);
        NewHexComponent.SetLength(TargetLength);
        NewHexComponent.SetHexVisual(SelectedHex);
        HitHex.SetIsWalkable(!(HitHex.Position.y == NewHexComponent.Position.y - NewHexComponent.Lenght));
        NewHexComponent.UpdateEmblemPosition();

        AllHexes = HexParent.GetComponentsInChildren<Hex>().ToList();
        AllHexes.Add(NewHexComponent);

        ConnectNeighborHexes(NewHexComponent);

        return NewHex;
    }

    private bool IsValidHexPosition(Vector3Int Coords, int TargetHeight, int TargetLength)
    {
        int NewStart = TargetHeight - TargetLength + 1;
        int NewEnd = TargetHeight;

        if (AllHexes.Count == 0) AllHexes = HexParent.GetComponentsInChildren<Hex>().ToList();

        var SameXZHexes = AllHexes
            .Where(H => H.Position.x == Coords.x && H.Position.z == Coords.z)
            .OrderBy(H => H.Position.y)
            .ToList();

        if (SameXZHexes.Count == 0) return true;

        var OccupiedRanges = new List<(int Start, int End)>();
        foreach (var Neighbor in SameXZHexes)
        {
            int Start = Neighbor.Position.y - Neighbor.Lenght + 1;
            int End = Neighbor.Position.y;
            OccupiedRanges.Add((Start, End));
        }

        OccupiedRanges = OccupiedRanges.OrderBy(R => R.Start).ToList();
        var MergedRanges = new List<(int Start, int End)>();

        foreach (var Range in OccupiedRanges)
        {
            if (MergedRanges.Count == 0)
            {
                MergedRanges.Add(Range);
            }
            else
            {
                var Last = MergedRanges[MergedRanges.Count - 1];
                if (Range.Start <= Last.End + 1)
                {
                    int MergedEnd = Mathf.Max(Last.End, Range.End);
                    MergedRanges[MergedRanges.Count - 1] = (Last.Start, MergedEnd);
                }
                else
                {
                    MergedRanges.Add(Range);
                }
            }
        }

        var FreeRanges = new List<(int Start, int End)>();
        int Pointer = int.MinValue;

        foreach (var (Start, End) in MergedRanges)
        {
            if (Start > Pointer)
            {
                FreeRanges.Add((Pointer, Start - 1));
            }
            Pointer = Mathf.Max(Pointer, End + 1);
        }

        FreeRanges.Add((Pointer, int.MaxValue));

        foreach (var (FreeStart, FreeEnd) in FreeRanges)
        {
            if (NewStart >= FreeStart && NewEnd <= FreeEnd)
                return true;
        }
        return false;
    }

    public void RemoveHex(Hex Hex)
    {
        foreach (Hex Neighbor in Hex.ConnectedHexes)
        {
            var UpdatedConnections = new HashSet<Hex>(Neighbor.ConnectedHexes);
            UpdatedConnections.Remove(Hex);
            Neighbor.SetNeighborHexes(UpdatedConnections);
        }
        AllHexes.Remove(Hex);
        DestroyImmediate(Hex.gameObject);
    }

    private Vector3 DirectionToWorldOffset(Vector3Int Direction)
    {
        float X = Mathf.Sqrt(3) * Direction.x + (Mathf.Sqrt(3) / 2f) * Direction.z;
        float Z = 1.5f * Direction.z;
        return new Vector3(X, 0, Z);
    }
}
