package com.vigileye.companion.ui.onboarding

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import android.view.View
import android.widget.Toast
import androidx.activity.result.ActivityResultCallback
import androidx.activity.result.ActivityResultLauncher
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.vigileye.companion.R
import com.vigileye.companion.databinding.ActivitySetupBinding
import com.vigileye.companion.ui.pairing.PairingActivity
import com.vigileye.companion.util.PermissionUtils

/**
 * VE-009: Android Guided Setup Flow
 *
 * Step-by-step onboarding that explains what the app does and requests permissions
 * with clear, transparent explanations.
 *
 * Flow:
 * 1. Welcome screen with app purpose explanation
 * 2. Permission explanations with rationale
 * 3. Network connectivity check
 * 4. QR code scanner entry point
 */
class SetupActivity : AppCompatActivity() {

    private lateinit var binding: ActivitySetupBinding
    private var currentStep = 0
    private val totalSteps = 4

    // Permission launchers
    private val callLogPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted: Boolean ->
        if (isGranted) {
            proceedToNextStep()
        } else {
            showPermissionRationale(Manifest.permission.READ_CALL_LOG)
        }
    }

    private val locationPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted: Boolean ->
        if (isGranted) {
            proceedToNextStep()
        } else {
            showPermissionRationale(Manifest.permission.ACCESS_FINE_LOCATION)
        }
    }

    private val packageUsagePermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted: Boolean ->
        if (isGranted) {
            proceedToNextStep()
        } else {
            showUsageAccessRationale()
        }
    }

    private val cameraPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted: Boolean ->
        if (isGranted) {
            proceedToNextStep()
        } else {
            showPermissionRationale(Manifest.permission.CAMERA)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySetupBinding.inflate(layoutInflater)
        setContentView(binding.root)

        // Setup UI
        setupUI()
        showStep(0)
    }

    private fun setupUI() {
        binding.btnNext.setOnClickListener {
            when (currentStep) {
                0 -> checkNetworkConnectivity()
                1 -> requestCallLogPermission()
                2 -> requestLocationPermission()
                3 -> {
                    // Check if we have usage access (special permission)
                    if (PermissionUtils.hasUsageAccess(this)) {
                        proceedToNextStep()
                    } else {
                        requestUsageAccessPermission()
                    }
                }
                4 -> requestCameraPermission()
                5 -> startPairing()
            }
        }

        binding.btnSkip.setOnClickListener {
            // Allow skipping to main app (limited functionality)
            startPairing()
        }

        binding.btnBack.setOnClickListener {
            if (currentStep > 0) {
                showStep(currentStep - 1)
            }
        }
    }

    private fun setupUI() {
        binding.tvTitle.text = getString(R.string.setup_welcome_title)
        binding.tvDescription.text = getString(R.string.setup_welcome_description)
        binding.progressBar.max = totalSteps
        binding.progressBar.progress = 0
        binding.btnBack.visibility = View.GONE
        binding.btnSkip.visibility = View.VISIBLE
        binding.btnNext.text = getString(R.string.next)
    }

    private fun showStep(step: Int) {
        currentStep = step
        binding.progressBar.progress = step

        when (step) {
            0 -> {
                binding.tvTitle.text = getString(R.string.setup_welcome_title)
                binding.tvDescription.text = getString(R.string.setup_welcome_description)
                binding.btnBack.visibility = View.GONE
            }
            1 -> {
                binding.tvTitle.text = getString(R.string.setup_permissions_title)
                binding.tvDescription.text = getString(R.string.setup_calllog_description)
                binding.btnBack.visibility = View.VISIBLE
            }
            2 -> {
                binding.tvTitle.text = getString(R.string.setup_location_title)
                binding.tvDescription.text = getString(R.string.setup_location_description)
                binding.btnBack.visibility = View.VISIBLE
            }
            3 -> {
                binding.tvTitle.text = getString(R.string.setup_usage_title)
                binding.tvDescription.text = getString(R.string.setup_usage_description)
                binding.btnBack.visibility = View.VISIBLE
            }
            4 -> {
                binding.tvTitle.text = getString(R.string.setup_camera_title)
                binding.tvDescription.text = getString(R.string.setup_camera_description)
                binding.btnBack.visibility = View.VISIBLE
                binding.btnSkip.visibility = View.GONE
            }
            5 -> {
                binding.tvTitle.text = getString(R.string.setup_complete_title)
                binding.tvDescription.text = getString(R.string.setup_complete_description)
                binding.btnBack.visibility = View.GONE
                binding.btnNext.text = getString(R.string.get_started)
            }
        }
    }

    private fun checkNetworkConnectivity() {
        val connectivityManager = getSystemService(CONNECTIVITY_SERVICE) as ConnectivityManager
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            val network = connectivityManager.activeNetwork
            if (network != null) {
                val capabilities = connectivityManager.getNetworkCapabilities(network)
                if (capabilities != null &&
                    capabilities.hasCapability(NET_CAPABILITY_INTERNET)) {
                    proceedToNextStep()
                    return
                }
            }
        }
        Toast.makeText(this, R.string.error_no_internet, Toast.LENGTH_LONG).show()
    }

    private fun proceedToNextStep() {
        showStep(currentStep + 1)
    }

    private fun requestCallLogPermission() {
        if (ContextCompat.checkSelfPermission(
                this, Manifest.permission.READ_CALL_LOG) ==
            PackageManager.PERMISSION_GRANTED) {
            proceedToNextStep()
        } else {
            if (ActivityCompat.shouldShowRequestPermissionRationale(
                    this, Manifest.permission.READ_CALL_LOG)) {
                showPermissionRationale(Manifest.permission.READ_CALL_LOG)
            } else {
                callLogPermissionLauncher.launch(Manifest.permission.READ_CALL_LOG)
            }
        }
    }

    private fun requestLocationPermission() {
        if (ContextCompat.checkSelfPermission(
                this, Manifest.permission.ACCESS_FINE_LOCATION) ==
            PackageManager.PERMISSION_GRANTED) {
            proceedToNextStep()
        } else {
            if (ActivityCompat.shouldShowRequestPermissionRationale(
                    this, Manifest.permission.ACCESS_FINE_LOCATION)) {
                showPermissionRationale(Manifest.permission.ACCESS_FINE_LOCATION)
            } else {
                locationPermissionLauncher.launch(Manifest.permission.ACCESS_FINE_LOCATION)
            }
        }
    }

    private fun requestUsageAccessPermission() {
        if (PermissionUtils.hasUsageAccess(this)) {
            proceedToNextStep()
        } else {
            showUsageAccessRationale()
        }
    }

    private fun requestCameraPermission() {
        if (ContextCompat.checkSelfPermission(
                this, Manifest.permission.CAMERA) ==
            PackageManager.PERMISSION_GRANTED) {
            proceedToNextStep()
        } else {
            if (ActivityCompat.shouldShowRequestPermissionRationale(
                    this, Manifest.permission.CAMERA)) {
                showPermissionRationale(Manifest.permission.CAMERA)
            } else {
                cameraPermissionLauncher.launch(Manifest.permission.CAMERA)
            }
        }
    }

    private fun showPermissionRationale(permission: String) {
        // Show explanation dialog
        when (permission) {
            Manifest.permission.READ_CALL_LOG -> {
                PermissionUtils.showPermissionRationaleDialog(
                    this,
                    getString(R.string.permission_calllog_rationale_title),
                    getString(R.string.permission_calllog_rationale_message)
                ) { granted ->
                    if (granted) {
                        callLogPermissionLauncher.launch(permission)
                    }
                }
            }
            Manifest.permission.ACCESS_FINE_LOCATION -> {
                PermissionUtils.showPermissionRationaleDialog(
                    this,
                    getString(R.string.permission_location_rationale_title),
                    getString(R.string.permission_location_rationale_message)
                ) { granted ->
                    if (granted) {
                        locationPermissionLauncher.launch(permission)
                    }
                }
            }
            Manifest.permission.CAMERA -> {
                PermissionUtils.showPermissionRationaleDialog(
                    this,
                    getString(R.string.permission_camera_rationale_title),
                    getString(R.string.permission_camera_rationale_message)
                ) { granted ->
                    if (granted) {
                        cameraPermissionLauncher.launch(permission)
                    }
                }
            }
        }
    }

    private fun showUsageAccessRationale() {
        PermissionUtils.showPermissionRationaleDialog(
            this,
            getString(R.string.permission_usage_rationale_title),
            getString(R.string.permission_usage_rationale_message)
        ) { granted ->
            if (granted) {
                val intent = Settings.ACTION_USAGE_ACCESS_SETTINGS
                startActivity(intent)
            }
        }
    }

    private fun startPairing() {
        val intent = Intent(this, PairingActivity::class.java)
        startActivity(intent)
        finish()
    }
}