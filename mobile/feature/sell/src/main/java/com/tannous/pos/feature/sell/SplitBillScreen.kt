package com.tannous.pos.feature.sell

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Remove
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.core.util.currencyFormatterFor
import java.math.BigDecimal

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SplitBillScreen(
    orderId: String,
    onComplete: (com.tannous.pos.core.data.model.OrderDto) -> Unit,
    viewModel: SplitBillViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }
    val currencyFormatter = currencyFormatterFor("USD")

    LaunchedEffect(orderId) {
        viewModel.initialize(orderId)
    }

    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it)
            viewModel.clearError()
        }
    }

    LaunchedEffect(uiState.isComplete) {
        uiState.finalizedOrder?.let { onComplete(it) }
    }

    BackHandler(enabled = true) { /* disabled during split flow */ }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(if (isArabic) "تقسيم الفاتورة" else "Split Bill")
                },
                navigationIcon = {
                    if (uiState.step == SplitBillStep.ChooseSplit) {
                        IconButton(onClick = { /* back disabled */ }, enabled = false) {
                            Icon(Icons.Default.ArrowBack, contentDescription = null)
                        }
                    }
                }
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) }
    ) { padding ->
        when (uiState.step) {
            SplitBillStep.ChooseSplit -> SplitChooseStep(
                modifier = Modifier.padding(padding),
                uiState = uiState,
                isArabic = isArabic,
                currencyFormatter = currencyFormatter,
                onIncrement = viewModel::incrementWays,
                onDecrement = viewModel::decrementWays,
                onContinue = viewModel::continueToCollect
            )
            SplitBillStep.CollectPayment -> SplitCollectStep(
                modifier = Modifier.padding(padding),
                uiState = uiState,
                isArabic = isArabic,
                currencyFormatter = currencyFormatter,
                viewModel = viewModel
            )
        }
    }
}

@Composable
private fun SplitChooseStep(
    modifier: Modifier,
    uiState: SplitBillState,
    isArabic: Boolean,
    currencyFormatter: java.text.NumberFormat,
    onIncrement: () -> Unit,
    onDecrement: () -> Unit,
    onContinue: () -> Unit
) {
    Column(
        modifier = modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        uiState.splitData?.let { split ->
            Text(
                text = if (isArabic) {
                    "إجمالي الطلب: ${currencyFormatter.format(split.orderTotal)}"
                } else {
                    "Order Total: ${currencyFormatter.format(split.orderTotal)}"
                },
                style = MaterialTheme.typography.titleMedium
            )
        }

        Text(
            text = if (isArabic) "كم عدد الأشخاص؟" else "How many ways?",
            style = MaterialTheme.typography.titleSmall
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.Center,
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onDecrement, enabled = uiState.selectedWays > 2) {
                Icon(Icons.Default.Remove, contentDescription = "Decrease")
            }
            Text(
                text = uiState.selectedWays.toString(),
                style = MaterialTheme.typography.headlineMedium,
                modifier = Modifier.padding(horizontal = 24.dp)
            )
            IconButton(onClick = onIncrement, enabled = uiState.selectedWays < 20) {
                Icon(Icons.Default.Add, contentDescription = "Increase")
            }
        }

        uiState.splitData?.let { split ->
            Text(
                text = if (isArabic) {
                    "كل شخص يدفع: ${currencyFormatter.format(split.amountPerPerson)}"
                } else {
                    "Each person pays: ${currencyFormatter.format(split.amountPerPerson)}"
                },
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.SemiBold
            )
        }

        if (uiState.isLoading) {
            CircularProgressIndicator(modifier = Modifier.align(Alignment.CenterHorizontally))
        }

        Button(
            onClick = onContinue,
            modifier = Modifier.fillMaxWidth(),
            enabled = uiState.splitData != null && !uiState.isLoading
        ) {
            Text(if (isArabic) "متابعة" else "Continue")
        }
    }
}

