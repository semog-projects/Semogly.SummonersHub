import { Navigate, useLocation, useNavigate, useSearchParams } from 'react-router'
import { AuthForm } from '@/features/auth/AuthForm'
import { ExternalLogin } from '@/features/auth/ExternalLogin'
import { useCurrentUser } from '@/features/auth/useAuth'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { data: user } = useCurrentUser()
  const [searchParams] = useSearchParams()

  // Volta para a página que pediu o login
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  if (user) {
    return <Navigate to={from} replace />
  }

  return (
    <AuthForm
      onSuccess={() => navigate(from, { replace: true })}
      extra={<ExternalLogin returnUrl={from} error={searchParams.get('erro')} />}
    />
  )
}
