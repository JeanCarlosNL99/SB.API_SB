import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { App } from './App';
import { AuthenticationProvider } from './context/AuthenticationProvider';
import './styles/global.css';

/**
 * Punto de entrada de la aplicacion. El proveedor de autenticacion envuelve al
 * enrutador para que las guardas de ruta puedan consultar la sesion.
 */
const rootElement = document.getElementById('root');

if (rootElement === null) {
  throw new Error('No se encontro el elemento raiz de la aplicacion.');
}

createRoot(rootElement).render(
  <StrictMode>
    <BrowserRouter>
      <AuthenticationProvider>
        <App />
      </AuthenticationProvider>
    </BrowserRouter>
  </StrictMode>,
);
