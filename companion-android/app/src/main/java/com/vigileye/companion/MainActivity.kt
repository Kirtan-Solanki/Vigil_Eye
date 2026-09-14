package com.vigileye.companion

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.vigileye.companion.databinding.ActivityMainBinding

/**
 * VE-009: Main entry point. No stop/disable path for VE-010 indicator.
 */
class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
    }
}
