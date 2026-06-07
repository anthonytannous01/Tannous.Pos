package com.tannous.pos

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
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
import com.tannous.pos.feature.sell.OrderReceiptState
import com.tannous.pos.feature.sell.OrderReceiptViewModel
import com.tannous.pos.feature.sell.ReceiptScreen
import com.tannous.pos.feature.sell.SellScreen
import com.tannous.pos.feature.sell.SellViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.feature.inventory.InventoryScreen
import com.tannous.pos.feature.reports.DashboardScreen
import com.tannous.pos.feature.reports.MenuEngineeringScreen
import com.tannous.pos.feature.reports.ReportsScreen
import com.tannous.pos.core.ui.LanguageViewModel
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.feature.settings.QrMenuScreen
import com.tannous.pos.feature.settings.ReservationsScreen
import com.tannous.pos.feature.settings.SettingsScreen
import com.tannous.pos.feature.sell.KdsScreen
import com.tannous.pos.feature.sell.LoyaltyScreen
import com.tannous.pos.feature.sell.TableMapScreen
import com.tannous.pos.feature.shifts.ShiftsScreen

@Composable
fun TannousPosApp(
    authViewModel: AuthViewModel = hiltViewModel(),
    languageViewModel: LanguageViewModel = hiltViewModel()
) {
    val navController = rememberNavController()
    val authState by authViewModel.authState.collectAsState()
    val isArabic  by languageViewModel.isArabic.collectAsState()
    
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
    
    CompositionLocalProvider(
        LocalIsArabic provides isArabic,
        androidx.compose.ui.platform.LocalLayoutDirection provides
            if (isArabic) LayoutDirection.Rtl else LayoutDirection.Ltr
    ) {
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
                val receiptState by receiptViewModel.state.collectAsStateWithLifecycle()

                LaunchedEffect(orderId) {
                    receiptViewModel.loadOrder(orderId)
                }

                when (val s = receiptState) {
                    is OrderReceiptState.Loading -> {
                        Box(
                            modifier = Modifier.fillMaxSize(),
                            contentAlignment = Alignment.Center
                        ) {
                            CircularProgressIndicator()
                        }
                    }
                    is OrderReceiptState.NotFound -> {
                        Box(
                            modifier = Modifier.fillMaxSize(),
                            contentAlignment = Alignment.Center
                        ) {
                            Column(
                                horizontalAlignment = Alignment.CenterHorizontally,
                                verticalArrangement = Arrangement.spacedBy(12.dp),
                                modifier = Modifier.padding(32.dp)
                            ) {
                                Text(
                                    text = "Receipt not available",
                                    style = MaterialTheme.typography.titleMedium
                                )
                                Text(
                                    text = "This order was not found on this device. " +
                                        "Sync to load recent orders.",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    textAlign = TextAlign.Center
                                )
                                TextButton(onClick = { navController.popBackStack() }) {
                                    Text("Go Back")
                                }
                            }
                        }
                    }
                    is OrderReceiptState.Found -> {
                        ReceiptScreen(
                            order = s.entity.toOrderDto(),
                            onDone = { navController.popBackStack() }
                        )
                    }
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
                    onNavigateToInventory = { navController.navigate("inventory") },
                    onNavigateToKds = { navController.navigate("kds") },
                    onNavigateToDashboard = { navController.navigate("dashboard") },
                    onNavigateToMenuEngineering = { navController.navigate("menu-engineering") },
                    onNavigateToTables = { navController.navigate("tables") },
                    onNavigateToQrMenu = { navController.navigate("qr-menu") },
                    onNavigateToReservations = { navController.navigate("reservations") }
                )
            }

            composable("qr-menu") {
                QrMenuScreen(onNavigateBack = { navController.popBackStack() })
            }

            composable("reservations") {
                ReservationsScreen(onNavigateBack = { navController.popBackStack() })
            }

            composable("dashboard") {
                DashboardScreen(onNavigateBack = { navController.popBackStack() })
            }

            composable("menu-engineering") {
                MenuEngineeringScreen(onNavigateBack = { navController.popBackStack() })
            }

            // Table map — view/manage mode
            composable("tables") {
                TableMapScreen(onNavigateBack = { navController.popBackStack() })
            }

            composable(
                route = "loyalty/{customerId}",
                arguments = listOf(navArgument("customerId") { type = NavType.StringType })
            ) { backStackEntry ->
                val customerId = backStackEntry.arguments?.getString("customerId") ?: return@composable
                LoyaltyScreen(
                    customerId = customerId,
                    onNavigateBack = { navController.popBackStack() }
                )
            }

            composable("kds") {
                KdsScreen(onNavigateBack = { navController.popBackStack() })
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
    } // CompositionLocalProvider
}
