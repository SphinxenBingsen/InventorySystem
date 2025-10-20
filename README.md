classDiagram
direction LR

class Item {
  +string Name
  +decimal PricePerUnit
  +decimal PriceFor(quantity)
  +string ToString()
}

class BulkItem {
  +string MeasurementUnit
}

class UnitItem {
  +decimal Weight
}

class Inventory {
  +Dictionary<Item, decimal> Stock
  +Add(item, amount)
  +TryConsume(item, amount)
  +LowStockItems(threshold)
}

class OrderLine {
  +Item Item
  +decimal Quantity
  +decimal LineTotal
}

class Order {
  +DateTime Time
  +List<OrderLine> OrderLines
  +Total()
  +OrderLinesDisplay()
  +TotalPrice()
}

class OrderBook {
  +Queue<Order> QueuedOrders
  +List<Order> ProcessedOrders
  +QueueOrder(order)
  +ProcessNextOrder(inventory)
  +TotalRevenue()
}

class Customer {
  +string Name
  +List<Order> Orders
  +CreateOrder(orderBook, order)
}

Item <|-- BulkItem
Item <|-- UnitItem
Inventory "1" o-- "0..*" Item
OrderBook "1" o-- "0..*" Order
Order "1" o-- "1..*" OrderLine
OrderLine --> Item
Customer "1" o-- "0..*" Order
