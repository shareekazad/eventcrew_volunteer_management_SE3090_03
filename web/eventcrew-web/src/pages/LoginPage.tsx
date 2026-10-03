import { useState, type FormEvent } from 'react'
import { isAxiosError } from 'axios'
import { CalendarDays, LoaderCircle, LogIn } from 'lucide-react'
import { useAuth } from '../auth/useAuth'

function getLoginErrorMessage(error: unknown): string {
  if (isAxiosError(error)) {
    if (!error.response || error.response.status >= 500) return 'Unable to connect to the server. Please try again.'
    if (error.response.status === 401) return 'Invalid email or password.'
  }
  return 'Unable to sign in. Check your details and try again.'
}

export default function LoginPage() {
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errorMessage, setErrorMessage] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrorMessage('')
    setIsSubmitting(true)
    try {
      await login(email.trim(), password)
      if (!window.location.hash || window.location.hash === '#login') window.location.hash = '#shifts'
    } catch (error) {
      setErrorMessage(getLoginErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-screen">
      <section className="auth-card" aria-labelledby="login-title">
        <div className="auth-brand"><span className="brand-mark"><CalendarDays size={18} /></span>eventcrew<span className="brand-period">.</span></div>
        <p className="eyebrow">ORGANIZER WORKSPACE</p>
        <h1 id="login-title">Welcome back</h1>
        <p className="auth-subtitle">Sign in to manage your EventCrew workspace.</p>
        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="form-field" htmlFor="login-email">Email
            <input id="login-email" type="email" autoComplete="username" required value={email} onChange={(event) => setEmail(event.target.value)} />
          </label>
          <label className="form-field" htmlFor="login-password">Password
            <input id="login-password" type="password" autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} />
          </label>
          {errorMessage && <p className="auth-error" role="alert">{errorMessage}</p>}
          <button className="button button-primary auth-submit" type="submit" disabled={isSubmitting}>
            {isSubmitting ? <LoaderCircle size={16} className="attendance-spinning" /> : <LogIn size={16} />}
            {isSubmitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </section>
    </main>
  )
}
