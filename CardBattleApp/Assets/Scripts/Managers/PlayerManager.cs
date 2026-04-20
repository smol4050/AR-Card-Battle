[System.Serializable]
public class PlayerManager
{
    public int playerId;
    public int hp = 20;
    public int energy = 1;
    public int maxEnergy = 1;

    public PlayerManager(int id)
    {
        playerId = id;
    }

    public void StartTurn()
    {
        maxEnergy = System.Math.Min(maxEnergy + 1, 5);
        energy = maxEnergy;
    }

    public bool ConsumeEnergy(int amount)
    {
        if (energy >= amount)
        {
            energy -= amount;
            return true;
        }
        return false;
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp < 0) hp = 0;
    }
}