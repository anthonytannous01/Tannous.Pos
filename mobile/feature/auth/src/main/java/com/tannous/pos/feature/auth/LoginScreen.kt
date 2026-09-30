package com.tannous.pos.feature.auth

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.core.ui.ServerAddressSection

@Composable
fun LoginScreen(
    onLoginSuccess: () -> Unit,
    viewModel: LoginViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    val isArabic = LocalIsArabic.current

    LaunchedEffect(uiState.isLoggedIn) {
        if (uiState.isLoggedIn) {
            onLoginSuccess()
        }
    }
    
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            text = "Tannous POS",
            style = MaterialTheme.typography.headlineLarge
        )
        
        Spacer(modifier = Modifier.height(32.dp))
        
        OutlinedTextField(
            value = uiState.username,
            onValueChange = { viewModel.updateUsername(it) },
            label = { Text(if (isArabic) "اسم المستخدم" else "Username") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )
        
        Spacer(modifier = Modifier.height(16.dp))
        
        OutlinedTextField(
            value = uiState.password,
            onValueChange = { viewModel.updatePassword(it) },
            label = { Text(if (isArabic) "كلمة المرور" else "Password") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            visualTransformation = PasswordVisualTransformation()
        )
        
        Spacer(modifier = Modifier.height(24.dp))
        
        Button(
            onClick = { viewModel.login() },
            modifier = Modifier.fillMaxWidth(),
            enabled = !uiState.isLoading && uiState.username.isNotBlank() && uiState.password.isNotBlank()
        ) {
            if (uiState.isLoading) {
                CircularProgressIndicator(
                    modifier = Modifier.size(16.dp),
                    color = MaterialTheme.colorScheme.onPrimary
                )
            } else {
                Text(if (isArabic) "تسجيل الدخول" else "Login")
            }
        }
        
        uiState.error?.let { error ->
            Spacer(modifier = Modifier.height(16.dp))
            Text(
                text = error,
                color = MaterialTheme.colorScheme.error,
                style = MaterialTheme.typography.bodyMedium
            )
            // A login failure is often the server being unreachable rather than a wrong password,
            // and the address is the thing to check. Say so here rather than leaving someone to
            // retype their password at a till that cannot reach anything.
            Text(
                text = if (isArabic) {
                    "إذا لم يستجب الجهاز إطلاقاً، تحقّق من عنوان الخادم أدناه."
                } else {
                    "If nothing responds at all, check the server address below."
                },
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                style = MaterialTheme.typography.bodySmall
            )
        }

        Spacer(modifier = Modifier.height(24.dp))

        // Always reachable, including when the app cannot reach the server. Putting this behind
        // login would make the setting that fixes a broken connection require a working one.
        TextButton(onClick = { viewModel.showServerSettings() }) {
            Text(if (isArabic) "عنوان الخادم" else "Server address")
        }

        Text(
            text = uiState.serverAddress,
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }

    if (uiState.showServerSettings) {
        AlertDialog(
            onDismissRequest = { viewModel.dismissServerSettings() },
            confirmButton = {
                TextButton(onClick = { viewModel.dismissServerSettings() }) {
                    Text(if (isArabic) "إغلاق" else "Close")
                }
            },
            text = {
                ServerAddressSection(
                    isArabic = isArabic,
                    address = uiState.serverAddress,
                    isOverridden = uiState.serverAddressIsOverridden,
                    defaultAddress = uiState.serverAddressDefault,
                    error = uiState.serverAddressError,
                    onAddressChange = viewModel::setServerAddressInput,
                    onSave = viewModel::saveServerAddress,
                    onReset = viewModel::resetServerAddress
                )
            }
        )
    }
}
