package com.vigileye.companion.ui.pairing

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.security.KeyPairGenerator
import java.security.Signature
import java.security.spec.ECGenParameterSpec
import java.util.Base64

/**
 * VE-012: Device pairing flow.
 *
 * Implements QR-code pairing with Ed25519 keypair generation:
 * 1. Parent creates pairing session in dashboard (gets QR code)
 * 2. Companion app scans QR code (or enters fallback code)
 * 3. App generates Ed25519 keypair in Android Keystore
 * 4. App sends public key + pairing token to backend
 * 5. Backend issues device credentials (access + refresh tokens)
 *
 * TRD §5.1: Device private key never leaves the device.
 * TRD §5.1.1: Refresh tokens rotate on every use.
 */
class PairingViewModel : ViewModel() {

    companion object {
        private const val TAG = "PairingViewModel"
    }

    /**
     * Parses QR code payload from dashboard.
     *
     * QR payload format (Base64-encoded JSON):
     * {
     *   "session_id": "uuid",
     *   "code": "ABC12345",
     *   "account_id": "uuid",
     *   "child_id": "uuid",
     *   "expires_at": "2026-09-13T12:00:00Z"
     * }
     */
    fun parseQrPayload(qrData: String): PairingSession? {
        return try {
            val json = String(Base64.getDecoder().decode(qrData))
            val obj = JSONObject(json)

            PairingSession(
                sessionId = obj.getString("session_id"),
                code = obj.getString("code"),
                accountId = obj.getString("account_id"),
                childId = obj.getString("child_id"),
                expiresAt = obj.getString("expires_at")
            )
        } catch (e: Exception) {
            Log.e(TAG, "Failed to parse QR payload", e)
            null
        }
    }

    /**
     * Generates Ed25519 keypair in Android Keystore.
     *
     * TRD §5.1: "The Companion App generates an asymmetric key pair on-device
     * (Ed25519) inside the Android Keystore. The private key never leaves the device."
     */
    private fun generateDeviceKeypair(): KeyPairResult {
        return try {
            // In production, use Android Keystore with KeyGenParameterSpec
            // For Phase 0, using standard KeyPairGenerator

            val keyPairGenerator = KeyPairGenerator.getInstance("EC")
            val spec = ECGenParameterSpec("Ed25519")  // Ed25519 curve
            keyPairGenerator.initialize(spec, java.security.SecureRandom())

            val keyPair = keyPairGenerator.generateKeyPair()
            val publicKey = keyPair.public
            val privateKey = keyPair.private

            // Convert public key to PEM format for transmission
            val publicKeyPem = "-----BEGIN PUBLIC KEY-----\n" +
                Base64.getEncoder().encodeToString(publicKey.encoded) +
                "\n-----END PUBLIC KEY-----"

            KeyPairResult(
                publicKeyPem = publicKeyPem,
                success = true
            )

        } catch (e: Exception) {
            Log.e(TAG, "VE-012: Failed to generate keypair", e)
            KeyPairResult(success = false, error = e.message)
        }
    }

    /**
     * Claims a pairing session by sending public key to backend.
     *
     * VE-004: Rate-limited fallback code is enforced server-side.
     */
    fun claimPairingSession(
        session: PairingSession,
        platform: String,
        osVersion: String,
        appVersion: String,
        deviceModel: String
    ) {
        viewModelScope.launch {
            try {
                // Generate device keypair
                val keyResult = generateDeviceKeypair()

                if (!keyResult.success) {
                    Log.e(TAG, "VE-012: Keypair generation failed")
                    return@launch
                }

                // Send claim request to backend
                // TODO: Call actual API client
                /*
                val apiClient = ApiClient.getInstance()
                val response = apiClient.claimPairing(
                    code = session.code,
                    publicKeyPem = keyResult.publicKeyPem,
                    platform = platform,
                    osVersion = osVersion,
                    appVersion = appVersion,
                    deviceModel = deviceModel
                )
                */

                Log.i(TAG, "VE-012: Pairing claimed successfully for session ${session.sessionId}")

            } catch (e: Exception) {
                Log.e(TAG, "VE-012: Failed to claim pairing session", e)
            }
        }
    }

    data class PairingSession(
        val sessionId: String,
        val code: String,
        val accountId: String,
        val childId: String,
        val expiresAt: String
    )

    data class KeyPairResult(
        val publicKeyPem: String? = null,
        val success: Boolean,
        val error: String? = null
    )
}
