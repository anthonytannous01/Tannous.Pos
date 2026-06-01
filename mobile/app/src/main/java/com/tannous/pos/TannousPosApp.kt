package com.tannous.pos

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.tannous.pos.core.data.repository.AuthState
import com.tannous.pos.core.ui.AuthViewModel
import com.tannous.pos.feature.auth.LoginScreen
import com.tannous.pos.feature.printing.PrintingPreviewScreen
import com.tannous.pos.feature.sell.SellScreen
import com.tannous.pos.feature.settings.SettingsScreen
import com.tannous.pos.feature.shifts.ShiftsScreen

@Composable
fun TannousPosApp(
    authViewModel: AuthViewModel = hiltViewModel()
) {
    val navController = rememberNavController()
    val authState by authViewModel.authState.collectAsState()
    
    // Navigate based on auth state
    LaunchedEffect(authState) {
        when (authState) {
            is AuthState.LoggedIn -> {
                if (navController.currentDestination?.route == "login") {
                    navController.navigate("sell") {
                        popUpTo("login") { inclusive = true }
                    }
                }
            }
            is AuthState.LoggedOut -> {
                if (navController.currentDestination?.route != "login") {
                    navController.navigate("login") {
                        popUpTo(0) { inclusive = true }
                    }
                }
            }
            is AuthState.Loading -> {
                // Show loading state or stay on current screen
            }
        }
    }
    
    Surface(
        modifier = Modifier.fillMaxSize(),
        color = MaterialTheme.colorScheme.background
    ) {
        NavHost(
            navController = navController,
            startDestination = when (authState) {
                is AuthState.LoggedIn -> "sell"
                else -> "login"
            }
        ) {
            composable("login") {
                LoginScreen(
                    onLoginSuccess = {
                        // Auth state will be updated by AuthRepository, which will trigger navigation
                        authViewModel.refreshAuthState()
                    }
                )
            }
            
            composable("sell") {
                SellScreen(
                    onNavigateToShifts = { navController.navigate("shifts") },
                    onNavigateToCustomers = { navController.navigate("customers") },
                    onNavigateToSettings = { navController.navigate("settings") }
                )
            }
            
            composable("shifts") {
                ShiftsScreen(
                    onNavigateBack = { navController.popBackStack() }
                )
            }
            
            composable("customers") {
                CustomersScreen(
                    onNavigateBack = { navController.popBackStack() }
                )
            }
            
            composable("settings") {
                SettingsScreen(
                    onNavigateBack = { navController.popBackStack() },
                    onNavigateToPrintingPreview = { navController.navigate("printing-preview") }
                )
            }
            
            composable("printing-preview") {
                PrintingPreviewScreen(
                    onNavigateBack = { navController.popBackStack() }
                )
            }
        }
    }
}
