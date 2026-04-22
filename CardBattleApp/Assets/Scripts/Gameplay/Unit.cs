[System.Serializable]
public class Unit
{
    public int ownerId;
    public int health;
    public int attack;
    public int tempAttack; // Bonus de ataque que dura solo un turno
    public int laneIndex;
    public CardID cardId;

    public bool IsDead => health <= 0;

    public Unit(int ownerId, int health, int attack, int laneIndex, CardID cardId)
    {
        this.ownerId = ownerId;
        this.health = health;
        this.attack = attack;
        this.laneIndex = laneIndex;
        this.cardId = cardId;
        this.tempAttack = 0;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
    }

    public void ResetTurnModifiers()
    {
        tempAttack = 0;
    }
}