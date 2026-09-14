package com.vigileye.companion.geofence.repo

import android.content.Context
import android.util.Log
import com.vigileye.companion.geofence.LocationTracker
import com.vigileye.companion.geofence.model.LocationEvent

/**
 * VE-020: Repository for location/geofence events.
 * Emits metadata-only events; never stores raw GPS.
 */
class LocationEventRepository(private val context: Context) {
    private val TAG = "LocationEventRepo"
    private val tracker = LocationTracker(context)

    fun startTracking() {
        Log.i(TAG, "Starting location tracking (metadata-only)")
        tracker.startLocationTracking()
    }

    fun addGeofence(
        geofenceId: String,
        lat: Double,
        lng: Double,
        radiusMeters: Float = 100f,
        expirationHours: Int = 24
    ) {
        Log.i(TAG, "Adding geofence: $geofenceId at ($lat,$lng) r=${radiusMeters}m")
        tracker.addGeofence(geofenceId, lat, lng, radiusMeters, expirationHours)
    }
}
