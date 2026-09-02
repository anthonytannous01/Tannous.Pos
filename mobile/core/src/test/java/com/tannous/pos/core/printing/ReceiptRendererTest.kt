package com.tannous.pos.core.printing

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Layout regression tests for receipt rendering.
 *
 * [ReceiptRenderer] is deliberately free of Android and printer dependencies so receipt layout
 * can be verified on the JVM, without an emulator and without a thermal printer attached.
 */
class ReceiptRendererTest {

    private val rows = ReceiptRenderer.rows(TestReceiptFactory.sample())

    @Test
    fun `paper width maps to the correct character count`() {
        assertEquals(ReceiptRenderer.CHARS_58MM, ReceiptRenderer.charsPerLine(58))
        assertEquals(ReceiptRenderer.CHARS_80MM, ReceiptRenderer.charsPerLine(80))
        // Anything narrower than 58mm is still treated as the narrow profile.
        assertEquals(ReceiptRenderer.CHARS_58MM, ReceiptRenderer.charsPerLine(48))
    }

    @Test
    fun `no plain-text line exceeds the paper width on 58mm`() {
        val width = ReceiptRenderer.CHARS_58MM
        ReceiptRenderer.toPlainText(rows, width).lines().forEach { line ->
            assertTrue("Line overflows ${width}ch: '$line' (${line.length})", line.length <= width)
        }
    }

    @Test
    fun `no plain-text line exceeds the paper width on 80mm`() {
        val width = ReceiptRenderer.CHARS_80MM
        ReceiptRenderer.toPlainText(rows, width).lines().forEach { line ->
            assertTrue("Line overflows ${width}ch: '$line' (${line.length})", line.length <= width)
        }
    }

    @Test
    fun `rules span exactly the configured paper width`() {
        listOf(ReceiptRenderer.CHARS_58MM, ReceiptRenderer.CHARS_80MM).forEach { width ->
            val ruleLines = ReceiptRenderer.toPlainText(rows, width)
                .lines()
                .filter { it.isNotEmpty() && it.all { ch -> ch == '-' || ch == '=' } }
            assertTrue("Expected separator lines at width $width", ruleLines.isNotEmpty())
            ruleLines.forEach { assertEquals(width, it.length) }
        }
    }

    @Test
    fun `long item names are truncated rather than colliding with the price`() {
        val width = ReceiptRenderer.CHARS_58MM
        val text = ReceiptRenderer.toPlainText(rows, width)
        // The sample contains a deliberately long item name.
        val juiceLine = text.lines().first { it.contains("Fresh Orange") }
        assertTrue(juiceLine.length <= width)
        assertTrue("Price must survive truncation", juiceLine.trimEnd().endsWith("$4.00"))
    }

    @Test
    fun `totals and payment details are present`() {
        val text = ReceiptRenderer.toPlainText(rows, ReceiptRenderer.CHARS_80MM)
        assertTrue(text.contains("TOTAL USD"))
        assertTrue(text.contains("TOTAL LBP"))
        assertTrue(text.contains("VAT (11%)"))
        assertTrue(text.contains("Discount"))
        assertTrue(text.contains("Change"))
    }

    @Test
    fun `esc pos output uses alignment tags instead of hardcoded padding`() {
        val escPos = ReceiptRenderer.toEscPos(rows, ReceiptRenderer.CHARS_80MM)
        assertTrue(escPos.contains("[L]Subtotal[R]"))
        assertTrue(escPos.contains("[C]"))
        // Manual padding would reintroduce the width bug the [L]/[R] parser exists to avoid.
        assertFalse(escPos.contains("Subtotal   "))
    }

    @Test
    fun `markup characters in receipt data are neutralized`() {
        val hostile = TestReceiptFactory.sample().let { sample ->
            sample.copy(businessName = "Tan<b>nous</b> [POS]")
        }
        val escPos = ReceiptRenderer.toEscPos(
            ReceiptRenderer.rows(hostile),
            ReceiptRenderer.CHARS_80MM
        )
        assertFalse("Item data must not be able to inject printer markup", escPos.contains("<b>nous</b>"))
        assertFalse(escPos.contains("[POS]"))
    }

    @Test
    fun `tax rate is derived from the receipt, not from settings`() {
        // Sample: subtotal 29.00 - discount 1.00 = 28.00 base, tax 3.08 -> 11%
        assertEquals("11", ReceiptRenderer.taxPercentFor(TestReceiptFactory.sample()))
        val text = ReceiptRenderer.toPlainText(rows, ReceiptRenderer.CHARS_80MM)
        assertTrue(text.contains("VAT (11%)"))
    }

    @Test
    fun `a zero-rate receipt never prints a contradictory VAT percentage`() {
        val noTax = TestReceiptFactory.sample().copy(taxAmount = java.math.BigDecimal.ZERO)
        assertEquals(null, ReceiptRenderer.taxPercentFor(noTax))
        val text = ReceiptRenderer.toPlainText(ReceiptRenderer.rows(noTax), ReceiptRenderer.CHARS_80MM)
        assertFalse("A 0% VAT line must never be printed", text.contains("VAT (0%)"))
    }

    @Test
    fun `double-width header is dropped when it cannot fit the paper`() {
        // 'Tannous Test Kitchen' is 20 chars; at double width that is 40 columns.
        assertFalse(ReceiptRenderer.fitsAtDoubleWidth("Tannous Test Kitchen", ReceiptRenderer.CHARS_58MM))
        assertTrue(ReceiptRenderer.fitsAtDoubleWidth("Tannous Test Kitchen", ReceiptRenderer.CHARS_80MM))

        val narrow = ReceiptRenderer.toEscPos(rows, ReceiptRenderer.CHARS_58MM)
        assertFalse(
            "A name too wide for 58mm must not request the big font",
            narrow.contains("<font size='big'>Tannous Test Kitchen</font>")
        )
        val wide = ReceiptRenderer.toEscPos(rows, ReceiptRenderer.CHARS_80MM)
        assertTrue(wide.contains("<font size='big'>Tannous Test Kitchen</font>"))
    }

    @Test
    fun `a short business name still gets the big font on 58mm`() {
        val short = TestReceiptFactory.sample().copy(businessName = "Tannous")
        val escPos = ReceiptRenderer.toEscPos(
            ReceiptRenderer.rows(short),
            ReceiptRenderer.CHARS_58MM
        )
        assertTrue(escPos.contains("<font size='big'>Tannous</font>"))
    }
}
