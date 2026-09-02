<#
.SYNOPSIS
    Fake ESC/POS thermal printer for testing receipt layout without hardware.

.DESCRIPTION
    Listens on TCP 9100 like a real network receipt printer, saves the raw byte
    stream, and prints a readable rendering of what would have come out on paper.

    Character width matters: ESC/POS can print at double width, where one character
    occupies two printer columns. This script counts real columns per run, so a
    double-width heading is reported at its true printed width rather than by
    character count. See PRINTING.md.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\fake-printer.ps1
#>

param(
    [int]$Port = 9100,
    [string]$Out = "captures"
)

$ErrorActionPreference = "Stop"

# ESC/POS opcodes and how many argument bytes follow them.
$script:EscArgs = @{
    0x21 = 1; 0x2D = 1; 0x40 = 0; 0x45 = 1; 0x47 = 1; 0x4A = 1
    0x52 = 1; 0x61 = 1; 0x64 = 1; 0x74 = 1; 0x7B = 1; 0x4D = 1; 0x20 = 1
}
$script:GsArgs = @{
    0x42 = 1; 0x4C = 2; 0x57 = 2; 0x68 = 1; 0x77 = 1; 0x66 = 1; 0x48 = 1
}

function Convert-EscPosToLines {
    <#
        Strips control sequences and returns one object per line:
          Text    - the printable characters
          Display - the line drawn at true column width: widened runs keep normal
                    letterforms but are centred inside the columns they occupy on paper
          Cols    - printer columns the line actually occupies
        Cols differs from Text.Length only where double-width printing is in effect.
    #>
    param([byte[]]$Raw)

    $lines    = New-Object System.Collections.ArrayList
    $sb       = New-Object System.Text.StringBuilder
    $runs     = New-Object System.Collections.ArrayList
    $runStart = -1
    $runCols  = 0
    $mult     = 1  # current width multiplier; persists until changed, as on a real printer
    $cols     = 0
    $i        = 0

    function Add-Line {
        param($Builder, $Runs, $Cols)
        # Build the display line: normal-width text kept as-is, and each widened run
        # centred inside the columns it occupies on paper.
        $text = $Builder.ToString()
        if ($Runs.Count -eq 0) {
            $display = $text
        } else {
            $display = ""
            $pos = 0
            foreach ($r in $Runs) {
                $display += $text.Substring($pos, $r.Start - $pos)
                $chunk = $text.Substring($r.Start, $r.Len)
                $pad   = [int](($r.Cols - $chunk.Length) / 2)
                if ($pad -lt 0) { $pad = 0 }
                $display += (" " * $pad) + $chunk + (" " * ($r.Cols - $chunk.Length - $pad))
                $pos = $r.Start + $r.Len
            }
            $display += $text.Substring($pos)
        }
        $null = $lines.Add([pscustomobject]@{ Text = $text; Display = $display; Cols = $Cols })
    }

    while ($i -lt $Raw.Length) {
        $b = $Raw[$i]

        if ($b -eq 0x1D -and ($i + 1) -lt $Raw.Length) {              # GS
            $op = $Raw[$i + 1]
            if ($op -eq 0x21 -and ($i + 2) -lt $Raw.Length) {         # GS ! n : character size
                $mult = ((($Raw[$i + 2] -band 0xF0) -shr 4) + 1)      # high nibble = width multiplier - 1
                $i += 3
                continue
            }
            if ($op -eq 0x76 -and ($i + 8) -le $Raw.Length) {         # GS v 0 : raster image
                $xl = $Raw[$i + 4]; $xh = $Raw[$i + 5]
                $yl = $Raw[$i + 6]; $yh = $Raw[$i + 7]
                $size = (($xh -shl 8) -bor $xl) * (($yh -shl 8) -bor $yl)
                [void]$sb.Append("[IMAGE]")
                $i += 8 + $size
                continue
            }
            $skip = if ($script:GsArgs.ContainsKey([int]$op)) { $script:GsArgs[[int]$op] } else { 0 }
            $i += 2 + $skip
            continue
        }

        if ($b -eq 0x1B -and ($i + 1) -lt $Raw.Length) {              # ESC
            $op = $Raw[$i + 1]
            if ($op -eq 0x21 -and ($i + 2) -lt $Raw.Length) {         # ESC ! n : print mode
                $mult = if (($Raw[$i + 2] -band 0x20) -ne 0) { 2 } else { 1 }
                $i += 3
                continue
            }
            $skip = if ($script:EscArgs.ContainsKey([int]$op)) { $script:EscArgs[[int]$op] } else { 0 }
            $i += 2 + $skip
            continue
        }

        if ($b -eq 0x0A) {                                            # line feed
            if ($runStart -ge 0) {
                $null = $runs.Add([pscustomobject]@{ Start = $runStart; Len = ($sb.Length - $runStart); Cols = $runCols })
                $runStart = -1
            }
            Add-Line -Builder $sb -Runs $runs -Cols $cols
            $sb       = New-Object System.Text.StringBuilder
            $runs     = New-Object System.Collections.ArrayList
            $runCols  = 0
            $cols     = 0
            $i++
            continue
        }

        if ($b -ge 0x20 -and $b -lt 0x7F) {
            [void]$sb.Append([char]$b)
            $cols += $mult
            # Record where each widened run starts and how many columns it spans, so the
            # line can be laid out afterwards at true width without letter-spacing the text.
            if ($mult -gt 1) {
                if ($runStart -lt 0) { $runStart = $sb.Length - 1; $runCols = 0 }
                $runCols += $mult
            } elseif ($runStart -ge 0) {
                $null = $runs.Add([pscustomobject]@{ Start = $runStart; Len = ($sb.Length - 1 - $runStart); Cols = $runCols })
                $runStart = -1
            }
        }
        $i++
    }

    if ($sb.Length -gt 0) {
        if ($runStart -ge 0) {
            $null = $runs.Add([pscustomobject]@{ Start = $runStart; Len = ($sb.Length - $runStart); Cols = $runCols })
        }
        Add-Line -Builder $sb -Runs $runs -Cols $cols
    }
    return $lines
}

