using System;
using System.Collections.Generic;

public class PlayerState
{
    public const int StartingBalance = 500;

    private readonly List<string> ownedOrder = new List<string>();
    private readonly HashSet<string> ownedLookup = new HashSet<string>();

    public int Balance { get; private set; }

    public IReadOnlyList<string> OwnedItemIds => ownedOrder;
    public event Action<int> BalanceChanged;
    public event Action InventoryChanged;

    public bool Owns(string id) => ownedLookup.Contains(id);

    public void RestoreFrom(SaveData data, ICollection<string> knownItemIds)
    {
        Balance = data != null && data.balance > 0 ? data.balance : StartingBalance;

        ownedOrder.Clear();
        ownedLookup.Clear();

        if (data?.purchasedItemIds == null)
            return;

        foreach (string id in data.purchasedItemIds)
        {
            if (string.IsNullOrEmpty(id) || !knownItemIds.Contains(id))
                continue;

            if (ownedLookup.Add(id))
                ownedOrder.Add(id);
        }
    }

    public bool TryPurchase(ItemData item)
    {
        if (item == null || Owns(item.Id) || Balance < item.Price)
            return false;

        Balance -= item.Price;
        ownedLookup.Add(item.Id);
        ownedOrder.Add(item.Id);

        BalanceChanged?.Invoke(Balance);
        InventoryChanged?.Invoke();
        return true;
    }

    public SaveData CaptureSaveData()
    {
        return new SaveData
        {
            balance = Balance,
            purchasedItemIds = new List<string>(ownedOrder)
        };
    }
}
