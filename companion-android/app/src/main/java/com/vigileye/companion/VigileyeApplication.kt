package com.vigileye.companion

import android.app.Application

/**
 * Application entry point.
 * Per VE-010 / invariant #1: there is NO code path to disable the monitoring indicator.
 */
class VigileyeApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        // Phase 1 scaffold — WorkManager / Room init deferred to pairing flow
    }
}
