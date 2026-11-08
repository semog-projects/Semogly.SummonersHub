import { Navigate, useLocation, useNavigate } from 'react-router'
import { AuthForm } from '@/features/auth/AuthForm'
import { useCurrentUser } from '@/features/auth/useAuth'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { data: user } = useCurrentUser()

  // Volta para a página que pediu o login
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  if (user) {
    return <Navigate to={from} replace />
  }

  return <AuthForm onSuccess={() => navigate(from, { replace: true })} />
}
