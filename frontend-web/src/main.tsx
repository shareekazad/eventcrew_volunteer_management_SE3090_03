import React from 'react';
import ReactDOM from 'react-dom/client';
import { App } from './App';
import { ensureAuthenticated } from './services/api';
import './index.css';

async function bootstrap() {
  try {
    const token = await ensureAuthenticated();
    if (!token) {
      console.warn('No auth token — API calls will fail until login succeeds');
    } else {
      console.log('Auth token ready');
    }
  } catch (err) {
    console.warn('Auth bootstrap failed:', err);
  }

  // Small delay to ensure any storage writes are flushed
  await new Promise((resolve) => setTimeout(resolve, 150));

  ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>
  );
}

bootstrap();