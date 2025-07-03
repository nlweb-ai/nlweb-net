#!/usr/bin/env pwsh

# Markdownlint script for NLWebNet repository
# Runs markdownlint on all markdown files with the project's configuration

Write-Host "Running markdownlint on all markdown files..." -ForegroundColor Cyan

# Run markdownlint with configuration
$result = markdownlint . --ignore node_modules --ignore bin --ignore obj 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "All markdown files pass linting!" -ForegroundColor Green
} else {
    Write-Host "Markdown linting issues found:" -ForegroundColor Red
    Write-Host $result
    exit 1
}
