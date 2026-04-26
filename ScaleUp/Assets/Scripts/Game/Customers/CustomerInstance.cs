using System;
using UnityEngine;

public enum CustomerState
{
    Waiting,
    Interacting,
    Served,
    Left,
    Dismissed
}

[Serializable]
public class CustomerInstance
{
    public string customerId;
    public CustomerPersonalityData personality;
    public string wantedSpeciesId;
    public int wantedFishCount;
    public int offerPrice;
    public int counterOfferPrice;
    public float patienceRemaining;
    public bool hasBeenGreeted;
    public bool counterOfferUsed;
    public CustomerState state;
    public string currentLine;
    public string selectedReviewLine;

    public bool IsActive =>
        state == CustomerState.Waiting || state == CustomerState.Interacting;

    public bool CanCounterOffer =>
        IsActive && !counterOfferUsed;

    public bool HasPatienceRemaining => patienceRemaining > 0f;

    public string GetDisplayName()
    {
        if (personality != null && !string.IsNullOrWhiteSpace(personality.displayName))
            return personality.displayName;
        return "Customer";
    }
}
