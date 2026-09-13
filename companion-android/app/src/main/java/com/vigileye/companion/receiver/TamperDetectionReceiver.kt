package com.vigileye.companion.receiver

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.vigileye.companion.service.MonitoringIndicatorService

/**
 * VE-018: Tamper detection receiver.
 *
 * Detects when the companion app is tampered with:
 * - Package replaced (app updated)
 * - My package replaced (forced reinstall)
 * - Boot completed (to restart monitoring indicator)
 *
 * Security Assessment §4.3: The absence of the indicator itself triggers
 * a tamper alert to the parent dashboard.
 */
class TamperDetectionReceiver : BroadcastReceiver() {

    companion object {
        private const val TAG = "TamperDetection"
    }

    override fun onReceive(context: Context?, intent: Intent?) {
        if (context == null || intent == null) return

        when (intent.action) {
            Intent.ACTION_BOOT_COMPLETED -> {
                Log.i(TAG, "VE-018: Boot completed - restarting monitoring indicator")
                // VE-010: Restart the persistent indicator after boot
                MonitoringIndicatorService.start(context)

                // TODO: Send boot event to backend
                logTamperEvent(context, "device_boot")
            }

            Intent.ACTION_MY_PACKAGE_REPLACED -> {
                Log.i(TAG, "VE-018: Package replaced - app updated or reinstalled")
                // Restart monitoring indicator after update
                MonitoringIndicatorService.start(context)

                // TODO: Send reinstall event to backend
                logTamperEvent(context, "package_replaced")
            }

            Intent.ACTION_PACKAGE_REMOVED -> {
                Log.w(TAG, "VE-018: Package removed detected")
                // This shouldn't fire for our own package, but log if it does
                logTamperEvent(context, "package_removed")
            }
        }
    }

    private fun logTamperEvent(context: Context, eventType: String) {
        // TODO: Queue tamper event for sync to backend
        // The backend will alert the parent dashboard
        Log.i(TAG, "VE-018: Tamper event logged: $eventType")
    }
}