function Get-PaperWidth {
    # Separator rules are emitted at exactly the configured character width, which is a
    # more reliable signal than the longest line.
    param($Lines)
    $rules = @($Lines | Where-Object {
        $_.Text.Length -gt 0 -and ($_.Text -match '^[-=]+$')
    } | ForEach-Object { $_.Text.Length })
    if ($rules.Count -gt 0) {
        $tally = @{}
        foreach ($r in $rules) { if ($tally.ContainsKey($r)) { $tally[$r]++ } else { $tally[$r] = 1 } }
        $bestLen = 0; $bestCount = -1
        foreach ($k in $tally.Keys) {
            if ($tally[$k] -gt $bestCount) { $bestCount = $tally[$k]; $bestLen = $k }
        }
        return [int]$bestLen
    }
    $max = 0
    foreach ($l in $Lines) { if ($l.Cols -gt $max) { $max = $l.Cols } }
    return $max
}

function Show-Receipt {
    param([byte[]]$Raw)

    $lines = @(Convert-EscPosToLines -Raw $Raw)
    while ($lines.Count -gt 0 -and $lines[-1].Text -eq "") {
        $lines = $lines[0..($lines.Count - 2)]
    }
    if ($lines.Count -eq 0) { Write-Host "  (no printable text)"; return }

    $paper = Get-PaperWidth -Lines $lines

    $maxDisp = 0
    foreach ($l in $lines) { if ($l.Display.Length -gt $maxDisp) { $maxDisp = $l.Display.Length } }
    $box = if ($paper -gt $maxDisp) { $paper } else { $maxDisp }

    Write-Host ""
    Write-Host ("  captured {0} bytes -> {1} lines, paper width {2} cols" -f $Raw.Length, $lines.Count, $paper)
    if ($paper -eq 32) { Write-Host "  looks like 58mm paper" -ForegroundColor Cyan }
    elseif ($paper -eq 48) { Write-Host "  looks like 80mm paper" -ForegroundColor Cyan }
    else { Write-Host ("  WARNING: expected 32 (58mm) or 48 (80mm), got {0}" -f $paper) -ForegroundColor Yellow }

    Write-Host ""
    Write-Host ("  +" + ("-" * ($box + 2)) + "+")
    $overflows = 0
    foreach ($l in $lines) {
        Write-Host ("  | " + $l.Display.PadRight($box) + " |") -NoNewline
        if ($l.Cols -gt $paper) {
            $overflows++
            Write-Host ("  <-- OVERFLOWS: {0} cols on {1}-col paper" -f $l.Cols, $paper) -ForegroundColor Red
        } elseif ($l.Cols -ne $l.Text.Length) {
            # Drawn letter-spaced above because it prints at double width.
            Write-Host ("  <-- 2x width, {0} of {1} cols" -f $l.Cols, $paper) -ForegroundColor DarkCyan
        } else {
            Write-Host ""
        }
    }
    Write-Host ("  +" + ("-" * ($box + 2)) + "+")
    Write-Host ""

    if ($overflows -gt 0) {
        Write-Host ("  !! {0} line(s) exceed the paper width" -f $overflows) -ForegroundColor Red
    } else {
        Write-Host ("  all {0} lines fit within {1} columns" -f $lines.Count, $paper) -ForegroundColor Green
    }
    Write-Host ""
}

