using UnityEngine;

public class DecorBuyButton : MonoBehaviour
{
    [SerializeField] DecorItemData data;

    public void Buy()
    {
        if (data == null || string.IsNullOrEmpty(data.decorId) || StoreManager.instance == null) return;
        StoreManager.instance.OrderDecor(data.decorId);
    }
}
