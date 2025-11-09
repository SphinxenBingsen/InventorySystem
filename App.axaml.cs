using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using InventorySystem.Data;

namespace InventorySystem;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        using (var db = new InventoryContext())
        {
            db.Database.EnsureCreated();
            SeedIfEmpty(db);
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }


    private static void SeedIfEmpty(InventoryContext db)
    {
        var orderBook = db.OrderBooks.FirstOrDefault();
        if (orderBook is null)
        {
            orderBook = new OrderBook { OrderBookId = 1 };
            db.OrderBooks.Add(orderBook);
            db.SaveChanges();
        }


        if (!db.Items.Any())
        {
            var screwdriver = new UnitItem { Name = "Screwdriver", PricePerUnit = 10m, Quantity = 100, Weight = 0.2m };
            var hammer = new UnitItem { Name = "Hammer", PricePerUnit = 25m, Quantity = 50, Weight = 0.9m };
            var sugar = new BulkItem { Name = "Sugar", PricePerUnit = 3m, Quantity = 30, MeasurementUnit = "kg" };

            db.Items.AddRange(screwdriver, hammer, sugar);
            db.SaveChanges();

            var order1 = new Order { CustomerName = "Ramanda" };
            order1.OrderLines.Add(new OrderLine { Item = screwdriver, Quantity = 2 });
            order1.OrderLines.Add(new OrderLine { Item = sugar, Quantity = 1 });

            var order2 = new Order { CustomerName = "Totoro" };
            order2.OrderLines.Add(new OrderLine { Item = hammer, Quantity = 4 });

            orderBook.QueuedOrders.Add(order1);
            orderBook.QueuedOrders.Add(order2);

            db.SaveChanges();
        }
    }
}