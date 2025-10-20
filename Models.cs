using System;
using System.Collections.Generic;
using System.Linq;

namespace InventorySystem;

public abstract class Item
{
    public string Name { get; set; } = "";
    public decimal PricePerUnit { get; set; }

    public virtual decimal PriceFor(decimal quantity) => PricePerUnit * quantity;
    public override string ToString() => $"{Name}: {PricePerUnit} per unit";
}

public class BulkItem : Item
{
    public string MeasurementUnit { get; set; } = "kg";
    public override string ToString() => $"{Name}: {PricePerUnit} per {MeasurementUnit}";
}

public class UnitItem : Item
{
    public decimal Weight { get; set; }    // weight per piece in kg
    public override string ToString() => $"{Name}: {PricePerUnit} per unit (wt {Weight} kg)";
}

public class Inventory
{
    public Dictionary<Item, decimal> Stock { get; } = new();

    public void Add(Item item, decimal amount)
    {
        if (Stock.ContainsKey(item)) Stock[item] += amount;
        else Stock[item] = amount;
    }

    public bool TryConsume(Item item, decimal amount)
    {
        if (!Stock.TryGetValue(item, out var have) || have < amount) return false;
        Stock[item] = have - amount;
        return true;
    }

    public List<Item> LowStockItems(decimal threshold = 5m) =>
        Stock.Where(kv => kv.Value < threshold).Select(kv => kv.Key).ToList();
}

public class OrderLine
{
    public Item Item { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal LineTotal => Item.PriceFor(Quantity);
    public override string ToString() => $"{Item.Name} × {Quantity} → {LineTotal}";
}

public class Order
{
    public DateTime Time { get; init; } = DateTime.Now;
    public List<OrderLine> OrderLines { get; } = new();
    public decimal TotalPrice() => OrderLines.Sum(o => o.LineTotal);
    
    public decimal Total => OrderLines.Sum(ol => ol.LineTotal);

    public string OrderLinesDisplay =>
        string.Join(", ", OrderLines.Select(ol => $"{ol.Item.Name} x {ol.Quantity}"));

}

public class OrderBook
{
    public Queue<Order> QueuedOrders { get; } = new();
    public List<Order> ProcessedOrders { get; } = new();

    public void QueueOrder(Order order) => QueuedOrders.Enqueue(order);

    public Order? ProcessNextOrder(Inventory inv)
    {
        if (QueuedOrders.Count == 0) return null;
        var next = QueuedOrders.Peek();

        
        foreach (var l in next.OrderLines)
            if (!inv.Stock.TryGetValue(l.Item, out var have) || have < l.Quantity)
                return null;

        
        foreach (var l in next.OrderLines) inv.TryConsume(l.Item, l.Quantity);
        QueuedOrders.Dequeue();
        ProcessedOrders.Add(next);
        return next;
    }

    public decimal TotalRevenue() => ProcessedOrders.Sum(o => o.TotalPrice());
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
