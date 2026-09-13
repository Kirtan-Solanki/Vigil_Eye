package com.vigileye.companion.data.local

import android.content.Context
import androidx.room.*
import net.zetetic.database.sqlcipher.SupportOpenHelperFactory

/**
 * VE-013: Local encrypted queue using Room + SQLCipher.
 *
 * All event data is encrypted at rest using SQLCipher with a key
 * derived from the device's hardware-backed keystore.
 *
 * This ensures that even if the device is compromised, the child's
 * activity data cannot be accessed without the device unlock.
 */
@Database(
    entities = [
        EventEntity::class,
        PendingSyncEntity::class
    ],
    version = 1,
    exportSchema = false
)
abstract class EventDatabase : RoomDatabase() {
    abstract fun eventDao(): EventDao
    abstract fun pendingSyncDao(): PendingSyncDao

    companion object {
        @Volatile
        private var INSTANCE: EventDatabase? = null

        /**
         * Gets the encrypted database instance.
         *
         * The encryption key is derived from Android Keystore and never
         * stored in plaintext. Each installation generates a unique key.
         */
        fun getInstance(context: Context, encryptionKey: ByteArray): EventDatabase {
            return INSTANCE ?: synchronized(this) {
                val passphrase = net.zetetic.database.sqlcipher.database.SQLiteDatabase.getBytes(encryptionKey)

                val builder = Room.databaseBuilder(
                    context.applicationContext,
                    EventDatabase::class.java,
                    "vigileye_events_encrypted.db"
                )
                    .openHelperFactory(SupportOpenHelperFactory(passphrase))
                    .fallbackToDestructiveMigration()

                INSTANCE = builder.build()
                INSTANCE!!
            }
        }
    }
}

/**
 * VE-003: Event entity with metadata-only fields for SMS.
 * No 'content' or 'body' field exists in v1 schema.
 */
@Entity(tableName = "events")
data class EventEntity(
    @PrimaryKey(autoGenerate = true)
    val id: Long = 0,

    @ColumnInfo(name = "event_type")
    val eventType: String,  // "call", "sms_meta", "location", "app_usage"

    @ColumnInfo(name = "payload")
    val payload: String,  // JSON serialized event data

    @ColumnInfo(name = "occurred_at")
    val occurredAt: Long,  // Unix timestamp in milliseconds

    @ColumnInfo(name = "synced")
    val synced: Boolean = false,  // Has been sent to backend

    @ColumnInfo(name = "synced_at")
    val syncedAt: Long? = null,  // When sync completed

    @ColumnInfo(name = "retry_count")
    val retryCount: Int = 0,  // For exponential backoff

    @ColumnInfo(name = "created_at")
    val createdAt: Long = System.currentTimeMillis()
)

@Dao
interface EventDao {
    @Query("SELECT * FROM events WHERE synced = 0 ORDER BY occurredAt ASC")
    suspend fun getUnsyncedEvents(): List<EventEntity>

    @Query("SELECT * FROM events WHERE synced = 0 AND eventType = :eventType ORDER BY occurredAt ASC LIMIT :limit")
    suspend fun getUnsyncedEventsByType(eventType: String, limit: Int): List<EventEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(event: EventEntity): Long

    @Update
    suspend fun update(event: EventEntity)

    @Query("UPDATE events SET synced = 1, syncedAt = :syncedAt WHERE id IN (:ids)")
    suspend fun markSynced(ids: List<Long>, syncedAt: Long)

    @Query("DELETE FROM events WHERE synced = 1 AND syncedAt < :olderThan")
    suspend fun deleteOldSyncedEvents(olderThan: Long)

    @Query("SELECT COUNT(*) FROM events WHERE synced = 0")
    suspend fun getUnsyncedCount(): Int
}

/**
 * VE-013: Tracks pending sync batches for retry logic.
 */
@Entity(tableName = "pending_sync")
data class PendingSyncEntity(
    @PrimaryKey(autoGenerate = true)
    val id: Long = 0,

    @ColumnInfo(name = "batch_id")
    val batchId: String,  // UUID for this sync attempt

    @ColumnInfo(name = "event_ids")
    val eventIds: String,  // JSON array of event IDs

    @ColumnInfo(name = "attempt_count")
    val attemptCount: Int = 0,

    @ColumnInfo(name = "last_attempt_at")
    val lastAttemptAt: Long? = null,

    @ColumnInfo(name = "next_retry_at")
    val nextRetryAt: Long? = null,  // For exponential backoff

    @ColumnInfo(name = "status")
    val status: String = "pending"  // "pending", "in_progress", "failed"
)

@Dao
interface PendingSyncDao {
    @Query("SELECT * FROM pending_sync WHERE status = 'pending' AND (next_retryAt IS NULL OR nextRetryAt <= :now) ORDER BY id ASC")
    suspend fun getPendingBatches(now: Long): List<PendingSyncEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(batch: PendingSyncEntity)

    @Update
    suspend fun update(batch: PendingSyncEntity)

    @Query("DELETE FROM pending_sync WHERE status = 'failed' AND id < :beforeId")
    suspend fun deleteFailedBatches(beforeId: Long)
}
