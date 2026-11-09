import { useEffect } from 'react'
import { useCurrentUser } from '@/features/auth/useAuth'
import { applyTheme, storedTheme, watchSystemTheme } from '@/lib/theme'
import { usePreferences } from './useSettings'

/**
 * Mantém o tema da página: o salvo no servidor (com login) ou o deste navegador, e acompanha o sistema
 * quando o tema é "Do sistema". Não desenha nada.
 */
export function ThemeController() {
  const { data: user } = useCurrentUser()
  const { data: preferences } = usePreferences(Boolean(user))
  const theme = preferences?.theme ?? storedTheme()

  useEffect(() => {
    applyTheme(theme)
    return theme === 'System' ? watchSystemTheme(() => applyTheme('System')) : undefined
  }, [theme])

  return null
}
