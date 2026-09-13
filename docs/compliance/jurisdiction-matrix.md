# Vigil Eye - Jurisdictional Compliance Matrix

**Status:** DRAFT — Requires Legal sign-off  
**Owner:** Legal (with Engineering support)  
**Prepared:** 2026-09-13  
**Related Ticket:** VE-001

---

## Purpose

This document maps Vigil Eye features to jurisdictional legal requirements. It is scaffolded by Engineering for Legal to complete before Phase 1 MVP launch.

**VE-001 Acceptance Criteria:**
- Legal sign-off obtained and documented per target launch jurisdiction
- Findings mapped to specific PRD features (call/SMS access, location tracking)
- Any jurisdiction where a feature cannot ship is flagged for regional feature-gating

---

## Feature → Legal Requirement Mapping

| Feature | Legal Domain | Status | Notes |
|---------|-------------|--------|-------|
| **Call Log Metadata** (incoming/outgoing/missed, contact, duration) | Wiretap/eavesdropping statutes | ⏸️ PENDING LEGAL | U.S.: ECPA Title III; State-level all-party consent laws |
| **SMS/Message Metadata** (sender, recipient, timestamp) | Wiretap/eavesdropping statutes | ⏸️ PENDING LEGAL | v1 ships metadata-only per VE-003 |
| **SMS/Message Content** (body text) | Wiretap/eavesdropping statutes | 🚫 BLOCKED IN v1 | Security Assessment R-2 (Critical): Most jurisdictions require all-party consent; minor's consent typically insufficient. **Feature-gated pending legal sign-off.** |
| **Location Tracking** (live + 30-day history) | Privacy law (COPPA, GDPR children's data) | ⏸️ PENDING LEGAL | U.S.: COPPA parental consent; EU/UK: GDPR Art. 8 children's data provisions |
| **Geofencing** (safe zone alerts) | Privacy law | ⏸️ PENDING LEGAL | Same as location tracking |
| **App Usage & Screen Time** | Privacy law, app store policies | ⏸️ PENDING LEGAL | Google Play / Apple App Store monitoring-app policies |
| **Web Browsing History** | Privacy law | ⏸️ PENDING LEGAL | Depends on browser data access method |
| **Guardian Consent Attestation** | Trust & Safety, app store policies | ✅ IMPLEMENTED | VE-002: Server-enforced, immutable attestation |
| **On-Device Monitoring Indicator** | Trust & Safety, anti-stalkerware | ✅ IMPLEMENTED | VE-010: Persistent, non-removable indicator (Phase 1) |

---

## Jurisdictional Analysis (TO BE COMPLETED BY LEGAL)

### Target Launch Jurisdictions

**❓ OPEN QUESTION (PRD §15):** Which regions/markets are the initial launch targets?

Engineering needs this decision to configure feature flags and regional gating.

### United States

#### Federal Level

- **ECPA (Electronic Communications Privacy Act), Title III**
  - **Applies to:** Call metadata, SMS content
  - **Requirement:** _[Legal to complete]_
  - **Can we ship?** ⏸️ PENDING
  
- **COPPA (Children's Online Privacy Protection Act)**
  - **Applies to:** All data collected about children <13
  - **Requirement:** Verifiable parental consent (already satisfied by guardian attestation VE-002)
  - **Can we ship?** ⏸️ PENDING — verify consent mechanism meets COPPA standards

#### State Level (varies by state)

- **All-Party Consent States** (e.g., CA, FL, PA, WA)
  - **Applies to:** SMS/call content capture
  - **Requirement:** _[Legal to complete — does minor's consent substitute? Does guardian consent suffice?]_
  - **Can we ship call content?** 🚫 NO (v1 metadata-only per VE-003)
  - **Can we ship SMS content?** 🚫 NO (v1 metadata-only per VE-003)

### European Union / United Kingdom

- **GDPR Article 8** (Children's data protection)
  - **Applies to:** All features
  - **Requirement:** _[Legal to complete]_
  - **Can we ship?** ⏸️ PENDING

- **ePrivacy Directive**
  - **Applies to:** Electronic communications monitoring
  - **Requirement:** _[Legal to complete]_
  - **Can we ship?** ⏸️ PENDING

### Other Jurisdictions

_[Legal to add target markets]_

---

## App Store Policy Compliance

### Google Play Store

- **Monitoring App Policy:** _[Legal to complete — current policy as of 2026-09]_
- **Required disclosures:** _[Legal to complete]_
- **Prohibited features:** _[Legal to complete]_

### Apple App Store

- **Monitoring App Policy:** _[Legal to complete — current policy as of 2026-09]_
- **Required disclosures:** _[Legal to complete]_
- **Prohibited features:** _[Legal to complete]_

---

## Feature Gating Strategy (Engineering Implementation)

Features with legal uncertainty are gated behind configuration flags:

```csharp
// Example: VE-003 message content feature flag
public class FeatureFlags
{
    // Defaulted to FALSE pending legal sign-off (VE-001)
    public bool EnableMessageContentCapture { get; set; } = false;
    
    // Regional feature availability (set per jurisdiction after legal review)
    public Dictionary<string, bool> EnableCallMetadataByRegion { get; set; } = new();
    public Dictionary<string, bool> EnableLocationTrackingByRegion { get; set; } = new();
}
```

**Process:**
1. Legal completes this matrix for each target jurisdiction
2. Engineering updates feature flags per jurisdiction
3. Backend API enforces flags server-side (not just UI)
4. Dashboard shows guardian which features are available in their region

---

## Sign-Off

| Role | Name | Date | Status |
|------|------|------|--------|
| Legal Counsel (U.S.) | _[TBD]_ | _[TBD]_ | ⏸️ PENDING |
| Legal Counsel (EU/UK) | _[TBD]_ | _[TBD]_ | ⏸️ PENDING |
| Product Lead | _[TBD]_ | _[TBD]_ | ⏸️ PENDING |
| Engineering Lead | _[TBD]_ | _[TBD]_ | ⏸️ PENDING |

---

## References

- Vigil Eye PRD v1.0 §7 (Legal, Ethical & Trust Considerations)
- Vigil Eye Security Assessment v1.0 §7 (Compliance & Legal Considerations)
- Security Assessment Risk R-2 (Critical): SMS content capture legal exposure
- Ticket VE-001: Jurisdictional legal review & sign-off
- Ticket VE-003: Ship v1 messaging as metadata-only
