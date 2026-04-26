using System;
using System.Collections.Generic;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager instance;

    public event Action<CustomerInstance> OnCustomerArrived;
    public event Action<CustomerInstance> OnCustomerServed;
    public event Action<CustomerInstance> OnCustomerLeft;
    public event Action<CustomerInstance> OnCustomerDismissed;
    public event Action<CustomerInstance, bool> OnCounterOfferResult;
    public event Action OnQueueChanged;

    const float servedRatingDelta = 0.75f;
    const float counterAcceptedRatingDelta = 0.80f;
    const float patienceExpiredRatingDelta = -0.50f;
    const float dismissedRatingDelta = -0.25f;

    [Header("Arrival Timing")]
    [SerializeField] float minSpawnIntervalSeconds = 30f;
    [SerializeField] float maxSpawnIntervalSeconds = 90f;
    [SerializeField] int maxQueueCount = 7;

    [Header("Counter Offers")]
    [SerializeField] float counterOfferIncreasePercent = 0.15f;
    [SerializeField] bool enableSpawnDebugLogs = true;

    readonly List<CustomerInstance> queue = new List<CustomerInstance>();
    float nextSpawnAtTime;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        ScheduleNextSpawn();
    }

    void Update()
    {
        TickPatience(Time.deltaTime);
        TrySpawnCustomer();
    }

    public IReadOnlyList<CustomerInstance> GetQueue() => queue;

    public int GetCurrentCustomerCount() => queue.Count;

    public CustomerInstance GetCustomerById(string customerId)
    {
        if (string.IsNullOrEmpty(customerId)) return null;
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] != null && queue[i].customerId == customerId) return queue[i];
        }
        return null;
    }

    public void GreetCustomer(string customerId)
    {
        var customer = GetCustomerById(customerId);
        if (customer == null || !customer.IsActive) return;
        customer.hasBeenGreeted = true;
        customer.state = CustomerState.Interacting;
        customer.currentLine = PickRandomLine(customer.personality != null ? customer.personality.greetingLines : null, "Hello!");
        NotifyQueueChanged();
    }

    public bool CanServeCustomer(string customerId)
    {
        var customer = GetCustomerById(customerId);
        return CanServeCustomer(customer);
    }

    public bool SellToCustomer(string customerId)
    {
        var customer = GetCustomerById(customerId);
        if (customer == null || !customer.IsActive || StoreManager.instance == null) return false;
        if (!CanServeCustomer(customer)) return false;

        bool sold = StoreManager.instance.SellFish(customer.wantedSpeciesId, customer.wantedFishCount, customer.offerPrice);
        if (!sold) return false;

        customer.state = CustomerState.Served;
        customer.currentLine = PickRandomLine(customer.personality != null ? customer.personality.satisfiedLines : null, "Thanks!");
        customer.selectedReviewLine = GetOutcomeReviewLine(customer, hadGoodExperience: true);
        if (GameManager.instance != null) GameManager.instance.AddStoreRating(servedRatingDelta);
        TryAddTip(customer);
        RemoveCustomer(customer);
        OnCustomerServed?.Invoke(customer);
        return true;
    }

    public bool CounterOffer(string customerId)
    {
        var customer = GetCustomerById(customerId);
        if (customer == null || !customer.CanCounterOffer || StoreManager.instance == null) return false;

        customer.counterOfferUsed = true;
        float acceptChance = customer.personality != null ? Mathf.Clamp01(customer.personality.counterOfferAcceptChance) : 0f;
        bool accepted = UnityEngine.Random.value <= acceptChance;
        OnCounterOfferResult?.Invoke(customer, accepted);

        if (!accepted)
        {
            customer.state = CustomerState.Dismissed;
            customer.currentLine = PickRandomLine(customer.personality != null ? customer.personality.counterRejectedLines : null, "No deal.");
            customer.selectedReviewLine = GetOutcomeReviewLine(customer, hadGoodExperience: false);
            if (GameManager.instance != null) GameManager.instance.AddStoreRating(dismissedRatingDelta);
            RemoveCustomer(customer);
            OnCustomerDismissed?.Invoke(customer);
            return false;
        }

        if (!CanServeCustomer(customer)) return false;

        bool sold = StoreManager.instance.SellFish(customer.wantedSpeciesId, customer.wantedFishCount, customer.counterOfferPrice);
        if (!sold) return false;

        customer.state = CustomerState.Served;
        customer.currentLine = PickRandomLine(customer.personality != null ? customer.personality.counterAcceptedLines : null, "Deal.");
        customer.selectedReviewLine = GetOutcomeReviewLine(customer, hadGoodExperience: true);
        if (GameManager.instance != null) GameManager.instance.AddStoreRating(counterAcceptedRatingDelta);
        TryAddTip(customer);
        RemoveCustomer(customer);
        OnCustomerServed?.Invoke(customer);
        return true;
    }

    public void DismissCustomer(string customerId)
    {
        var customer = GetCustomerById(customerId);
        if (customer == null || !customer.IsActive) return;
        customer.state = CustomerState.Dismissed;
        customer.currentLine = PickRandomLine(customer.personality != null ? customer.personality.dissatisfiedLines : null, "Maybe next time.");
        customer.selectedReviewLine = null;
        if (GameManager.instance != null) GameManager.instance.AddStoreRating(dismissedRatingDelta);
        RemoveCustomer(customer);
        OnCustomerDismissed?.Invoke(customer);
    }

    void TickPatience(float deltaTime)
    {
        if (deltaTime <= 0f || queue.Count == 0) return;

        List<CustomerInstance> expiredCustomers = null;
        for (int i = 0; i < queue.Count; i++)
        {
            var customer = queue[i];
            if (customer == null || !customer.IsActive) continue;

            customer.patienceRemaining -= deltaTime;
            if (customer.patienceRemaining > 0f) continue;

            if (expiredCustomers == null) expiredCustomers = new List<CustomerInstance>();
            expiredCustomers.Add(customer);
        }

        if (expiredCustomers == null) return;

        for (int i = 0; i < expiredCustomers.Count; i++)
        {
            var customer = expiredCustomers[i];
            customer.state = CustomerState.Left;
            customer.currentLine = PickRandomLine(customer.personality != null ? customer.personality.dissatisfiedLines : null, "I'm leaving.");
            customer.selectedReviewLine = GetOutcomeReviewLine(customer, hadGoodExperience: false);
            if (GameManager.instance != null) GameManager.instance.AddStoreRating(patienceExpiredRatingDelta);
            RemoveCustomer(customer);
            OnCustomerLeft?.Invoke(customer);
        }
    }

    void TrySpawnCustomer()
    {
        if (Time.time < nextSpawnAtTime) return;
        ScheduleNextSpawn();
        if (queue.Count >= Mathf.Max(1, maxQueueCount))
        {
            LogSpawnFailure($"queue is full ({queue.Count}/{Mathf.Max(1, maxQueueCount)})");
            return;
        }

        var customer = BuildCustomer();
        if (customer == null) return;

        queue.Add(customer);
        LogSpawnSuccess(customer);
        OnCustomerArrived?.Invoke(customer);
        NotifyQueueChanged();
    }

    void ScheduleNextSpawn()
    {
        float minValue = Mathf.Max(1f, minSpawnIntervalSeconds);
        float maxValue = Mathf.Max(minValue, maxSpawnIntervalSeconds);
        float baseDelay = UnityEngine.Random.Range(minValue, maxValue);
        float trafficBoost = Mathf.Clamp01(GetTotalDecorBonus(DecorBonusType.TrafficBoost));
        float delay = baseDelay * (1f - trafficBoost);
        nextSpawnAtTime = Time.time + Mathf.Max(1f, delay);
    }

    CustomerInstance BuildCustomer()
    {
        if (StoreManager.instance == null)
        {
            LogSpawnFailure("StoreManager.instance is null");
            return null;
        }
        if (FishSpeciesRegistry.instance == null)
        {
            LogSpawnFailure("FishSpeciesRegistry.instance is null");
            return null;
        }
        if (CustomerPersonalityRegistry.instance == null)
        {
            LogSpawnFailure("CustomerPersonalityRegistry.instance is null");
            return null;
        }

        List<string> unlockedSpeciesIds = StoreManager.instance.GetUnlockedSpeciesIds();
        if (unlockedSpeciesIds == null || unlockedSpeciesIds.Count == 0)
        {
            LogSpawnFailure("no unlocked fish species available for customer demand");
            return null;
        }

        var personality = CustomerPersonalityRegistry.instance.GetRandom();
        if (personality == null)
        {
            LogSpawnFailure("no valid customer personality found in registry");
            return null;
        }

        string wantedSpeciesId = unlockedSpeciesIds[UnityEngine.Random.Range(0, unlockedSpeciesIds.Count)];
        var speciesData = FishSpeciesRegistry.instance.GetSpecies(wantedSpeciesId);
        if (speciesData == null)
        {
            LogSpawnFailure($"species lookup failed for id '{wantedSpeciesId}'");
            return null;
        }

        int fishCount = Mathf.Max(1, personality.GetRandomFishCount());
        float offerMultiplier = personality.GetRandomOfferMultiplier();
        int baseOfferPrice = Mathf.Max(1, Mathf.RoundToInt(speciesData.purchaseCost * offerMultiplier * fishCount));
        int counterOfferPrice = Mathf.Max(
            baseOfferPrice,
            Mathf.RoundToInt(baseOfferPrice * (1f + Mathf.Max(0f, counterOfferIncreasePercent)))
        );
        float waitBonus = Mathf.Max(0f, GetTotalDecorBonus(DecorBonusType.WaitTimeReduction));
        float patienceSeconds = Mathf.Max(1f, personality.patienceSeconds * (1f + waitBonus));

        return new CustomerInstance
        {
            customerId = Guid.NewGuid().ToString("N"),
            personality = personality,
            wantedSpeciesId = wantedSpeciesId,
            wantedFishCount = fishCount,
            offerPrice = baseOfferPrice,
            counterOfferPrice = counterOfferPrice,
            patienceRemaining = patienceSeconds,
            hasBeenGreeted = false,
            counterOfferUsed = false,
            state = CustomerState.Waiting,
            currentLine = PickRandomLine(personality.greetingLines, "Hello!")
        };
    }

    void LogSpawnSuccess(CustomerInstance customer)
    {
        if (!enableSpawnDebugLogs || customer == null) return;

        string personalityName = customer.personality != null
            ? customer.personality.displayName
            : "Unknown Personality";
        string speciesName = customer.wantedSpeciesId;
        if (FishSpeciesRegistry.instance != null)
        {
            var species = FishSpeciesRegistry.instance.GetSpecies(customer.wantedSpeciesId);
            if (species != null && !string.IsNullOrWhiteSpace(species.displayName))
                speciesName = species.displayName;
        }

        Debug.Log(
            $"[CustomerSpawn] Spawned customer '{customer.customerId}' ({personalityName}) " +
            $"wanting {customer.wantedFishCount}x {speciesName} for ${customer.offerPrice} " +
            $"(counter ${customer.counterOfferPrice}, patience {customer.patienceRemaining:0.0}s)"
        );
    }

    void LogSpawnFailure(string reason)
    {
        if (!enableSpawnDebugLogs) return;
        Debug.LogWarning($"[CustomerSpawn] Spawn failed: {reason}");
    }

    bool CanServeCustomer(CustomerInstance customer)
    {
        if (customer == null || StoreManager.instance == null) return false;
        return FindSellableSlot(customer.wantedSpeciesId, customer.wantedFishCount) >= 0;
    }

    int FindSellableSlot(string speciesId, int fishCount)
    {
        if (TankManager.instance == null || string.IsNullOrEmpty(speciesId) || fishCount <= 0) return -1;
        for (int i = 0; i < TankManager.instance.SlotCount; i++)
        {
            var slot = TankManager.instance.GetSlot(i);
            if (slot == null || !slot.isOwned) continue;
            if (slot.speciesId != speciesId || slot.fishCount < fishCount) continue;
            if (!TankManager.instance.IsAcclimationComplete(i)) continue;
            if (!TankManager.instance.IsTankClean(i)) continue;
            return i;
        }
        return -1;
    }

    float GetTotalDecorBonus(DecorBonusType bonusType)
    {
        if (DecorRegistry.instance == null || GameManager.instance == null) return 0f;
        return DecorRegistry.instance.GetTotalOwnedBonus(bonusType);
    }

    void TryAddTip(CustomerInstance customer)
    {
        if (customer == null || customer.personality == null || TipJar.instance == null) return;
        int tipAmount = customer.personality.GetRandomTipAmount();
        if (tipAmount > 0) TipJar.instance.AddTip(tipAmount);
    }

    void RemoveCustomer(CustomerInstance customer)
    {
        if (customer == null) return;
        queue.Remove(customer);
        NotifyQueueChanged();
    }

    void NotifyQueueChanged() => OnQueueChanged?.Invoke();

    static string GetOutcomeReviewLine(CustomerInstance customer, bool hadGoodExperience)
    {
        if (customer == null || customer.personality == null)
        {
            return hadGoodExperience
                ? "Great service and healthy fish."
                : "Service was disappointing.";
        }

        return hadGoodExperience
            ? customer.personality.GetRandomGoodReviewLine()
            : customer.personality.GetRandomBadReviewLine();
    }

    static string PickRandomLine(string[] lines, string fallback)
    {
        if (lines == null || lines.Length == 0) return fallback;
        int randomStart = UnityEngine.Random.Range(0, lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            int index = (randomStart + i) % lines.Length;
            if (!string.IsNullOrWhiteSpace(lines[index])) return lines[index];
        }
        return fallback;
    }
}
