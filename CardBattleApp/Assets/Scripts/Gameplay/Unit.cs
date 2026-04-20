[System.Serializable]
public class Unit
{
    public int ownerId;
    public int health;
    public int attack;
    public int laneIndex;

    public bool IsDead => health <= 0;

    public Unit(int ownerId, int health, int attack, int laneIndex)
    {
        this.ownerId = ownerId;
        this.health = health;
        this.attack = attack;
        this.laneIndex = laneIndex;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
    }
}