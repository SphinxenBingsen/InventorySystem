using System;
using System.Collections.Generic;
using System.Linq;

namespace InventorySystem;

public abstract class Item
{
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public decimal PricePerUnit { get; set; }
    public uint InventoryLocation { get; set; } = 0;


    public decimal Quantity { get; set; }

    public virtual decimal PriceFor(decimal quantity)
    {
        return PricePerUnit * quantity;
    }

    public override string ToString()
    {
        return $"{Name}: {PricePerUnit} per unit";
    }
}

public class BulkItem : Item
{
    public string MeasurementUnit { get; set; } = "kg";

    public override string ToString()
    {
        return $"{Name}: {PricePerUnit} per {MeasurementUnit}";
    }
}

public class UnitItem : Item
{
    public decimal Weight { get; set; }

    public override string ToString()
    {
        return $"{Name}: {PricePerUnit} per unit (wt {Weight} kg)";
    }
}

public class Inventory
{
    public List<Item> Stock { get; } = new();

    public void Add(Item item, decimal amount)
    {
        if (!Stock.Contains(item))
            Stock.Add(item);

        item.Quantity += amount;
    }

    public bool TryConsume(Item item, decimal amount)
    {
        if (item.Quantity < amount) return false;
        item.Quantity -= amount;
        return true;
    }

    public List<Item> LowStockItems(decimal threshold = 5m)
    {
        return Stock.Where(i => i.Quantity < threshold).ToList();
    }
}

public class OrderLine
{
    public int OrderLineId { get; set; }
    public Item Item { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal LineTotal => Item.PriceFor(Quantity);

    public override string ToString()
    {
        return $"{Item.Name} × {Quantity} → {LineTotal}";
    }
}

public class Order
{
    public int OrderId { get; set; }
    public DateTime Time { get; init; } = DateTime.Now;
    public string CustomerName { get; set; } = "";
    public List<OrderLine> OrderLines { get; } = new();

    public decimal TotalPrice()
    {
        return OrderLines.Sum(o => o.LineTotal);
    }

    public decimal Total => OrderLines.Sum(ol => ol.LineTotal);

    public string OrderLinesDisplay =>
        string.Join(", ", OrderLines.Select(ol => $"{ol.Item.Name} x {ol.Quantity}"));
}

public class OrderBook
{
    public int OrderBookId { get; set; }
    public List<Order> QueuedOrders { get; } = new();
    public List<Order> ProcessedOrders { get; } = new();

    public void QueueOrder(Order order)
    {
        QueuedOrders.Add(order);
    }

    public Order? ProcessNextOrder(Inventory inv)
    {
        if (QueuedOrders.Count == 0) return null;
        var next = QueuedOrders[0];


        foreach (var l in next.OrderLines)
            if (l.Item.Quantity < l.Quantity)
                return null;


        foreach (var l in next.OrderLines)
            inv.TryConsume(l.Item, l.Quantity);


        QueuedOrders.RemoveAt(0);
        ProcessedOrders.Add(next);
        return next;
    }

    public decimal TotalRevenue()
    {
        return ProcessedOrders.Sum(o => o.TotalPrice());
    }
}

public class Customer
{
    public string Name { get; set; } = "";
    public List<Order> Orders { get; } = new();

    public void CreateOrder(OrderBook book, Order order)
    {
        Orders.Add(order);
        book.QueueOrder(order);
    }
}