using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public int GetBalance()
    {
        if (!GameManager.instance) return 0;
        return GameManager.instance.moneyAmount;
    }

    public void AddMoney(int amount)
    {
        if (GameManager.instance) GameManager.instance.AddMoney(amount);
    }

    public bool SpendMoney(int amount)
    {
        return GameManager.instance != null && GameManager.instance.SpendMoney(amount);
    }

    public bool CanAfford(int amount) => GameManager.instance != null && GameManager.instance.moneyAmount >= amount;
}
