# Set PostgreSQL password
$env:PGPASSWORD = 'Magnus123$'

# Try to find psql in the common installation paths
$psqlPaths = @(
    "C:\Program Files\PostgreSQL\15\bin\psql.exe",
    "C:\Program Files\PostgreSQL\14\bin\psql.exe",
    "C:\Program Files\PostgreSQL\13\bin\psql.exe",
    "C:\Program Files\PostgreSQL\12\bin\psql.exe"
)

$psqlPath = $null
foreach ($path in $psqlPaths) {
    if (Test-Path $path) {
        $psqlPath = $path
        break
    }
}

if ($null -eq $psqlPath) {
    Write-Host "PostgreSQL not found in common locations. Please ensure PostgreSQL is installed."
    Write-Host "Expected paths:"
    $psqlPaths | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "Using PostgreSQL at: $psqlPath"

# Read and execute the SQL file
$query = Get-Content -Path "fix_demo_grid_function.sql" -Raw
$query | & "$psqlPath" -h localhost -p 5433 -U postgres -d postgres
