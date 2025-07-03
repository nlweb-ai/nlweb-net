#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fix MD012 multiple consecutive blank lines in markdown files
.DESCRIPTION
    Reduces sequences of more than 2 consecutive blank lines to exactly 2 blank lines
#>

param(
    [string]$FilePath = "doc/package-validation.md"
)

Write-Host "🔧 Fixing MD012 multiple blank lines in $FilePath..." -ForegroundColor Blue

if (-not (Test-Path $FilePath)) {
    Write-Host "❌ File not found: $FilePath" -ForegroundColor Red
    exit 1
}

$content = Get-Content -Path $FilePath -Raw -Encoding UTF8
$originalContent = $content

# Replace sequences of 3 or more consecutive newlines with exactly 2 newlines
# This regex matches 3 or more consecutive line breaks and replaces with 2
$content = $content -replace '(\r?\n\s*){3,}', "`n`n"

if ($content -ne $originalContent) {
    Set-Content -Path $FilePath -Value $content -Encoding UTF8 -NoNewline
    Write-Host "✅ Fixed multiple blank lines in $FilePath" -ForegroundColor Green
} else {
    Write-Host "ℹ️  No multiple blank lines found in $FilePath" -ForegroundColor Yellow
}
