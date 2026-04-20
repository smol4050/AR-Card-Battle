using UnityEngine;

public class LaneManager : MonoBehaviour
{
    private CharacterCombat[,] _grid = new CharacterCombat[2, 3];

    [Header("Lane Positions")]
    public Transform[] player1LanePositions = new Transform[3];
    public Transform[] player2LanePositions = new Transform[3];

    public bool TryPlaceCard(int playerIndex, int laneIndex, CharacterCombat unit)
    {
        if (playerIndex < 0 || playerIndex > 1 || laneIndex < 0 || laneIndex > 2)
            return false;

        if (_grid[playerIndex, laneIndex] != null)
        {
            return false;
        }

        _grid[playerIndex, laneIndex] = unit;

        Transform targetPos = (playerIndex == 0) ? player1LanePositions[laneIndex] : player2LanePositions[laneIndex];
        if (targetPos != null)
        {
            unit.transform.position = targetPos.position;
            unit.transform.rotation = targetPos.rotation;
        }

        return true;
    }

    public CharacterCombat GetUnit(int playerIndex, int laneIndex)
    {
        return _grid[playerIndex, laneIndex];
    }

    public void CleanUpDeadUnits()
    {
        for (int p = 0; p < 2; p++)
        {
            for (int l = 0; l < 3; l++)
            {
                CharacterCombat unit = _grid[p, l];
                if (unit != null && unit.IsDead)
                {
                    // 1. Liberamos nuestra lógica primero
                    _grid[p, l] = null;

                    // 2. Delegamos la destrucción a Unity
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(unit.gameObject);
                    }
                    else
                    {
                        Destroy(unit.gameObject);
                    }
#else
                    Destroy(unit.gameObject);
#endif
                }
            }
        }
    }
}