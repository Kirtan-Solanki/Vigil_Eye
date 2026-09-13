#!/bin/bash
# Database setup script for Phase 0
# Creates PostgreSQL database and runs initial migrations

set -e

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_USER="${DB_USER:-vigileye_dev}"
DB_PASSWORD="${DB_PASSWORD:-changeme}"
DB_NAME="${DB_NAME:-vigileye}"

echo "Starting database setup for Vigil Eye Phase 0..."

# Wait for PostgreSQL to be ready
until PGPASSWORD=$DB_PASSWORD psql -h "$DB_HOST" -U "$DB_USER" -d postgres -c '\q'; do
  echo "Postgres is unavailable - sleeping"
  sleep 2
done

# Create database if not exists
PGPASSWORD=$DB_PASSWORD psql -h "$DB_HOST" -U "$DB_USER" -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = '$DB_NAME'" | grep -q 1 || \
  PGPASSWORD=$DB_PASSWORD psql -h "$DB_HOST" -U "$DB_USER" -d postgres -c "CREATE DATABASE $DB_NAME;"

echo "Database '$DB_NAME' ready."
echo "Connection string: Server=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;UserId=$DB_USER;Password=***"

# Enable TimescaleDB extension (for time-series hypertables)
PGPASSWORD=$DB_PASSWORD psql -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" -c "CREATE EXTENSION IF NOT EXISTS timescaledb;" || true

echo "Setup complete!"
