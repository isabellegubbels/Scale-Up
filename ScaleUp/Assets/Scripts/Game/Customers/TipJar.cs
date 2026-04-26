using UnityEngine;

public class TipJar : MonoBehaviour
{
    public static TipJar instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public int GetTipTotal()
    {
        if (GameManager.instance == null) return 0;
        return Mathf.Max(0, GameManager.instance.tipJarAmount);
    }

    public void AddTip(int amount)
    {
        if (GameManager.instance == null || amount <= 0) return;
        GameManager.instance.tipJarAmount += amount;
        GameManager.instance.SaveGame();
    }

    public int CollectTips()
    {
        if (GameManager.instance == null) return 0;
        int tipTotal = Mathf.Max(0, GameManager.instance.tipJarAmount);
        if (tipTotal <= 0) return 0;

        GameManager.instance.tipJarAmount = 0;
        GameManager.instance.AddMoney(tipTotal);
        GameManager.instance.SaveGame();
        return tipTotal;
    }
}
