# Vigil Eye - Windows Dashboard

A native Windows desktop application for parents and guardians to monitor their children's device activity.

## Status

**Phase 1 MVP** - In Progress (VE-014 through VE-018)

## Tech Stack

- **Framework:** WinUI 3 (.NET 8)
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **UI:** Native Windows controls for WCAG 2.1 AA accessibility compliance
- **Networking:** HttpClient with certificate pinning (VE-024)

## Features Implemented

### VE-014: Unified Activity Feed
- Chronological feed of call, message, location, and app usage events
- Filterable by date range and search query
- Real-time updates via WebSocket

### VE-015: Multi-Child Switcher
- Switch between linked children from one account
- Persistent state across sessions
- Filtered views per child

### VE-016: Guardian Authentication with Mandatory MFA
- Password + TOTP/push MFA required
- No permanent opt-out (security requirement)
- Account recovery cannot bypass MFA

### VE-024: Certificate Pinning
- REST API calls use pinned certificates
- WebSocket channel also pinned
- Prevents MITM attacks on parent dashboard

## Building

```bash
cd dashboard-windows
dotnet restore
dotnet build
```

## Project Structure

```
VigilEye.Dashboard/
├── ViewModels/
│   ├── MainViewModel.cs      # VE-014, VE-015
│   └── LoginViewModel.cs     # VE-016
├── Network/
│   └── ApiClient.cs          # VE-024: Certificate pinning
├── Views/
│   ├── MainPage.xaml
│   └── LoginPage.xaml
└── Assets/
    └── icon.ico
```

## Non-Negotiable Invariants

✅ **No hidden install mode** - Indicator cannot be suppressed  
✅ **Consent attestation** - Server-enforced before pairing  
✅ **v1 metadata-only** - No SMS content in database  
✅ **MFA mandatory** - No opt-out allowed  
✅ **Certificate pinning** - MITM protection on dashboard

## Related Tickets

- VE-014: Unified activity feed
- VE-015: Multi-child switcher & call log viewer
- VE-016: Guardian authentication with mandatory MFA
- VE-017: App usage & screen time reporting
- VE-018: Baseline tamper/uninstall alert
- VE-024: Extend certificate pinning to Windows dashboard

---
**Status:** Phase 1 MVP Implementation in Progress