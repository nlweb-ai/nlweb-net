#!/usr/bin/env pwsh

Write-Host "Testing markdownlint installation and output..." -ForegroundColor Blue

# First try to run markdownlint directly
Write-Host "`n1. Trying markdownlint directly:" -ForegroundColor Yellow
try {
    $result = & markdownlint --version 2>&1
    Write-Host "markdownlint version: $result" -ForegroundColor Green
} catch {
    Write-Host "markdownlint not found directly" -ForegroundColor Red
}

# Try with npx
Write-Host "`n2. Trying with npx:" -ForegroundColor Yellow
try {
    $result = npx markdownlint-cli --version 2>&1
    Write-Host "npx markdownlint-cli version: $result" -ForegroundColor Green
} catch {
    Write-Host "npx markdownlint-cli failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Try to run on a single file and capture output
Write-Host "`n3. Testing on package-validation.md:" -ForegroundColor Yellow
try {
    $result = & markdownlint --config .markdownlint.json doc/package-validation.md 2>&1
    if ($result) {
        Write-Host "Markdownlint output:" -ForegroundColor Green
        $result | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
    } else {
        Write-Host "No output from markdownlint" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error running markdownlint: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nDone." -ForegroundColor Blue
