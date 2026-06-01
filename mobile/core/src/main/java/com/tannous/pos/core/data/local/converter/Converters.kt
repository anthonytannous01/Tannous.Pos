package com.tannous.pos.core.data.local.converter

import androidx.room.TypeConverter
import com.tannous.pos.core.data.local.entity.OutboxStatus
import java.math.BigDecimal
import java.time.Instant
import java.util.UUID

class Converters {
    
    @TypeConverter
    fun fromBigDecimal(value: BigDecimal?): String? {
        return value?.toString()
    }
    
    @TypeConverter
    fun toBigDecimal(value: String?): BigDecimal? {
        return value?.let { BigDecimal(it) }
    }
    
    @TypeConverter
    fun fromInstant(value: Instant?): String? {
        return value?.toString()
    }
    
    @TypeConverter
    fun toInstant(value: String?): Instant? {
        return value?.let { Instant.parse(it) }
    }
    
    @TypeConverter
    fun fromUUID(value: UUID?): String? {
        return value?.toString()
    }
    
    @TypeConverter
    fun toUUID(value: String?): UUID? {
        return value?.let { UUID.fromString(it) }
    }
    
    @TypeConverter
    fun fromOutboxStatus(value: OutboxStatus): String {
        return value.name
    }
    
    @TypeConverter
    fun toOutboxStatus(value: String): OutboxStatus {
        return OutboxStatus.valueOf(value)
    }
}
