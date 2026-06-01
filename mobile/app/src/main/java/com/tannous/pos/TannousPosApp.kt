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
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.tannous.pos.core.data.repository.AuthState
import com.tannous.pos.core.ui.AuthViewModel
import com.tannous.pos.feature.auth.LoginScreen
import com.tannous.pos.feature.customers.CustomersScreen
import com.tannous.pos.feature.printing.PrintingPreviewScreen
import com.tannous.pos.core.data.repository.toOrderDto
import com.tannous.pos.feature.sell.OrderHistoryScreen
import com.tannous.pos.feature.sell.OrderReceiptViewModel
import com.tannous.pos.feature.sell.ReceiptScreen
import com.tannous.pos.feature.sell.SellScreen
import com.tannous.pos.feature.sell.SellViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.feature.inventory.InventoryScreen
import com.tannous.pos.feature.reports.ReportsScreen
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
                val sellBackStackEntry = navController.currentBackStackEntry!!
                val sellViewModel: SellViewModel = hiltViewModel(sellBackStackEntry)
                SellScreen(
                    viewModel = sellViewModel,
                    onNavigateToShifts = { navController.navigate("shifts") },
                    onNavigateToCustomers = { navController.navigate("customers") },
                    onNavigateToSettings = { navController.navigate("settings") },
                    onNavigateToOrderHistory = { navController.navigate("order-history") }
                )
            }

            composable("order-history") {
                OrderHistoryScreen(
                    onNavigateBack = { navController.popBackStack() },
                    onNavigateToReceipt = { orderId ->
                        navController.navigate("receipt/$orderId")
                    }
                )
            }

            composable(
                route = "receipt/{orderId}",
                arguments = listOf(navArgument("orderId") { type = NavType.StringType })
            ) { backStackEntry ->
                val orderId = backStackEntry.arguments?.getString("orderId") ?: return@composable
                val receiptViewModel: OrderReceiptViewModel = hiltViewModel()
                val orderEntity by receiptViewModel.order.collectAsStateWithLifecycle()

                LaunchedEffect(orderId) {
                    receiptViewModel.loadOrder(orderId)
                }

                orderEntity?.let { entity ->
                    ReceiptScreen(
                        order = entity.toOrderDto(),
                        onDone = { navController.popBackStack() }
                    )
                }
            }
            
            composable("shifts") {
                ShiftsScreen(
                    onNavigateBack = { navController.popBackStack() }
                )
            }
            
            composable("customers") {
                val sellBackStackEntry = navController.getBackStackEntry("sell")
                val sellViewModel: SellViewModel = hiltViewModel(sellBackStackEntry)
                CustomersScreen(
                    onNavigateBack = { navController.popBackStack() },
                    onCustomerSelected = { customer ->
                        sellViewModel.attachCustomer(customer)
                        navController.popBackStack()
                    }
                )
            }
            
            composable("settings") {
                SettingsScreen(
                    onNavigateBack = { navController.popBackStack() },
                    onNavigateToPrintingPreview = { navController.navigate("printing-preview") },
                    onNavigateToReports = { navController.navigate("reports") },
                    onNavigateToOrderHistory = { navController.navigate("order-history") },
                    onNavigateToInventory = { navController.navigate("inventory") }
                )
            }

            composable("inventory") {
                InventoryScreen(
                    onNavigateBack = { navController.popBackStack() }
                )
            }

            composable("reports") {
                ReportsScreen(
                    onNavigateBack = { navController.popBackStack() }
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
