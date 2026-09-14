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
    private void LoadAlerts()
    {
        // TODO: Wire to backend/alert-engine/Controllers/AlertsController
        // Metdata-only feed: alert type, timestamp, device id — no content
    }
}
