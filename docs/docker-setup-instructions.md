# Docker Setup Instructions

**Issue:** Docker Desktop is not running on this Windows machine.

## To Start Backend Services (Option 1: Docker)

1. **Start Docker Desktop:**
   - Open Docker Desktop from Windows Start menu
   - Wait for Docker Engine to start (whale icon in system tray)
   - Verify with: `docker ps`

2. **Run backend:**
   ```bash
   cd C:\Vigil_Eye
   docker-compose up -d
   ```

3. **Verify services:**
   ```bash
   docker-compose ps
   curl http://localhost:8080/health  # Auth & Pairing
   curl http://localhost:8081/health  # Ingestion
   ```

## Alternative: Local Development without Docker (Option 2)

If Docker is not available, you can run services locally:

### Prerequisites
- .NET 8 SDK installed
- PostgreSQL 16 installed locally
- Redis installed locally (or use MemoryDB)

### Start Services Locally

```bash
# Terminal 1: Auth & Pairing Service
cd C:\Vigil_Eye\backend\auth-pairing
dotnet restore
dotnet run

# Terminal 2: Ingestion Service
cd C:\Vigil_Eye\backend\ingestion
dotnet restore
dotnet run

# Terminal 3: PostgreSQL (if installed locally)
# Use your local PostgreSQL instance on port 5432
```

## Next Steps After Backend is Running

Once services are up (either via Docker or locally), proceed to Phase 1 MVP implementation:

- **VE-009:** Android companion app setup
- **VE-010:** Persistent on-device indicator
- **VE-011:** Call log collection
- **VE-012:** Device pairing UI
- **VE-013:** Local encrypted queue
- **VE-014-018:** Windows dashboard

---

**Status:** Phase 0 complete. Phase 1 ready to begin once backend is running.
