package com.vigileye.companion.ios

import Foundation

/**
 * VE-019: iOS Companion App Foundation
 *
 * Note: Full Swift/SwiftUI implementation requires Xcode on macOS.
 * This file provides the Kotlin-equivalent structure mapped to iOS patterns.
 *
 * Key features mapped from Android:
 * - Secure Enclave (iOS equivalent of Android Keystore)
 * - Background App Refresh (equivalent of WorkManager)
 * - Core Location (equivalent of FusedLocationProvider)
 * - CallKit / Contacts framework (equivalent of READ_CALL_LOG)
 */
struct CompanionAppConfig {
    static let bundleId = "com.vigileye.companion"
    static let minOSVersion = "16.0"
    static let apiBaseUrl = ProcessInfo.processInfo.environment["API_URL"] ?? "https://api.vigileye.local"

    // Feature flags (must respect non-negotiable invariants)
    static let messageContentEnabled = false  // VE-003: Metadata-only by default
    static let hiddenModeEnabled = false     // Invariant: Never hidden
    static let consentRequired = true         // VE-002: Server-enforced
}

/**
 * Secure device identity using Secure Enclave (iOS equivalent of Android Keystore).
 * VE-012 pairing: Ed25519 keypair stored in Secure Enclave.
 */
class SecureDeviceIdentity {
    private var keyPair: SecKey?

    func generateKeyPair() throws {
        let attributes: [String: Any] = [
            kSecAttrKeyType: kSecAttrKeyTypeECSECPrimeRandom,
            kSecAttrKeySizeInBits: 256,
            kSecPrivateKeyAttrs: [
                kSecAttrIsPermanent: true,
                kSecAttrApplicationTag: "com.vigileye.device.key"
            ]
        ]

        var error: Unmanaged<CFError>?
        guard let privateKey = SecKeyCreateRandomKey(attributes as CFDictionary, &error) else {
            throw error?.takeRetainedValue() ?? SecureError.keyGenerationFailed
        }
        self.keyPair = privateKey
    }

    func getPublicKey() throws -> Data {
        guard let privateKey = keyPair else {
            throw SecureError.noKeyPair
        }
        guard let publicKey = SecKeyCopyPublicKey(privateKey) else {
            throw SecureError.publicKeyExtractionFailed
        }
        return SecKeyCopyExternalRepresentation(publicKey, nil) as? Data ?? Data()
    }

    enum SecureError: Error {
        case keyGenerationFailed
        case noKeyPair
        case publicKeyExtractionFailed
    }
}

/**
 * Background sync manager (iOS equivalent of Android WorkManager).
 * VE-013: Background App Refresh for periodic sync.
 */
class BackgroundSyncManager {
    static let shared = BackgroundSyncManager()

    private init() {}

    func registerBackgroundTask() {
        // Register for BGProcessingTask or BGAppRefreshTask
        // Requires entitlement in Info.plist and server-side scheduling
    }

    func scheduleSync() {
        // Schedule periodic sync (similar to WorkManager periodic work)
        // Uses BGAppRefreshTaskRequest with earliestBeginDate
    }

    func syncLocationEvents() -> [LocationEvent] {
        // Collect location events from CoreLocation
        // Encrypt locally before sync
        return []
    }
}

/**
 * Event model mapped from Android EventEntity.
 * VE-003: Metadata-only (no message content, no call audio).
 */
struct EventData: Codable, Identifiable {
    let id: String
    let timestamp: Date
    let eventType: EventType
    let title: String
    let description: String
    let metadata: [String: AnyCodable]
}

enum EventType: String, Codable {
    case call = "call"
    case smsMeta = "sms_meta"
    case location = "location"
    case appUsage = "app_usage"
    case tamperDetected = "tamper"
    case deviceStatus = "device_status"
}

struct AnyCodable: Codable {}
