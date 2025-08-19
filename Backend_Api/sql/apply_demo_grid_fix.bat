@echo off
echo Applying demo grid function fix...
set PGPASSWORD=Magnus123$
"C:\Program Files\PostgreSQL\15\bin\psql" -h localhost -p 5433 -U postgres -d postgres -f "fix_demo_grid_function.sql"
echo Done.
