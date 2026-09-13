using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VigilEye.Dashboard.ViewModels;

/**
 * VE-014: Unified Activity Feed
 * VE-015: Multi-child Switcher
 *
 * Main ViewModel for the dashboard.
 * Displays chronological feed of all events across linked children.
 * Supports switching between children from one account.
 */
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedChildId = string.Empty;

    [ObservableProperty]
    private string _selectedChildName = "All Children";

    [ObservableProperty]
    private ObservableCollection<ActivityItem> _activityFeed = new();

    [ObservableProperty]
    private ObservableCollection<ChildProfile> _linkedChildren = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private DateTime? _filterStartDate;

    [ObservableProperty]
    private DateTime? _filterEndDate;

    // VE-016: Authentication state
    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    public MainViewModel()
    {
        // Load children on initialization
        // In production, this would call the API
        LoadMockData();
    }

    /**
     * VE-015: Switch between children.
     * Updates all dashboard views consistently.
     */
    [RelayCommand]
    private async Task SelectChild(ChildProfile? child)
    {
        if (child == null)
        {
            SelectedChildId = string.Empty;
            SelectedChildName = "All Children";
        }
        else
        {
            SelectedChildId = child.Id;
            SelectedChildName = child.Name;
        }

        await RefreshActivityFeed();
    }

    /**
     * VE-014: Refresh activity feed with current filters.
     */
    [RelayCommand]
    private async Task RefreshActivityFeed()
    {
        IsLoading = true;

        try
        {
            // TODO: Call API to get events
            // var events = await _apiClient.GetEventsAsync(
            //     childId: SelectedChildId,
            //     startDate: FilterStartDate,
            //     endDate: FilterEndDate,
            //     searchQuery: SearchQuery
            // );

            // For now, use mock data
            await Task.Delay(500); // Simulate API latency

            ActivityFeed.Clear();

            // Filter by selected child if any
            var events = GetMockEvents()
                .Where(e => string.IsNullOrEmpty(SelectedChildId) || e.ChildId == SelectedChildId);

            foreach (var evt in events)
            {
                ActivityFeed.Add(evt);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        await RefreshActivityFeed();
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        SearchQuery = string.Empty;
        FilterStartDate = null;
        FilterEndDate = null;
        await RefreshActivityFeed();
    }

    private void LoadMockData()
    {
        LinkedChildren.Add(new ChildProfile { Id = "1", Name = "Emma", Age = 12 });
        LinkedChildren.Add(new ChildProfile { Id = "2", Name = "Jack", Age = 15 });

        foreach (var evt in GetMockEvents())
        {
            ActivityFeed.Add(evt);
        }
    }

    private List<ActivityItem> GetMockEvents()
    {
        return new List<ActivityItem>
        {
            new ActivityItem
            {
                Id = "1",
                ChildId = "1",
                ChildName = "Emma",
                Type = ActivityType.Call,
                Title = "Incoming Call",
                Description = "From: +1 555-0123 (Grandma)",
                Timestamp = DateTime.Now.AddMinutes(-15),
                Duration = "5:23"
            },
            new ActivityItem
            {
                Id = "2",
                ChildId = "1",
                ChildName = "Emma",
                Type = ActivityType.Location,
                Title = "Arrived at Home",
                Description = "Geofence: Home",
                Timestamp = DateTime.Now.AddMinutes(-30)
            },
            new ActivityItem
            {
                Id = "3",
                ChildId = "2",
                ChildName = "Jack",
                Type = ActivityType.AppUsage,
                Title = "YouTube",
                Description = "45 minutes today",
                Timestamp = DateTime.Now.AddHours(-1)
            },
            new ActivityItem
            {
                Id = "4",
                ChildId = "1",
                ChildName = "Emma",
                Type = ActivityType.SmsMeta,
                Title = "SMS to Friend",
                Description = "To: +1 555-0456",
                Timestamp = DateTime.Now.AddHours(-2)
            }
        };
    }
}

public partial class ChildProfile : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public partial class ActivityItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string ChildId { get; set; } = string.Empty;
    public string ChildName { get; set; } = string.Empty;
    public ActivityType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Duration { get; set; }
}

public enum ActivityType
{
    Call,
    SmsMeta,
    Location,
    AppUsage,
    WebHistory,
    DeviceStatus,
    TamperDetected,
    Alert
}