package com.vigileye.companion.geofence.model

/**
 * VE-020: Metadata-only location event.
 * Per VE-003 / Phase 0 blocking: no raw GPS coordinates stored.
 */
sealed class LocationEvent {
    data class GeofenceTransition(
        val geofenceId: String,
        val transitionType: Int // ENTER or EXIT
    ) : LocationEvent()

    data class TrackingStarted(val timestampMs: Long) : LocationEvent()
    data class TrackingStopped(val reason: String) : LocationEvent()
}