# Report every candidate address, flagging virtual adapters the tablet cannot reach.
$candidates = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } |
    Select-Object IPAddress, InterfaceAlias, InterfaceMetric

$virtualPattern = "vEthernet|WSL|Hyper-V|Docker|VirtualBox|VMware|Loopback|Bluetooth"
$real    = @($candidates | Where-Object { $_.InterfaceAlias -notmatch $virtualPattern })
$virtual = @($candidates | Where-Object { $_.InterfaceAlias -match  $virtualPattern })

$best = $real | Where-Object { $_.InterfaceAlias -match "Wi-?Fi|Wireless|WLAN" } | Select-Object -First 1
if (-not $best) { $best = $real | Sort-Object InterfaceMetric | Select-Object -First 1 }
$lanIp = if ($best) { $best.IPAddress } else { "<this machine's LAN IP>" }

if (-not (Test-Path $Out)) { New-Item -ItemType Directory -Path $Out | Out-Null }

$listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Any, $Port)
$listener.Start()

Write-Host ""
Write-Host "Fake ESC/POS printer listening on port $Port" -ForegroundColor Green
Write-Host ""
Write-Host "  In the app:  Settings -> Receipt Printer" -ForegroundColor White
Write-Host "               Connection = LAN" -ForegroundColor White
Write-Host "               Host       = $lanIp" -ForegroundColor Yellow
Write-Host "               Port       = $Port" -ForegroundColor Yellow
Write-Host ""

if ($real.Count -gt 1) {
    Write-Host "  Other real adapters - if the address above does not work, use whichever" -ForegroundColor DarkGray
    Write-Host "  one is on the same subnet as the tablet:" -ForegroundColor DarkGray
    foreach ($c in $real) { Write-Host ("    {0,-16} {1}" -f $c.IPAddress, $c.InterfaceAlias) -ForegroundColor DarkGray }
    Write-Host ""
}
if ($virtual.Count -gt 0) {
    Write-Host "  Ignoring these virtual adapters (unreachable from the tablet):" -ForegroundColor DarkGray
    foreach ($c in $virtual) { Write-Host ("    {0,-16} {1}" -f $c.IPAddress, $c.InterfaceAlias) -ForegroundColor DarkGray }
    Write-Host ""
}

try {
    $probe = New-Object System.Net.Sockets.TcpClient
    $probe.Connect("127.0.0.1", $Port)
    $probe.Close()
    Write-Host "  Self-test: listener is accepting connections. OK" -ForegroundColor Green
} catch {
    Write-Host "  Self-test FAILED - the listener is not accepting connections locally." -ForegroundColor Red
}
Write-Host ""
Write-Host "  Press Ctrl+C to stop." -ForegroundColor DarkGray
Write-Host ""

try {
    while ($true) {
        $client = $listener.AcceptTcpClient()
        Write-Host "--- print job from $($client.Client.RemoteEndPoint.Address) ---" -ForegroundColor Green

        $stream = $client.GetStream()
        $stream.ReadTimeout = 5000
        $buffer = New-Object System.IO.MemoryStream
        $chunk  = New-Object byte[] 4096
        try {
            while ($true) {
                $read = $stream.Read($chunk, 0, $chunk.Length)
                if ($read -le 0) { break }
                $buffer.Write($chunk, 0, $read)
            }
        } catch {
            # Read timeout means the job finished; a printer never replies.
        } finally {
            $client.Close()
        }

        $raw = $buffer.ToArray()
        if ($raw.Length -eq 0) {
            Write-Host "  (empty job - this is the startup self-test, ignore it)" -ForegroundColor DarkGray
            continue
        }

        $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $path  = Join-Path $Out "receipt-$stamp.bin"
        [System.IO.File]::WriteAllBytes($path, $raw)

        Show-Receipt -Raw $raw
        Write-Host "  raw bytes saved to $path" -ForegroundColor DarkGray
        Write-Host ""
    }
} finally {
    $listener.Stop()
    Write-Host "stopped"
}
