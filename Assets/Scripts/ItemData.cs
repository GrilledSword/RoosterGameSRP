using System;
using Unity.Collections;
using Unity.Netcode;

// A fájl neve legyen ItemData.cs
// Ez a struktúra tárolja egy tárgy adatait az inventory-ban.

[Serializable]
public struct ItemData : INetworkSerializable, IEquatable<ItemData>
{
    public int itemID;
    public FixedString32Bytes itemName;
    public int quantity;
    public bool isEmpty;

    public ItemData(int id, string name, int qty)
    {
        itemID = id;
        itemName = new FixedString32Bytes(name);
        quantity = qty;
        isEmpty = false;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemID);
        serializer.SerializeValue(ref itemName);
        serializer.SerializeValue(ref quantity);
        serializer.SerializeValue(ref isEmpty);
    }

    public bool Equals(ItemData other)
    {
        return itemID == other.itemID && itemName.Equals(other.itemName) && quantity == other.quantity && isEmpty == other.isEmpty;
    }
}