package com.vigileye.companion.collector

import android.content.ContentResolver
import android.content.Context
import android.database.Cursor
import android.net.Uri
import android.provider.CallLog
import android.util.Log
import com.vigileye.companion.data.local.EventDatabase
import com.vigileye.companion.data.local.EventEntity
import com.vigileye.companion.util.DateTimeUtils
import java.util.*

/**
 * VE-011: Call Log Collector
 *
 * Collects call log metadata (no content) for transmission to parent dashboard.
 *
 * Collects:
 * - Phone number (normalized)
 * - Call type (incoming/outgoing/missed/rejected/blocked)
 * - Duration (in seconds)
 * - Timestamp
 *
 * Does NOT collect:
 * - Call recording/audio
 * - Voicemail transcripts
 * - Any call content
 *
 * Data is encrypted locally via Room + SQLCipher before sync.
 */
class CallLogCollector(private val context: Context) {

    companion object {
        private const val TAG = "CallLogCollector"
    }

    /**
     * Collect new call log entries since last sync.
     *
     * @param lastSync Timestamp of last successful sync (milliseconds since epoch)
     * @return List of new call events to be stored locally
     */
    fun collectNewCalls(lastSync: Long): List<EventEntity> {
        val newCalls = mutableListOf<EventEntity>()
        val contentResolver: ContentResolver = context.contentResolver

        // Call log URI
        val callsUri: Uri = CallLog.Calls.CONTENT_URI

        // Projection - only what we need (metadata only)
        val projection = arrayOf(
            CallLog.Calls._ID,           // Call log row ID
            CallLog.Calls.NUMBER,        // Phone number
            CallLog.Calls.TYPE,          // Call type
            CallLog.Calls.DURATION,      // Call duration in seconds
            CallLog.Calls.DATE,          // Timestamp
            CallLog.Calls.CACHED_NAME,   // Contact name (if available)
            CallLog.Calls.CACHED_NUMBER_LABEL, // Label (mobile, home, work)
            CallLog.Calls.FEATURES       // Features (video, wifi, etc.)
        )

        // Selection - only get calls newer than last sync
        val selection = "${CallLog.Calls.DATE} > ?"
        val selectionArgs = arrayOf(lastSync.toString())

        // Sort by date descending (newest first)
        val sortOrder = "${CallLog.Calls.DATE} DESC"

        contentResolver.query(callsUri, projection, selection, selectionArgs, sortOrder)
            .use { cursor ->
                if (cursor != null && cursor.moveToFirst()) {
                    val idIndex = cursor.getColumnIndex(CallLog.Calls._ID)
                    val numberIndex = cursor.getColumnIndex(CallLog.Calls.NUMBER)
                    val typeIndex = cursor.getColumnIndex(CallLog.Calls.TYPE)
                    val durationIndex = cursor.getColumnIndex(CallLog.Calls.DURATION)
                    val dateIndex = cursor.getColumnIndex(CallLog.Calls.DATE)
                    val nameIndex = cursor.getColumnIndex(CallLog.Calls.CACHED_NAME)
                    val labelIndex = cursor.getColumnIndex(CallLog.Calls.CACHED_NUMBER_LABEL)
                    val featuresIndex = cursor.getColumnIndex(CallLog.Calls.FEATURES)

                    do {
                        val callId = cursor.getLong(idIndex)
                        val number = cursor.getString(numberIndex) ?: ""
                        val type = cursor.getInt(typeIndex)
                        val duration = cursor.getInt(durationIndex)
                        val timestamp = cursor.getLong(dateIndex)
                        val contactName = cursor.getString(nameIndex)
                        val numberLabel = cursor.getString(labelIndex)
                        val features = cursor.getInt(featuresIndex)

                        // Skip if number is empty or invalid
                        if (number.isBlank()) continue

                        // Normalize phone number (remove non-digits except leading +)
                        val normalizedNumber = normalizePhoneNumber(number)

                        // Determine call type string
                        val callType = when (type) {
                            CallLog.Calls.INCOMING -> "incoming"
                            CallLog.Calls.OUTGOING -> "outgoing"
                            CallLog.Calls.MISSED -> "missed"
                            CallLog.Calls.VOICEMAIL -> "voicemail"
                            CallLog.Calls.REJECTED -> "rejected"
                            CallLog.Calls.BLOCKED -> "blocked"
                            else -> "unknown"
                        }

                        // Build description
                        val description = buildCallDescription(
                            normalizedNumber,
                            callType,
                            contactName,
                            numberLabel,
                            features
                        )

                        // Create event entity
                        val event = EventEntity(
                            id = "call_$callId",
                            timestamp = timestamp,
                            type = "call",
                            title = "$callType call",
                            description = description,
                            childId = "", // Will be set by caller
                            metadata = mapOf(
                                "phoneNumber" to normalizedNumber,
                                "callType" to callType,
                                "duration" to duration,
                                "contactName" to contactName,
                                "numberLabel" to numberLabel,
                                "features" to features
                            )
                        )

                        newCalls.add(event)
                    } while (cursor.moveToNext())
                }
            }

        Log.i(TAG, "Collected ${newCalls.size} new call log entries")
        return newCalls
    }

    /**
     * Normalize phone number for consistent storage and comparison.
     */
    private fun normalizePhoneNumber(number: String): String {
        // Remove all non-digit characters except leading +
        var normalized = number.replace(Regex("[^0-9+]"), "")
        // Ensure it starts with + if it's an international number
        if (!normalized.startsWith("+") && normalized.length > 10) {
            normalized = "+$normalized"
        }
        return normalized
    }

    /**
     * Build human-readable description for the call.
     */
    private fun buildCallDescription(
        number: String,
        callType: String,
        contactName: String?,
        numberLabel: String?,
        features: Int
    ): String {
        val parts = mutableListOf<String>()

        // Add contact info if available
        if (contactName != null && contactName.isNotBlank()) {
            parts.add(contactName)
        } else if (numberLabel != null && numberLabel.isNotBlank()) {
            parts.add(numberLabel)
        } else {
            parts.add(number)
        }

        // Add call type
        parts.add("($callType call)")

        // Add features if notable
        val featureDescriptions = mutableListOf<String>()
        if (features and CallLog.Calls.FEATURES_VIDEO != 0) {
            featureDescriptions.add("video")
        }
        if (features and CallLog.Calls.FEATURES_WIFI != 0) {
            featureDescriptions.add("wifi")
        }
        if (feedures and CallLog.Calls.FEATURES_SIP != 0) {
            featureDescriptions.add("SIP")
        }
        if (featureDescriptions.isNotEmpty()) {
            parts.add("(${featureDescriptions.joinToString(", ")})")
        }

        return parts.joinWithSpace(" ")
    }

    /**
     * Get the timestamp of the most recent call log entry.
     * Used to track sync position.
     */
    fun getLatestCallTimestamp(): Long {
        val contentResolver: ContentResolver = context.contentResolver
        val callsUri: Uri = CallLog.Calls.CONTENT_URI
        val projection = arrayOf(CallLog.Calls.DATE)
        val sortOrder = "${CallLog.Calls.DATE} DESC"
        val limit = "1"

        return contentResolver.query(
            callsUri,
            projection,
            null,
            null,
            "$sortOrder LIMIT $limit"
        ).use { cursor ->
            if (cursor != null && cursor.moveToFirst()) {
                cursor.getLong(cursor.getColumnIndexOrThrow(CallLog.Calls.DATE))
            } else {
                0L // No calls found
            }
        }
    }
}