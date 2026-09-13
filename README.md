# Vigil Eye

Family Device Safety & Parental Monitoring Platform

**Status:** Phase 0 (Pre-MVP) — Backend infrastructure and security foundations

## Overview

Vigil Eye is a transparent, legally-compliant parental monitoring platform that gives guardians visibility into their minor children's phone use. It consists of:

- **Windows Dashboard** (WinUI 3) — Parent-facing management interface
- **Android/iOS Companion App** — Installed on child's device with transparent monitoring indicator
- **Cloud Backend** — Microservices architecture for data ingestion, alerting, and storage

**Design Principles:**
- Transparent by design — persistent on-device indicator that cannot be hidden
- Legal compliance first — guardian consent attestation, no covert install mode
- Privacy-respecting — no camera/mic capture, no keystroke logging, metadata-only messaging in v1

## Project Structure

```
vigil-eye/
├── backend/              # Microservices (Docker containers)
│   ├── auth-pairing/     # Authentication & device pairing
│   ├── ingestion/        # Event data ingestion
│   ├── alert-engine/     # Rule evaluation & alerting
│   ├── notification/     # Push notification dispatch
│   ├── reporting/        # Export & reporting
│   └── shared/           # Common libraries
├── companion-android/    # Android companion app (Kotlin)
├── companion-ios/        # iOS companion app (Swift) 
├── dashboard-windows/    # Windows dashboard (WinUI 3 / .NET 8)
├── docs/                 # Documentation & compliance artifacts
└── migrations/           # Database schema versions
```

## Tech Stack

- **Backend:** REST/JSON, containerized microservices, PostgreSQL + TimescaleDB + S3
- **Android:** Kotlin, WorkManager, Room + SQLCipher, FusedLocationProviderClient
- **iOS:** Swift, BGTaskScheduler, Core Data + Data Protection API
- **Windows:** WinUI 3 / .NET 8 for WCAG 2.1 AA accessibility compliance
- **Security:** TLS 1.2+, AES-256 at rest, envelope encryption via KMS, certificate pinning

## Phase 0 Implementation

Current focus: Backend services, data model, and blocking security items before Phase 1 MVP.

See `docs/tickets/` for detailed acceptance criteria per ticket (VE-001 through VE-008).

## Non-Negotiable Invariants

1. **No hidden install mode** — on-device indicator cannot be suppressed by any code path
2. **Consent attestation is server-enforced** — no pairing without immutable guardian attestation
3. **v1 is metadata-only for messages** — no SMS/message content capture pending legal review
4. **Tamper detection alerts** — indicator removal triggers parent notification
5. **Explicitly out of scope:** camera/mic capture, keystroke logging, adult monitoring without consent

## Getting Started (Local Dev)

```bash
# Start backend services
docker-compose up -d

# Run migrations
cd backend && ./scripts/migrate.sh

# Verify services
curl http://localhost:8080/health
```

Detailed setup instructions: `docs/development-setup.md` (coming soon)

## Documentation

- [Product Requirements (PRD)](./docs/Vigil%20Eye%20-%20PRD.docx)
- [Technical Requirements (TRD)](./docs/Vigil%20Eye%20-%20TRD.docx)
- [Security Assessment](./docs/Vigil%20Eye%20-%20Security%20Assessment.docx)
- [Ticket Backlog](./docs/Vigil%20Eye%20-%20Ticket%20Backlog.docx)

## License

Proprietary — All rights reserved

---

**Prepared:** September 13, 2026  
**Document Owner:** Engineering  
**Status:** Draft — Active Development
