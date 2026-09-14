package com.vigileye.companion.service

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.os.Build
import android.os.IBinder
import androidx.core.app.NotificationCompat
import com.vigileye.companion.geofence.LocationTracker
import com.vigileye.companion.geofence.model.LocationEvent
import com.vigileye.companion.MainActivity
import com.vigileye.companion.R

/**
 * VE-010: Persistent, non-removable on-device monitoring indicator.
 *
 * This Foreground Service displays a permanent notification that:
 * 1. Informs the user that monitoring is active
 * 2. Cannot be dismissed or hidden
 * 3. Is required by law to prevent stalkerware classification
 *
 * NON-NEGOTIABLE INVARIANT #1: There must be NO code path — in the client,
 * backend API, or admin tooling — that can disable or suppress this indicator.
 *
 * Security Assessment §4.3: The visible on-device status indicator is enforced
 * by the app itself (not just a UI toggle a parent can hide) — no code path
 * exists to suppress it, and its absence itself triggers a tamper alert.
 */
class MonitoringIndicatorService : Service() {

    companion object {
        private const val NOTIFICATION_ID = 1001
        private const val CHANNEL_ID = "vigileye_monitoring"
        private const val CHANNEL_NAME = "Monitoring Active"

        /**
         * Starts the monitoring indicator service.
         * Called after successful pairing (VE-012).
         */
        fun start(context: Context) {
            val intent = Intent(context, MonitoringIndicatorService::class.java)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                context.startForegroundService(intent)
            } else {
                context.startService(intent)
            }
        }

        /**
         * WARNING: VE-010 / Invariant #1
         *
         * There is intentionally NO public stop() method.
         *
         * The indicator can only be stopped by:
         * 1. User uninstalling the app (triggers VE-018 tamper alert)
         * 2. Guardian explicitly revoking the device from dashboard
         *
         * Any attempt to add a stop() method or disable the notification
         * violates the anti-stalkerware design and must be flagged in code review.
         */
    }

    override fun onCreate() {
        super.onCreate()
        createNotificationChannel()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        // Start as foreground service with persistent notification
        val notification = createMonitoringNotification()
        startForeground(NOTIFICATION_ID, notification)

        // VE-020: Start location tracking (metadata-only, no raw GPS)
        val tracker = LocationTracker(this)
        tracker.startLocationTracking()

        // Return sticky to ensure service restarts if killed by system
        return START_STICKY
    }

    override fun onBind(intent: Intent?): IBinder? {
        // This service does not support binding
        return null
    }

    /**
     * Creates the notification channel (Android O+).
     * High importance to ensure it's visible and non-dismissible.
     */
    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                CHANNEL_NAME,
                NotificationManager.IMPORTANCE_HIGH  // VE-010: High importance = always visible
            ).apply {
                description = "Indicates that parental monitoring is active on this device"
                setShowBadge(true)
                // Cannot be dismissed by user
                lockscreenVisibility = Notification.VISIBILITY_PUBLIC
            }

            val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            notificationManager.createNotificationChannel(channel)
        }
    }

    /**
     * Creates the persistent monitoring notification.
     *
     * Notification text is intentionally clear and non-technical to meet
     * transparency requirements (PRD §7, Security Assessment §4.3).
     */
    private fun createMonitoringNotification(): Notification {
        // Intent to open app when notification is tapped
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        val pendingIntent = PendingIntent.getActivity(
            this,
            0,
            intent,
            PendingIntent.FLAG_IMMUTABLE
        )

        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("Parental Monitoring Active")
            .setContentText("This device is being monitored by a parent or guardian")
            .setSmallIcon(R.drawable.ic_monitoring_indicator)  // VE-010: Custom icon
            .setContentIntent(pendingIntent)
            .setOngoing(true)  // VE-010: Cannot be dismissed
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setCategory(NotificationCompat.CATEGORY_STATUS)
            .setShowWhen(false)
            .build()
    }

    override fun onDestroy() {
        super.onDestroy()

        // VE-018: Tamper detection
        // If this service is destroyed unexpectedly (not by explicit unpairing),
        // trigger a tamper alert to the backend
        // TODO: Implement tamper alert in Phase 1
    }
}
