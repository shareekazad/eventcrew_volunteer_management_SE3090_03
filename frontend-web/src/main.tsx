import React from 'react';
import ReactDOM from 'react-dom/client';
import { App } from './App';
import { ensureAuthenticated } from './services/api';
import './index.css';

// ── Auto-login as the demo organizer before the app renders ──
// On first load (no stored token), this calls POST /api/Auth/login with the
// seeded organizer credentials and stores the returned JWT in localStorage.
// Subsequent loads reuse the stored token.
ensureAuthenticated()
  .then((token) => {
    if (!token) console.warn('No auth token — API calls will fail until login succeeds');
  })
  .catch((err) => console.warn('Auto-login failed:', err))
  .finally(() => {
    ReactDOM.createRoot(document.getElementById('root')!).render(
      <React.StrictMode>
        <App />
      </React.StrictMode>
    );
  });