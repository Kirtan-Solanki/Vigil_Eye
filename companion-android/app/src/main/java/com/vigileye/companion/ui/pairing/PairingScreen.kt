package com.vigileye.companion.ui.pairing

import android.Manifest
import android.content.Intent
import android.graphics.Bitmap
import android.net.Uri
import android.os.Bundle
import android.util.Log
import android.widget.Toast
import androidx.activity.result.ActivityResultCallback
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import com.google.zxing.BinaryBitmap
import com.google.zxing.RGBLuminanceSource
import com.google.zxing.common.HybridBinarizer
import com.google.zxing.integration.android.IntentIntegrator
import com.google.zxing.integration.android.IntentResult
import com.vigileye.companion.R
import com.vigileye.companion.databinding.ActivityPairingBinding

/**
 * VE-012: QR Pairing Ceremony UI
 *
 * Handles the pairing flow with the parent dashboard:
 * 1. Requests camera permission
 * 2. Launches QR scanner (ZXing)
 * 3. Parses QR payload from dashboard
 * 4. Falls back to manual code entry
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
class PairingActivity : AppCompatActivity() {

    private lateinit var binding: ActivityPairingBinding

    private val cameraPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted: Boolean ->
        if (isGranted) {
            openQrScanner()
        } else {
            showManualFallback()
        }
    }

    private val scanQrLauncher = registerForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { result ->
        val intentResult: IntentResult? = IntentIntegrator.parseActivityResult(
            result.resultCode, result.data
        )
        if (intentResult != null) {
            if (intentResult.contents != null) {
                handleQrResult(intentResult.contents)
            } else {
                // User cancelled scan
                Log.d(TAG, "QR scan cancelled by user")
                showManualFallback()
            }
        } else {
            Log.e(TAG, "Failed to parse QR result")
            showManualFallback()
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityPairingBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupUI()
        checkCameraPermission()
    }

    private fun setupUI() {
        binding.tvPairingTitle.text = getString(R.string.pairing_title)
        binding.tvPairingSubtitle.text = getString(R.string.pairing_subtitle)
        binding.tvPairingInstructions.text = getString(R.string.pairing_instructions)

        binding.btnScanQr.setOnClickListener {
            checkCameraPermission()
        }

        binding.btnEnterCode.setOnClickListener {
            showManualFallback()
        }

        binding.btnConfirmPairing.setOnClickListener {
            val code = binding.etPairingCode.text.toString().trim()
            if (code.isNotEmpty()) {
                handleManualCode(code)
            } else {
                Toast.makeText(this, R.string.error_empty_code, Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun checkCameraPermission() {
        if (android.Manifest.permission.CAMERA in arrayOf(
            android.Manifest.permission.CAMERA
        )) {
            // Check if permission granted
            if (checkSelfPermission(Manifest.permission.CAMERA) ==
                android.content.pm.PackageManager.PERMISSION_GRANTED) {
                openQrScanner()
            } else {
                cameraPermissionLauncher.launch(Manifest.permission.CAMERA)
            }
        }
    }

    private fun openQrScanner() {
        val integrator = IntentIntegrator(this)
        integrator.setDesiredBarcodeFormats(
            arrayOf(IntentIntegrator.QR_CODE)
        )
        integrator.setPrompt(getString(R.string.qr_scan_prompt))
        integrator.setCameraId(0)  // Back camera
        integrator.setBeepEnabled(true)
        integrator.setBarcodeImageEnabled(false)
        integrator.setOrientationLocked(true)
        integrator.initiateScan()
    }

    private fun handleQrResult(qrData: String) {
        Log.i(TAG, "QR scanned: $qrData")

        // Parse the QR payload using the PairingViewModel
        val viewModel = PairingViewModel()
        val session = viewModel.parseQrPayload(qrData)

        if (session != null) {
            Log.i(TAG, "QR payload parsed successfully: session=${session.sessionId}")
            binding.tvPairingStatus.text = getString(R.string.pairing_qr_success)
            binding.tvPairingStatus.setTextColor(
                resources.getColor(R.color.success_green, null)
            )

            // Show pairing confirmation screen
            binding.layoutScan.visibility = android.view.View.GONE
            binding.layoutConfirm.visibility = android.view.View.VISIBLE
            binding.tvPairingCode.text = session.code
        } else {
            Log.e(TAG, "Failed to parse QR payload: $qrData")
            binding.tvPairingStatus.text = getString(R.string.pairing_qr_invalid)
            binding.tvPairingStatus.setTextColor(
                resources.getColor(R.color.error_red, null)
            )
            showManualFallback()
        }
    }

    private fun handleManualCode(code: String) {
        Log.i(TAG, "Manual pairing code entered: $code")

        // Create a session from manual entry (simplified)
        val session = PairingViewModel.PairingSession(
            sessionId = "manual_${System.currentTimeMillis()}",
            code = code,
            accountId = "pending",
            childId = "pending",
            expiresAt = ""
        )

        // Proceed to claim
        completePairing(session)
    }

    private fun showManualFallback() {
        binding.layoutScan.visibility = android.view.View.GONE
        binding.layoutManual.visibility = android.view.View.VISIBLE
    }

    private fun completePairing(session: PairingViewModel.PairingSession) {
        // Generate Ed25519 keypair (as done in PairingViewModel)
        val viewModel = PairingViewModel()

        // In production, this calls the backend API
        // For Phase 1, we log and navigate to success
        viewModel.claimPairingSession(
            session = session,
            platform = "android",
            osVersion = android.os.Build.VERSION.RELEASE,
            appVersion = packageInfo?.versionName ?: "unknown",
            deviceModel = android.os.Build.MODEL
        )

        // Show success and navigate to monitoring indicator
        Toast.makeText(this, R.string.pairing_success, Toast.LENGTH_LONG).show()
        // Navigate to main activity
        finish()
    }

    companion object {
        private const val TAG = "PairingActivity"
    }
}

// Helper function for PairingScreen (used in pairing flow)
fun parseQrPayload(qrData: String): PairingViewModel.PairingSession? {
    return try {
        val base64Data = qrData.trim()
        val decodedBytes = android.util.Base64.decode(base64Data, android.util.Base64.DEFAULT)
        val jsonString = String(decodedBytes)
        org.json.JSONObject(jsonString).run {
            PairingViewModel.PairingSession(
                sessionId = getString("session_id"),
                code = getString("code"),
                accountId = getString("account_id"),
                childId = getString("child_id"),
                expiresAt = getString("expires_at")
            )
        }
    } catch (e: Exception) {
        Log.e("PairingScreen", "Failed to parse QR payload: ${e.message}")
        null
    }
}