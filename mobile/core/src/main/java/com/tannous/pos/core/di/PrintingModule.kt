package com.tannous.pos.core.di

import android.content.Context
import com.tannous.pos.core.printing.Printer
import com.tannous.pos.core.printing.SystemPrintPrinter
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object PrintingModule {
    
    /**
     * Provides the Printer implementation.
     * Currently uses SystemPrintPrinter (Android Print Framework).
     * 
     * In the future, this can be swapped for:
     * - EscPosBluetoothPrinter (for thermal printers via Bluetooth)
     * - EscPosNetworkPrinter (for thermal printers via network)
     * - Or use a factory/qualifier pattern to support multiple printer types
     */
    @Provides
    @Singleton
    fun providePrinter(
        @ApplicationContext context: Context
    ): Printer {
        return SystemPrintPrinter(context)
    }
}


