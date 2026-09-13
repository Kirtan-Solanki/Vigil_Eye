package com.vigileye.companion.worker

import android.content.Context
import android.util.Log
import androidx.work.*
import com.vigileye.companion.data.local.EventDao
import com.vigileye.companion.data.local.EventDatabase
import com.vigileye.companion.network.ApiClient
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.util.concurrent.TimeUnit

/**
 * VE-013: Background sync using WorkManager.
 *
 * Syncs queued events to backend on a periodic schedule:
 * - Location events: Near-real-time (target: within 2 minutes)
 * - Other events: 5-15 minute batches to conserve battery
 *
 * TRD §10.1: Backend acknowledges receipt only after durable write,
 * so the client can safely clear its local queue.
 *
 * Battery target: ~5% additional daily drain (PRD §10)
 */
class SyncWorker(
    context: Context,
    workerParams: WorkerParameters
) : CoroutineWorker(context, workerParams) {

    companion object {
        private const val TAG = "SyncWorker"
        private const val WORK_NAME_PERIODIC = "vigileye_periodic_sync"
        private const val WORK_NAME_IMMEDIATE = "vigileye_immediate_sync"

        /**
         * Schedules periodic sync for non-urgent events.
         * Runs every 15 minutes (minimum interval for WorkManager).
         */
        fun schedulePeriodicSync(context: Context) {
            val constraints = Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .setBatteryNotLow(true)  // Respect battery constraints
                .build()

            val periodicWork = PeriodicWorkRequestBuilder<SyncWorker>(
                15, TimeUnit.MINUTES,
                5, TimeUnit.MINUTES  // Flex window
            )
                .setConstraints(constraints)
                .setBackoffCriteria(
                    BackoffPolicy.LINEAR,
                    30, TimeUnit.SECONDS
                )
                .addTag("periodic_sync")
                .build()

            WorkManager.getInstance(context).enqueueUniquePeriodicWork(
                WORK_NAME_PERIODIC,
                ExistingPeriodicWorkPolicy.KEEP,
                periodicWork
            )

            Log.d(TAG, "VE-013: Periodic sync scheduled (15 min interval)")
        }

        /**
         * Triggers immediate sync for location/geofence events.
         * VE-020: Target latency <2 minutes for location updates.
         */
        fun triggerImmediateSync(context: Context) {
            val constraints = Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .build()

            val immediateWork = OneTimeWorkRequestBuilder<SyncWorker>()
                .setConstraints(constraints)
                .addTag("immediate_sync")
                .build()

            WorkManager.getInstance(context).enqueueUniqueWork(
                WORK_NAME_IMMEDIATE,
                ExistingWorkPolicy.REPLACE,
                immediateWork
            )

            Log.d(TAG, "VE-013: Immediate sync triggered")
        }
    }

    override suspend fun doWork(): Result = withContext(Dispatchers.IO) {
        try {
            Log.d(TAG, "VE-013: Starting sync")

            // Get database instance (encryption key would be from Keystore in production)
            val encryptionKey = ByteArray(32) // TODO: Derive from Keystore
            val db = EventDatabase.getInstance(applicationContext, encryptionKey)
            val eventDao = db.eventDao()

            // Get unsynced events
            val unsyncedEvents = eventDao.getUnsyncedEvents()

            if (unsyncedEvents.isEmpty()) {
                Log.d(TAG, "VE-013: No events to sync")
                return@withContext Result.success()
            }

            Log.d(TAG, "VE-013: Syncing ${unsyncedEvents.size} events")

            // Group events by type for prioritized sync
            val locationEvents = unsyncedEvents.filter { it.eventType == "location" }
            val otherEvents = unsyncedEvents.filter { it.eventType != "location" }

            // Sync location events first (near-real-time requirement)
            if (locationEvents.isNotEmpty()) {
                val success = syncEvents(locationEvents, eventDao, prioritize = true)
                if (!success) {
                    Log.w(TAG, "VE-013: Location sync failed, will retry")
                    return@withContext Result.retry()
                }
            }

            // Sync other events in batches
            if (otherEvents.isNotEmpty()) {
                val success = syncEvents(otherEvents, eventDao, prioritize = false)
                if (!success) {
                    Log.w(TAG, "VE-013: Event sync failed, will retry")
                    return@withContext Result.retry()
                }
            }

            Log.d(TAG, "VE-013: Sync completed successfully")
            Result.success()

        } catch (e: Exception) {
            Log.e(TAG, "VE-013: Sync failed with exception", e)
            Result.retry()
        }
    }

    private suspend fun syncEvents(
        events: List<com.vigileye.companion.data.local.EventEntity>,
        eventDao: EventDao,
        prioritize: Boolean
    ): Boolean {
        return try {
            // TODO: Call actual API client
            // val apiClient = ApiClient.getInstance()
            // val response = apiClient.ingestEvents(events)

            // Mock success for now
            val syncedAt = System.currentTimeMillis()
            eventDao.markSynced(events.map { it.id }, syncedAt)

            true
        } catch (e: Exception) {
            Log.e(TAG, "VE-013: Failed to sync ${events.size} events", e)
            false
        }
    }
}
