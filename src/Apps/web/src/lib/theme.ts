/** Tema escolhido pelo usuário; "System" segue o sistema operacional. */
export type ThemePreference = 'System' | 'Light' | 'Dark'

/** Chave no localStorage: o index.html lê a mesma chave para aplicar o tema antes do React carregar. */
export const THEME_STORAGE_KEY = 'summonershub.theme'

/** Tema de quem ainda não escolheu (o app nasceu escuro). */
export const DEFAULT_THEME: ThemePreference = 'Dark'

export const THEME_OPTIONS: Array<{ value: ThemePreference; label: string }> = [
  { value: 'Light', label: 'Claro' },
  { value: 'Dark', label: 'Escuro' },
  { value: 'System', label: 'Do sistema' },
]

const DARK_QUERY = '(prefers-color-scheme: dark)'

export function systemPrefersDark(): boolean {
  return typeof window.matchMedia === 'function' && window.matchMedia(DARK_QUERY).matches
}

export function isDark(theme: ThemePreference, prefersDark = systemPrefersDark()): boolean {
  return theme === 'Dark' || (theme === 'System' && prefersDark)
}

/** Tema guardado neste navegador (ou o padrão). */
export function storedTheme(): ThemePreference {
  try {
    const value = localStorage.getItem(THEME_STORAGE_KEY)
    return value === 'Light' || value === 'Dark' || value === 'System' ? value : DEFAULT_THEME
  } catch {
    return DEFAULT_THEME
  }
}

/** Aplica o tema na página e guarda neste navegador. */
export function applyTheme(theme: ThemePreference) {
  document.documentElement.classList.toggle('dark', isDark(theme))
  try {
    localStorage.setItem(THEME_STORAGE_KEY, theme)
  } catch {
    // Sem armazenamento (modo privado): o tema vale só nesta visita
  }
}

/** Reaplica o tema quando o sistema troca entre claro e escuro; devolve a função que para de ouvir. */
export function watchSystemTheme(onChange: () => void): () => void {
  if (typeof window.matchMedia !== 'function') return () => {}
  const media = window.matchMedia(DARK_QUERY)
  media.addEventListener('change', onChange)
  return () => media.removeEventListener('change', onChange)
}
