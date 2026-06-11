# Smart Energy City / Ағымды Қала

Educational game prototype for Astana energy and ecology.

## Stack
- Backend: ASP.NET Core 10 + EF Core + PostgreSQL + 2GIS API integration
- Frontend: React + Leaflet + Axios
- Persistence: PostgreSQL (server) and localStorage autosave (client)

## Backend
```bash
cd /home/runner/work/smart-energy-city1/smart-energy-city1/kent1012/smart-energy-city1/backend/SmartEnergyCity.Api
dotnet restore
dotnet run
```

### Backend configuration
`appsettings.json`:
- `ConnectionStrings:PostgreSql`
- `TwoGis:ApiKey`
- `TwoGis:BaseUrl`

If `TwoGis:ApiKey` is empty, the API uses fallback Astana district/building data.

## Frontend
```bash
cd /home/runner/work/smart-energy-city1/smart-energy-city1/kent1012/smart-energy-city1/frontend
npm install
npm run dev
```

Optional frontend env:
- `VITE_API_BASE_URL=http://localhost:5029/api`

## Main implemented game features
- 5 progression stages based on connected objects
- 5 Astana districts with map boundaries and center coordinates
- Real-data integration path through 2GIS API + graceful fallback
- Generator building (coal/gas/solar/wind), substation and power line actions
- Power supply vs demand tracking and ecology/CO2 scoring
- RU/KZ UI translations and language switch
- Building power status color coding on map (gray/yellow/green)
- Educational cards for each energy type
- Autosave to localStorage every 30 seconds
- No registration/authentication required
