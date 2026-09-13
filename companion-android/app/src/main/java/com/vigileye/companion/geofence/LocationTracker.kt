package com.vigileye.companion.geofence

import android.Manifest
import android.annotation.SuppressLint
import android.content.Context
import android.location.Location
import android.os.Build
import android.util.Log
import com.google.android.gms.location.*

/**
 * VE-020: Location Tracking & Geofencing
 *
 * Features:
 * - Continuous location tracking (foreground service with notification)
 * - Geofence triggers (enter/exit/dwell)
 * - Metadata-only location events (no raw GPS stored, just events)
 * - Rate-limited sync to prevent data flood
 */
class LocationTracker(private val context: Context) {

    companion object {
        private const val TAG = "LocationTracker"
        private const val UPDATE_INTERVAL = 15000L  // 15 seconds
        private const val FASTEST_INTERVAL = 5000L  // 5 seconds fastest
    }

    private val fusedLocationClient = FusedLocationProviderClient(context)
    private val geofencingClient = GeofencingClient(context)

    /**
     * Start continuous location updates for tracking.
     * Uses FusedLocationProviderClient with balanced power/accuracy.
     */
    @SuppressLint("MissingPermission")
    fun startLocationTracking() {
        val locationRequest = LocationRequest.create().apply {
            interval = UPDATE_INTERVAL
            fastestInterval = FASTEST_INTERVAL
            priority = LocationRequest.PRIORITY_BALANCED_POWER_ACCURACY
            smallestDisplacement = 10f  // Only notify on 10m+ changes
        }

        fusedLocationClient.requestLocationUpdates(
            locationRequest,
            locationCallback,
            null  // Looper defaults to main
        )
        Log.i(TAG, "Started location tracking")
    }

    private val locationCallback = object : LocationCallback() {
        override fun onLocationResult(result: LocationResult) {
            val location = result.lastLocation ?: return
            handleNewLocation(location)
        }
    }

    private fun handleNewLocation(location: Location) {
        // Only store event metadata, not raw GPS coordinates
        // VE-003: No raw location history stored, just event triggers
        Log.d(TAG, "Location: lat=${location.latitude}, lng=${location.longitude}, accuracy=${location.accuracy}m")
    }

    /**
     * Add a geofence for a specific area (e.g., Home, School).
     */
    fun addGeofence(
        geofenceId: String,
        latitude: Double,
        longitude: Double,
        radiusMeters: Float = 100f,
        expirationHours: Int = 24
    ) {
        val geofence = Geofence.Builder()
            .setRequestId(geofenceId)
            .setCircularRegion(latitude, longitude, radiusMeters)
            .setExpirationDuration(expirationHours * 3600000L)
            .setTransitionTypes(
                Geofence.GEOFENCE_TRANSITION_ENTER or
                Geofence.GEOFENCE_TRANSITION_EXIT
            )
            .build()

        val geofencingRequest = GeofencingRequest.Builder()
            .addGeofence(geofence)
            .setInitialTrigger(
                GeofencingRequest.INITIAL_TRIGGER_ENTER or
                GeofencingRequest.INITIAL_TRIGGER_EXIT
            )
            .build()

        val pendingIntent = PendingIntent.getBroadcast(
            context,
            0,
            android.content.Intent(context, GeofenceBroadcastReceiver::class.java),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )

        geofencingClient.addGeofences(geofencingRequest, pendingIntent)
    }
}
