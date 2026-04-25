using UnityEngine;

/// <summary>
/// Representa una nave desplegada en combate.
/// </summary>
[System.Serializable]
public class ShipInstance
{
    public CardID cardId;
    public int ownerId;

    public float duration;
    public float elapsed;
    public bool IsExpired => elapsed >= duration;

    public int totalPulses;
    public int pulsesFired;
    public float pulseInterval;
    public float pulseTimer;

    public Vector2 logicalCenter;

    public float atkBuffPercent;
    public float speedBuff;

    public ShipInstance(CardID id, int owner, float dur, int pulses, float interval,
                        Vector2 center, float atkBuff = 0f, float speedBuff = 0f)
    {
        cardId = id;
        ownerId = owner;
        duration = dur;
        elapsed = 0f;
        totalPulses = pulses;
        pulsesFired = 0;
        pulseInterval = interval;
        pulseTimer = 0f;
        logicalCenter = center;
        atkBuffPercent = atkBuff;
        this.speedBuff = speedBuff;
    }
}