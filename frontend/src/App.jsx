import { useEffect, useMemo, useState } from 'react'
import axios from 'axios'
import { CircleMarker, MapContainer, Polygon, Popup, TileLayer } from 'react-leaflet'
import './App.css'

const storageKey = 'smart-energy-city-state-v1'
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5029/api',
  timeout: 8000,
})

const generatorPalette = {
  coal: '#8b4513',
  gas: '#4169e1',
  solar: '#ffd700',
  wind: '#87ceeb',
}

const translations = {
  ru: {
    title: 'Smart Energy City',
    subtitle: 'Астана: энергетика и экология',
    stage: 'Этап',
    connected: 'Подключено зданий',
    power: 'Энергия',
    co2: 'CO2 / час',
    ecology: 'Экология',
    score: 'Очки',
    help: 'Помощь',
    addGenerator: '+ Генератор',
    addSubstation: '+ Подстанция',
    addPowerLine: '+ ЛЭП',
    connect: 'Подключить',
    reset: 'Сбросить',
    mapLegend: 'Легенда',
    noPower: 'Нет энергии',
    connectedPower: 'Подключено',
    ecoPower: 'Эко-энергия',
    generatorType: 'Тип генератора',
    close: 'Закрыть',
    education: 'Обучающие карточки',
    districtOnline: 'Районы с питанием',
    districts: 'районов',
    startMoney: 'Стартовый капитал 10,000',
  },
  kz: {
    title: 'Smart Energy City',
    subtitle: 'Астана: энергетика және экология',
    stage: 'Кезең',
    connected: 'Қосылған ғимараттар',
    power: 'Энергия',
    co2: 'CO2 / сағат',
    ecology: 'Экология',
    score: 'Ұпай',
    help: 'Көмек',
    addGenerator: '+ Генератор',
    addSubstation: '+ Қосалқы станция',
    addPowerLine: '+ ЭБЖ',
    connect: 'Қосу',
    reset: 'Қалпына келтіру',
    mapLegend: 'Аңыз',
    noPower: 'Энергия жоқ',
    connectedPower: 'Қосылған',
    ecoPower: 'Эко-энергия',
    generatorType: 'Генератор түрі',
    close: 'Жабу',
    education: 'Оқу карточкалары',
    districtOnline: 'Қуаты бар аудандар',
    districts: 'аудан',
    startMoney: 'Бастапқы капитал 10,000',
  },
}

const fallbackDistricts = [
  {
    id: 1,
    name: 'esil',
    nameRu: 'Есиль',
    nameKz: 'Есіл',
    centerLat: 51.1694,
    centerLng: 71.4491,
    boundary: [
      { lat: 51.175, lng: 71.436 },
      { lat: 51.179, lng: 71.46 },
      { lat: 51.163, lng: 71.467 },
      { lat: 51.158, lng: 71.438 },
    ],
  },
  {
    id: 2,
    name: 'akmol',
    nameRu: 'Акмол',
    nameKz: 'Ақмол',
    centerLat: 51.14,
    centerLng: 71.52,
    boundary: [
      { lat: 51.147, lng: 71.505 },
      { lat: 51.149, lng: 71.535 },
      { lat: 51.134, lng: 71.536 },
      { lat: 51.132, lng: 71.508 },
    ],
  },
  {
    id: 3,
    name: 'saryarka',
    nameRu: 'Сарыарка',
    nameKz: 'Сарыарқа',
    centerLat: 51.12,
    centerLng: 71.48,
    boundary: [
      { lat: 51.127, lng: 71.465 },
      { lat: 51.13, lng: 71.495 },
      { lat: 51.114, lng: 71.498 },
      { lat: 51.109, lng: 71.468 },
    ],
  },
  {
    id: 4,
    name: 'aygarlyn',
    nameRu: 'Айгарлын',
    nameKz: 'Айғарлын',
    centerLat: 51.16,
    centerLng: 71.39,
    boundary: [
      { lat: 51.168, lng: 71.377 },
      { lat: 51.171, lng: 71.404 },
      { lat: 51.152, lng: 71.408 },
      { lat: 51.149, lng: 71.382 },
    ],
  },
  {
    id: 5,
    name: 'almaty',
    nameRu: 'Алматинский',
    nameKz: 'Алматы',
    centerLat: 51.09,
    centerLng: 71.55,
    boundary: [
      { lat: 51.097, lng: 71.536 },
      { lat: 51.101, lng: 71.564 },
      { lat: 51.086, lng: 71.568 },
      { lat: 51.081, lng: 71.541 },
    ],
  },
]

