using System.Collections.ObjectModel;
using Avalonia.Controls;
using System.ComponentModel;

namespace InventorySystem
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<Order> QueuedOrders { get; } = new();
        public ObservableCollection<Order> ProcessedOrders { get; } = new();

        private readonly Inventory _inventory = new();
        private readonly OrderBook _orderBook = new();

        public decimal TotalRevenue => _orderBook.TotalRevenue();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; 

            
            var apple = new UnitItem { Name = "Apple", PricePerUnit = 2.5m, Weight = 0.2m };
            var flour = new BulkItem { Name = "Flour", PricePerUnit = 8.5m, MeasurementUnit = "kg" };

            _inventory.Add(apple, 10);
            _inventory.Add(flour, 20);

            var o1 = new Order();
            o1.OrderLines.Add(new OrderLine { Item = apple, Quantity = 3 });
            o1.OrderLines.Add(new OrderLine { Item = flour, Quantity = 1 });
            _orderBook.QueueOrder(o1);

            var o2 = new Order();
            o2.OrderLines.Add(new OrderLine { Item = apple, Quantity = 2 });
            _orderBook.QueueOrder(o2);

            foreach (var o in _orderBook.QueuedOrders)
                QueuedOrders.Add(o);
        }

        private void ProcessNext_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var processed = _orderBook.ProcessNextOrder(_inventory);
            if (processed is null) return;

            
            if (QueuedOrders.Count > 0) QueuedOrders.RemoveAt(0);
            ProcessedOrders.Add(processed);

            
            Raise(nameof(TotalRevenue));
        }
    }
}
