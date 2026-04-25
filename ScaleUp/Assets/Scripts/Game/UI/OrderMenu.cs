using UnityEngine;
using UnityEngine.Serialization;

public class OrderMenu : MonoBehaviour
{
    [Header("Panels")]
    [FormerlySerializedAs("orderMenuPanel")] [SerializeField] GameObject orderMenu;
    [FormerlySerializedAs("fishMenuPanel")] [SerializeField] GameObject fishMenu;
    [FormerlySerializedAs("tankMenuPanel")] [SerializeField] GameObject tankMenu;
    [FormerlySerializedAs("foodMenuPanel")] [SerializeField] GameObject foodMenu;
    [FormerlySerializedAs("decorMenuPanel")] [SerializeField] GameObject decorMenu;

    public void OpenOrderMenu() => Show(orderMenu);
    public void OpenFishMenu() => Show(fishMenu);
    public void OpenTankMenu() => Show(tankMenu);
    public void OpenFoodMenu() => Show(foodMenu);
    public void OpenDecorMenu() => Show(decorMenu);
    public void CloseAll() => Show(null);

    public void HireEmployee()
    {
        if (StoreManager.instance != null) StoreManager.instance.HireEmployee();
    }

    public void BuyGoodReviews()
    {
        if (StoreManager.instance != null) StoreManager.instance.BuyGoodReviews();
    }

    public void FranchiseProperty()
    {
        if (StoreManager.instance != null) StoreManager.instance.FranchiseProperty();
    }

    void Show(GameObject target)
    {
        if (orderMenu) orderMenu.SetActive(target == orderMenu);
        if (fishMenu) fishMenu.SetActive(target == fishMenu);
        if (tankMenu) tankMenu.SetActive(target == tankMenu);
        if (foodMenu) foodMenu.SetActive(target == foodMenu);
        if (decorMenu) decorMenu.SetActive(target == decorMenu);
    }

}