@Composable
private fun SplitCollectStep(
    modifier: Modifier,
    uiState: SplitBillState,
    isArabic: Boolean,
    currencyFormatter: java.text.NumberFormat,
    viewModel: SplitBillViewModel
) {
    val split = uiState.splitData
    val amountDue = if (uiState.useCustomAmount) {
        uiState.customAmount.toBigDecimalOrNull() ?: BigDecimal.ZERO
    } else {
        split?.amountPerPerson ?: BigDecimal.ZERO
    }

    Column(
        modifier = modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text(
            text = if (isArabic) {
                "شخص ${uiState.currentPerson} من ${uiState.selectedWays}"
            } else {
                "Person ${uiState.currentPerson} of ${uiState.selectedWays}"
            },
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
        )

        Text(
            text = if (isArabic) {
                "المبلغ المستحق: ${currencyFormatter.format(amountDue)}"
            } else {
                "Amount due: ${currencyFormatter.format(amountDue)}"
            },
            style = MaterialTheme.typography.titleSmall
        )

        Text(
            text = if (isArabic) "طريقة الدفع" else "Payment method",
            style = MaterialTheme.typography.labelLarge
        )

        SplitPaymentMethod.entries.forEach { method ->
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                RadioButton(
                    selected = uiState.selectedMethod == method,
                    onClick = { viewModel.setPaymentMethod(method) }
                )
                Text(
                    text = when (method) {
                        SplitPaymentMethod.Cash -> if (isArabic) "نقداً" else "Cash"
                        SplitPaymentMethod.Card -> if (isArabic) "بطاقة" else "Card"
                        SplitPaymentMethod.LbpCash -> if (isArabic) "نقد ل.ل" else "LBP Cash"
                    }
                )
            }
        }

        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(
                checked = uiState.useCustomAmount,
                onCheckedChange = viewModel::setCustomAmountEnabled
            )
            Text(if (isArabic) "مبلغ مخصص" else "Custom amount")
        }

        if (uiState.useCustomAmount) {
            OutlinedTextField(
                value = uiState.customAmount,
                onValueChange = viewModel::setCustomAmount,
                label = { Text(if (isArabic) "المبلغ" else "Amount") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )
        }

        OutlinedTextField(
            value = uiState.tenderedAmount,
            onValueChange = viewModel::setTenderedAmount,
            label = { Text(if (isArabic) "المبلغ المدفوع" else "Tendered amount") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            enabled = !uiState.useCustomAmount || uiState.selectedMethod == SplitPaymentMethod.Cash
        )

        if (uiState.selectedMethod == SplitPaymentMethod.Cash) {
            Text(
                text = if (isArabic) {
                    "الباقي: ${currencyFormatter.format(viewModel.changeDue(uiState))}"
                } else {
                    "Change: ${currencyFormatter.format(viewModel.changeDue(uiState))}"
                },
                style = MaterialTheme.typography.bodyMedium
            )
        }

        split?.portions?.let { portions ->
            Text(
                text = if (isArabic) "الأشخاص المدفوع لهم" else "Persons paid",
                style = MaterialTheme.typography.labelLarge
            )
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                portions.forEach { portion ->
                    val label = if (portion.isPaid) "✅ ${portion.personNumber}" else "⏳ ${portion.personNumber}"
                    Text(text = label, style = MaterialTheme.typography.bodyMedium)
                }
            }
        }

        if (uiState.isLoading) {
            CircularProgressIndicator(modifier = Modifier.align(Alignment.CenterHorizontally))
        }

        Button(
            onClick = viewModel::recordPayment,
            modifier = Modifier.fillMaxWidth(),
            enabled = !uiState.isLoading
        ) {
            Text(if (isArabic) "تحصيل الدفع" else "Collect Payment")
        }

        if (split?.isFullyPaid == true) {
            Card(modifier = Modifier.fillMaxWidth()) {
                Row(
                    modifier = Modifier.padding(16.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(Icons.Default.CheckCircle, contentDescription = null, tint = MaterialTheme.colorScheme.primary)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text(
                        text = if (isArabic) "تم الدفع بالكامل!" else "All paid!",
                        fontWeight = FontWeight.Bold
                    )
                }
            }
        }
    }
}
