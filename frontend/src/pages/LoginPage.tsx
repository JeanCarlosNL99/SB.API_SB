import { useState, type FormEvent } from 'react';
import { Navigate } from 'react-router-dom';
import { BrandLogo } from '@/components/BrandLogo';
import { ErrorMessage } from '@/components/Feedback';
import { useAuthentication } from '@/hooks/useAuthentication';

/**
 * Pantalla de inicio de sesion.
 *
 * Valida en el cliente lo minimo indispensable para no enviar peticiones vacias;
 * la validacion real y la unica fuente de verdad de las credenciales estan en la
 * API.
 */
export function LoginPage() {
  const { login, isAuthenticated } = useAuthentication();

  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<unknown>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/inicio" replace />;
  }

  const isFormComplete = userName.trim().length > 0 && password.length > 0;

  async function handleSubmit(formEvent: FormEvent<HTMLFormElement>) {
    formEvent.preventDefault();

    if (!isFormComplete) {
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      await login(userName.trim(), password);
    } catch (loginError) {
      setError(loginError);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="login-screen">
      <div className="login-card">
        <div className="login-card__brand">
          <BrandLogo variant="dark" />
        </div>

        <h1 className="login-card__title">Portal de mantenimientos y nomina</h1>
        <p className="login-card__description">
          Ingrese sus credenciales institucionales para continuar.
        </p>

        <form className="login-form" onSubmit={handleSubmit} noValidate>
          <ErrorMessage error={error} />

          <div className="field">
            <label className="field__label" htmlFor="userName">
              Usuario
            </label>
            <input
              id="userName"
              className="control"
              type="text"
              autoComplete="username"
              autoFocus
              value={userName}
              onChange={(changeEvent) => setUserName(changeEvent.target.value)}
              required
            />
          </div>

          <div className="field">
            <label className="field__label" htmlFor="password">
              Contrasena
            </label>
            <input
              id="password"
              className="control"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(changeEvent) => setPassword(changeEvent.target.value)}
              required
            />
          </div>

          <button
            type="submit"
            className="button button--primary"
            disabled={!isFormComplete || isSubmitting}
          >
            {isSubmitting ? 'Verificando...' : 'Iniciar sesion'}
          </button>
        </form>

        <p className="login-card__footer">
          Usuario inicial de demostracion: <strong>administrador</strong>. La contrasena
          se define en la seccion <code>Seed</code> de AppSettings.json.
        </p>
      </div>
    </div>
  );
}
