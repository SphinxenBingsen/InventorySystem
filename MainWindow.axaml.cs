using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using InventorySystem.Data;

namespace InventorySystem
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
       
        public ObservableCollection<Order> QueuedOrders { get; } = new();
        public ObservableCollection<Order> ProcessedOrders { get; } = new();

       
        private readonly InventoryContext _db = new();
        private OrderBook _orderBook = null!;
        private readonly Inventory _inventory = new();
        private readonly ItemSorterRobot _robot = new();

        private bool _isBusy;

        public decimal TotalRevenue => _orderBook?.TotalRevenue() ?? 0m;

        public new event PropertyChangedEventHandler? PropertyChanged;
        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

          
            _robot.IpAddress = "127.0.0.1";

          
            ReadDatabase();
            RefreshBindings();

            AppendStatus("App ready. Click Process Next Order.");
        }

        
        private void ReadDatabase()
        {
           
            _orderBook = _db.OrderBooks
                .Include(ob => ob.QueuedOrders)
                    .ThenInclude(o => o.OrderLines)
                        .ThenInclude(ol => ol.Item)
                .Include(ob => ob.ProcessedOrders)
                    .ThenInclude(o => o.OrderLines)
                        .ThenInclude(ol => ol.Item)
                .First(); 

          
            _inventory.Stock.Clear();
            _inventory.Stock.AddRange(_db.Items.ToList());
        }

       
        private void RefreshBindings()
        {
            QueuedOrders.Clear();
            foreach (var o in _orderBook.QueuedOrders)
                QueuedOrders.Add(o);

            ProcessedOrders.Clear();
            foreach (var o in _orderBook.ProcessedOrders)
                ProcessedOrders.Add(o);

            Raise(nameof(TotalRevenue));
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
                AppendStatus($"Queued orders (DB): {_orderBook.QueuedOrders.Count}");

                if (_orderBook.QueuedOrders.Count == 0)
                {
                    AppendStatus("No order in queue.");
                    return;
                }

                var processed = _orderBook.QueuedOrders[0];

               
                foreach (var line in processed.OrderLines)
                {
                    if (line.Item.Quantity < line.Quantity)
                    {
                        AppendStatus($"Not enough stock for {line.Item.Name}. Needed {line.Quantity}, have {line.Item.Quantity}.");
                        return;
                    }
                }

                AppendStatus($"Processing order with {processed.OrderLines.Count} lines...");

                
                foreach (var line in processed.OrderLines)
                {
                    AppendStatus($"Line: {line.Item.Name}, qty={line.Quantity}, loc={line.Item.InventoryLocation}");

                    for (int i = 0; i < line.Quantity; i++)
                    {
                        if (line.Item.InventoryLocation > 0)
                        {
                            _robot.PickUp((int)line.Item.InventoryLocation);
                            AppendStatus($"→ Sent pickup for {line.Item.Name} (loc {line.Item.InventoryLocation}) #{i + 1}");
                            await Task.Delay(9500); 
                        }
                        else
                        {
                            AppendStatus($"Skipping {line.Item.Name} – no InventoryLocation.");
                        }
                    }

                    
                    _inventory.TryConsume(line.Item, line.Quantity);
                }

                
                _orderBook.QueuedOrders.RemoveAt(0);
                _orderBook.ProcessedOrders.Add(processed);

                
                _db.Update(_orderBook);
                _db.SaveChanges();

               
                RefreshBindings();

                AppendStatus("✅ Order complete (saved to DB).");
            }
            catch (Exception ex)
            {
                AppendStatus("⚠️ Robot/DB error: " + ex.Message);
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
