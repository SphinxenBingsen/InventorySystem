// MainWindow.axaml.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace InventorySystem
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<Order> QueuedOrders { get; } = new();
        public ObservableCollection<Order> ProcessedOrders { get; } = new();

        private readonly Inventory _inventory = new();
        private readonly OrderBook _orderBook = new();
        private readonly ItemSorterRobot _robot = new();
        private bool _isBusy;

        public decimal TotalRevenue => _orderBook.TotalRevenue();

        public new event PropertyChangedEventHandler? PropertyChanged;
        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _robot.IpAddress = "127.0.0.1"; // Docker-published localhost ports

            // DEMO inventory: A,B,C
            var item1 = new UnitItem { Name = "M3 screw", PricePerUnit = 1m,   InventoryLocation = 1 };
            var item2 = new UnitItem { Name = "M3 nut",   PricePerUnit = 1.5m, InventoryLocation = 2 };
            var item3 = new UnitItem { Name = "Pen",      PricePerUnit = 1m,   InventoryLocation = 3 };

            _inventory.Add(item1, 10);
            _inventory.Add(item2, 10);
            _inventory.Add(item3, 10);

            // DEMO orders
            var order1 = new Order();
            order1.OrderLines.Add(new OrderLine { Item = item1, Quantity = 1 }); // A
            order1.OrderLines.Add(new OrderLine { Item = item2, Quantity = 2 }); // B
            order1.OrderLines.Add(new OrderLine { Item = item3, Quantity = 1 }); // C
            _orderBook.QueueOrder(order1);

            var order2 = new Order();
            order2.OrderLines.Add(new OrderLine { Item = item2, Quantity = 1 }); // B
            _orderBook.QueueOrder(order2);

            QueuedOrders.Clear();
            foreach (var o in _orderBook.QueuedOrders)
                QueuedOrders.Add(o);

            AppendStatus("App ready. Click Process Next Order.");
        }

        private async void ProcessNext_Click(object? sender, RoutedEventArgs e)
        {
            if (_isBusy)
            {
                AppendStatus("Busy; ignoring click.");
                return;
            }
            _isBusy = true;

            try
            {
                AppendStatus($"Queued orders (internal): {_orderBook.QueuedOrders.Count}");

                var processed = _orderBook.ProcessNextOrder(_inventory);
                if (processed is null)
                {
                    AppendStatus("No order processed (empty queue or insufficient stock).");
                    return;
                }

                AppendStatus($"Processing order with {processed.OrderLines.Count} lines...");

                foreach (var line in processed.OrderLines)
                {
                    AppendStatus($"Line: {line.Item.Name}, qty={line.Quantity}, loc={line.Item.InventoryLocation}");

                    for (int i = 0; i < line.Quantity; i++)
                    {
                        if (line.Item.InventoryLocation > 0)
                        {
                            // FIX: InventoryLocation is uint -> cast to int for PickUp
                            _robot.PickUp((int)line.Item.InventoryLocation);
                            AppendStatus($"→ Sent pickup for {line.Item.Name} (loc {line.Item.InventoryLocation}) #{i + 1}");
                            await Task.Delay(9500); // wait for each motion
                        }
                        else
                        {
                            AppendStatus($"Skipping {line.Item.Name} – no InventoryLocation.");
                        }
                    }
                }

                if (QueuedOrders.Count > 0)
                    QueuedOrders.RemoveAt(0);

                ProcessedOrders.Add(processed);
                Raise(nameof(TotalRevenue));

                AppendStatus("✅ Order complete.");
            }
            catch (Exception ex)
            {
                AppendStatus("⚠️ Robot error: " + ex.Message);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void TestRobotMove_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // single back-and-forth so it doesn't look like a triple-run bug
                const string prog = @"
def f():
  p1 = p[.3, -.3, .1, 0, -3.1415, 0]
  p2 = p[.2, -.3, .1, 0, -3.1415, 0]
  movej(get_inverse_kin(p1))
  movej(get_inverse_kin(p2))
end
f()
";
                _robot.SendUrscript(prog);
                AppendStatus("Test Robot Move sent.");
                await Task.Delay(9500);
                AppendStatus("Test Robot Move finished.");
            }
            catch (Exception ex)
            {
                AppendStatus("Test move failed: " + ex.Message);
            }
        }

        private void PingRobot_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                const string ping = @"
def ping():
  textmsg(""PING from C# program"")
end
ping()
";
                _robot.SendUrscript(ping);
                AppendStatus("Ping sent. Check URSim → Log tab for 'PING'.");
            }
            catch (Exception ex)
            {
                AppendStatus("Ping failed: " + ex.Message);
            }
        }

        private void AppendStatus(string line)
        {
            if (StatusMessages is null) return;
            var prefix = StatusMessages.Text?.Length > 0 ? Environment.NewLine : string.Empty;
            StatusMessages.Text += prefix + line;
        }
    }
}