const fallbackDashboard = {
  stage: 1,
  currency: 10000,
  connectedBuildings: 0,
  totalDemandKw: 0,
  totalSupplyKw: 0,
  totalCo2PerHour: 0,
  ecologyPercent: 100,
  score: 0,
  districtsOnline: 0,
  totalBuildings: 0,
}

const getBuildingColor = (building) => {
  if (!building.isConnected) return '#666666'
  return building.isEcoPowered ? '#00d26a' : '#ffdc4d'
}

const normalizeDistrict = (district) => ({
  ...district,
  boundary: district.boundary?.map((point) => [point.lat, point.lng]) ?? [],
})

function App() {
  const saved = useMemo(() => {
    const raw = window.localStorage.getItem(storageKey)
    return raw ? JSON.parse(raw) : null
  }, [])

  const [language, setLanguage] = useState(saved?.language ?? 'ru')
  const [dashboard, setDashboard] = useState(saved?.dashboard ?? fallbackDashboard)
  const [districts, setDistricts] = useState(saved?.districts ?? fallbackDistricts)
  const [buildings, setBuildings] = useState(saved?.buildings ?? [])
  const [cards, setCards] = useState(saved?.cards ?? [])
  const [modalBuilding, setModalBuilding] = useState(null)
  const [generatorType, setGeneratorType] = useState('solar')
  const [error, setError] = useState('')

  const t = translations[language]

  const loadData = async () => {
    try {
      const [districtRes, stateRes, buildingRes, cardsRes] = await Promise.all([
        api.get('/map/districts'),
        api.get('/game/state'),
        api.get('/game/buildings'),
        api.get('/game/education-cards'),
      ])

      setDistricts(districtRes.data.map(normalizeDistrict))
      setDashboard(stateRes.data)
      setBuildings(buildingRes.data)
      setCards(cardsRes.data)
      setError('')
    } catch {
      setDistricts(fallbackDistricts.map(normalizeDistrict))
      setError('Backend unavailable. Running in local mode.')
    }
  }

  useEffect(() => {
    const timer = setTimeout(() => {
      void loadData()
    }, 0)

    return () => clearTimeout(timer)
  }, [])

  useEffect(() => {
    const timer = setInterval(() => {
      window.localStorage.setItem(
        storageKey,
        JSON.stringify({ language, dashboard, districts, buildings, cards }),
      )
    }, 30000)

    return () => clearInterval(timer)
  }, [language, dashboard, districts, buildings, cards])

  const connectBuilding = async (buildingId) => {
    try {
      const response = await api.post(`/game/buildings/${buildingId}/connect`)
      setDashboard(response.data)
      await loadData()
    } catch (requestError) {
      setError(requestError.response?.data?.message ?? 'Operation failed')
    }
  }

  const buildGenerator = async () => {
    const district = districts[0]
    if (!district) return

    try {
      const response = await api.post('/game/generator', {
        districtId: district.id,
        type: generatorType,
        latitude: district.centerLat,
        longitude: district.centerLng,
      })
      setDashboard(response.data)
      await loadData()
    } catch (requestError) {
      setError(requestError.response?.data?.message ?? 'Operation failed')
    }
  }

  const buildSubstation = async () => {
    const district = districts[0]
    if (!district) return

    try {
      const response = await api.post('/game/substation', {
        districtId: district.id,
        name: 'Central substation',
        latitude: district.centerLat + 0.01,
        longitude: district.centerLng + 0.01,
      })
      setDashboard(response.data)
      await loadData()
    } catch (requestError) {
      setError(requestError.response?.data?.message ?? 'Operation failed')
    }
  }

  const buildPowerLine = async () => {
    try {
      const response = await api.post('/game/power-lines', {
        fromNode: 'substation-1',
        toNode: 'district-grid',
        lengthKm: 1,
        capacityKw: 300,
      })
      setDashboard(response.data)
      await loadData()
    } catch (requestError) {
      setError(requestError.response?.data?.message ?? 'Operation failed')
    }
  }

  const resetGame = async () => {
    try {
      const response = await api.post('/game/reset')
      setDashboard(response.data)
      await loadData()
      setError('')
    } catch {
      setError('Reset failed')
    }
  }

  return (
    <div className="app-shell">
      <header className="top-bar">
        <div>
          <h1>{t.title}</h1>
          <p>{t.subtitle}</p>
        </div>
        <div className="top-controls">
          <select value={language} onChange={(event) => setLanguage(event.target.value)}>
            <option value="ru">RU</option>
            <option value="kz">KZ</option>
          </select>
          <div className="ecology-chart">
            <span>{t.ecology}</span>
            <div className="ecology-track">
              <div style={{ width: `${dashboard.ecologyPercent}%` }} />
            </div>
            <strong>{dashboard.ecologyPercent.toFixed(1)}%</strong>
          </div>
          <button type="button">{t.help}</button>
        </div>
      </header>

      <main className="main-layout">
        <aside className="left-panel">
          <h2>
            {t.stage}: {dashboard.stage}/5
          </h2>
          <ul>
            <li>
              {t.connected}: {dashboard.connectedBuildings}/{dashboard.totalBuildings}
            </li>
            <li>
              {t.power}: {dashboard.totalSupplyKw.toFixed(0)} / {dashboard.totalDemandKw.toFixed(0)} kW
            </li>
            <li>
              {t.co2}: {dashboard.totalCo2PerHour.toFixed(1)}
            </li>
            <li>
              {t.ecology}: {dashboard.ecologyPercent.toFixed(1)}%
            </li>
            <li>
              {t.score}: {dashboard.score}
            </li>
            <li>
              {t.districtOnline}: {dashboard.districtsOnline}/5 {t.districts}
            </li>
            <li>
              ₸ {dashboard.currency.toLocaleString()} · {t.startMoney}
            </li>
          </ul>
          {error ? <div className="error-box">{error}</div> : null}
          <button type="button" className="reset-btn" onClick={resetGame}>
            {t.reset}
          </button>
        </aside>

        <section className="map-panel">
          <MapContainer center={[51.14, 71.48]} zoom={11} className="map" scrollWheelZoom>
            <TileLayer
              attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />

            {districts.map((district) => (
              <Polygon
                key={district.id}
                positions={district.boundary}
                pathOptions={{ color: '#00d4ff', fillOpacity: 0.08 }}
              >
                <Popup>{language === 'ru' ? district.nameRu : district.nameKz}</Popup>
              </Polygon>
            ))}

            {buildings.map((building) => (
              <CircleMarker
                key={building.id}
                center={[building.latitude, building.longitude]}
                radius={6}
                pathOptions={{ color: getBuildingColor(building), fillOpacity: 0.9 }}
                eventHandlers={{ click: () => setModalBuilding(building) }}
              />
            ))}
          </MapContainer>

          <div className="legend-box">
            <h3>{t.mapLegend}</h3>
            <p>
              <span className="dot gray" /> {t.noPower}
            </p>
            <p>
              <span className="dot yellow" /> {t.connectedPower}
            </p>
            <p>
              <span className="dot green" /> {t.ecoPower}
            </p>
          </div>
        </section>
      </main>

      <footer className="bottom-bar">
        <label>
          {t.generatorType}
          <select value={generatorType} onChange={(event) => setGeneratorType(event.target.value)}>
            {Object.entries(generatorPalette).map(([type, color]) => (
              <option key={type} value={type}>
                {type} ({color})
              </option>
            ))}
          </select>
        </label>
        <button type="button" onClick={buildGenerator}>
          {t.addGenerator}
        </button>
        <button type="button" onClick={buildSubstation}>
          {t.addSubstation}
        </button>
        <button type="button" onClick={buildPowerLine}>
          {t.addPowerLine}
        </button>
      </footer>

      <section className="education-panel">
        <h3>{t.education}</h3>
        <div className="cards-grid">
          {cards.map((card) => (
            <article key={card.type} className="edu-card" style={{ borderColor: card.color }}>
              <h4>{language === 'ru' ? card.titleRu : card.titleKz}</h4>
              <p>{language === 'ru' ? card.descriptionRu : card.descriptionKz}</p>
              <small>CO2: {card.co2PerHour}/h</small>
            </article>
          ))}
        </div>
      </section>

      {modalBuilding ? (
        <div className="modal-overlay" role="dialog" aria-modal="true">
          <div className="modal">
            <h3>{modalBuilding.name}</h3>
            <p>Demand: {modalBuilding.powerDemandKw} kW</p>
            <p>Status: {modalBuilding.isConnected ? t.connectedPower : t.noPower}</p>
            <div className="modal-actions">
              <button type="button" onClick={() => connectBuilding(modalBuilding.id)}>
                {t.connect}
              </button>
              <button type="button" onClick={() => setModalBuilding(null)}>
                {t.close}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  )
}

export default App
