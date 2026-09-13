# Vigil Eye Phase 1 Implementation Plan

**Date:** 2026-09-13  
**Phase:** Phase 1 MVP  
**Target:** Weeks 4-10 per PRD milestones  
**Platform Focus:** Android companion app + Windows dashboard

---

## Phase 1 Tickets (VE-009 through VE-018)

### Android Companion App

**VE-009: Android guided setup & permission grants** (P0)
- Guided onboarding flow
- OS permission requests (location, call log, usage access)
- Complete within 15 minutes (KPI target)

**VE-010: Persistent, non-removable on-device monitoring indicator** (P0)
- Foreground Service with persistent notification
- No code path to suppress indicator
- Tamper alert if indicator removed

**VE-011: Call log collection & sync** (P0)
- Collect call metadata (contact, duration, direction)
- Sync to backend via batch upload
- Offline queue with retry

**VE-012: Device pairing (QR + fallback code)** (P0)
- Scan QR code from dashboard
- 8-char fallback code entry
- Ed25519 keypair generation in Android Keystore
- Exchange for access/refresh tokens

**VE-013: Local encrypted queue & background sync** (P0)
- SQLite + SQLCipher for local storage
- WorkManager for periodic sync (5-15 min)
- Near-real-time for location events (<2 min)

### Windows Dashboard

**VE-014: Unified activity feed** (P0)
- Chronological feed of all events
- Multi-child support
- Load time <2 seconds

**VE-015: Multi-child switcher & call log viewer** (P0)
- Switch between children from one account
- Filterable call log viewer
- Persistent state across session

**VE-016: Guardian authentication with mandatory MFA** (P0)
- Password + TOTP/push MFA
- Required at every new session
- No permanent opt-out

**VE-017: App usage & screen time reporting (v1)** (P0)
- Daily/weekly time-per-app
- Install/uninstall events
- Visible within one sync cycle

**VE-018: Baseline tamper/uninstall alert** (P1)
- Alert when app disabled/uninstalled
- Alert when permissions revoked
- Historical event preserved

---

## Implementation Order

### Week 1-2: Android Foundation
1. Create Android Studio project (Kotlin)
2. VE-010: Foreground Service with persistent indicator
3. VE-013: SQLite + SQLCipher local storage
4. VE-013: WorkManager background sync setup

### Week 2-3: Android Pairing & Data Collection
1. VE-012: QR code scanner + fallback code entry
2. VE-012: Ed25519 keypair generation (Android Keystore)
3. VE-009: Permission grant flow
4. VE-011: Call log collection

### Week 3-4: Windows Dashboard Foundation
1. Create WinUI 3 project (.NET 8)
2. VE-016: Authentication (password + MFA)
3. VE-014: Activity feed UI
4. VE-015: Multi-child switcher

### Week 4-5: Dashboard Features
1. VE-015: Call log viewer
2. VE-017: App usage reporting
3. VE-018: Tamper alert display
4. End-to-end testing

---

## Technical Stack (Locked)

**Android:**
- Language: Kotlin
- Min SDK: API 29 (Android 10+)
- Architecture: MVVM with Jetpack components
- Background: WorkManager + Foreground Service
- Storage: Room + SQLCipher
- Location: FusedLocationProviderClient
- Networking: Retrofit + OkHttp (with cert pinning)

**Windows:**
- Framework: WinUI 3 (.NET 8)
- Architecture: MVVM Community Toolkit
- Networking: HttpClient with certificate pinning
- State: Local SQLite for cached data
- WCAG 2.1 AA accessibility

---

## Next Step

Starting with VE-010 (persistent indicator) since it's the most critical anti-stalkerware control and must be built correctly from the start.

Creating Android project structure now...
