import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { useCurrentUser, useLogout } from './useAuth'

/** E-mail do usuário logado e botão de sair, no cabeçalho. */
export function UserMenu() {
  const navigate = useNavigate()
  const { data: user } = useCurrentUser()
  const logout = useLogout()

  if (!user) return null

  return (
    <div className="flex items-center gap-3">
      <span className="hidden text-sm text-muted-foreground sm:inline">{user.email}</span>
      <Button
        variant="outline"
        size="sm"
        disabled={logout.isPending}
        onClick={() => logout.mutate(undefined, { onSettled: () => navigate('/login') })}
      >
        Sair
      </Button>
    </div>
  )
}
