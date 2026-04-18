using MobilityMonitor.Worker;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore; // Required for ToListAsync
using MobilityMonitor.Shared;        // Required to know what an AnomalyRecord is
using System.Linq;

namespace MobilityMonitor.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Use a slight delay to let the window render before hitting the DB
            Loaded += async (s, e) =>
            {
                StatusText.Text = "Window Loaded. Connecting to database...";
                await RunUiUpdateLoop();
            };
        }
        private async Task RunUiUpdateLoop()
        {
            while (true)
            {
                await RefreshTable();
                await Task.Delay(TimeSpan.FromSeconds(30)); // Refresh UI every 30s
            }
        }

        private async Task RefreshTable()
        {
            try
            {
                using var db = new MobilityDbContext();
                // Ensure the table exists (important for the very first run)
                db.Database.EnsureCreated();

                var data = await db.Anomalies
                    .OrderByDescending(a => a.DetectedAt)
                    .Take(100)
                    .ToListAsync();

                // Direct update because we are using async Task and awaiting it
                AnomalyGrid.ItemsSource = data;
                StatusText.Text = $"Last Update: {DateTime.Now:HH:mm:ss} | {data.Count} entries found.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Database Error: {ex.Message}";
            }
        }
    }
}