package com.vigileye.companion.receiver

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.google.android.gms.location.Geofence
import com.google.android.gms.location.GeofencingEvent

/**
 * VE-020: Geofence transition receiver.
 * Receives enter/exit/dwell events from GeofencingClient.
 * Only emits metadata events (geofence id + transition type) — no raw GPS.
 */
class GeofenceBroadcastReceiver : BroadcastReceiver() {
    private val TAG = "GeofenceBroadcastReceiver"

    override fun onReceive(context: Context, intent: Intent) {
        val event = GeofencingEvent.fromIntent(intent) ?: return
        if (event.hasError()) {
            Log.e(TAG, "Geofence error: ${event.errorCode}")
            return
        }

        val geofenceTransition = event.geofenceTransition
        for (geofence in event.triggeringGeofences ?: emptyList()) {
            val geofenceId = geofence.requestId
            Log.i(TAG, "Geofence event: $geofenceId transition=$geofenceTransition")
            // VE-003 / VE-020: Emit metadata-only event to sync queue
            // (Body excluded per Phase 0 VE-003 — metadata only until legal sign-off)
        }
    }
}
