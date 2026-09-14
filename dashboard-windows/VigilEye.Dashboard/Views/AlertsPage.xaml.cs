using Microsoft.UI.Xaml.Controls;

namespace VigilEye.Dashboard.Views;

public sealed partial class AlertsPage : Page
{
    public AlertsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// VE-021: Load alerts feed from alert-engine (metadata-only).
    /// Per VE-003 / invariants.md: never expose raw message content; rule-triggered events only.
    /// </summary>
    private async void LoadAlerts()
    {
        // VE-021: Wire to backend/alert-engine/Controllers/AlertsController
        // Per VE-003 / invariants.md: metadata-only feed — alert type, timestamp, device id.
        // No raw message content exposed; rule-triggered events only.
        try
        {
            var client = Network.ApiClient.Instance;
            // Fetch alert rules (scaffold endpoint; full engine in Phase 2)
            await client.GetAsync<object>("/api/alerts/health");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VE-021] LoadAlerts wire error: {ex.Message}");
        }
    }
}
