import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import App from './App.tsx'
import { Toaster, ConfirmHost } from './components/feedback'
import { initTheme } from './theme'

initTheme()

// Subpath the app is served under (matches Vite's base / VITE_BASE_PATH).
const basename = (import.meta.env.VITE_BASE_PATH ?? '').replace(/\/$/, '')

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter basename={basename}>
      <App />
      <Toaster />
      <ConfirmHost />
    </BrowserRouter>
  </StrictMode>,
)
