import { applyTheme, isDark, storedTheme, THEME_STORAGE_KEY } from './theme'

afterEach(() => {
  localStorage.clear()
  document.documentElement.classList.remove('dark')
})

describe('theme', () => {
  it('resolve o tema do sistema pela preferência do navegador', () => {
    expect(isDark('Dark', false)).toBe(true)
    expect(isDark('Light', true)).toBe(false)
    expect(isDark('System', true)).toBe(true)
    expect(isDark('System', false)).toBe(false)
  })

  it('aplica a classe dark e guarda o tema neste navegador', () => {
    applyTheme('Dark')
    expect(document.documentElement).toHaveClass('dark')
    expect(storedTheme()).toBe('Dark')

    applyTheme('Light')
    expect(document.documentElement).not.toHaveClass('dark')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('Light')
  })

  it('usa o escuro quando não há tema guardado ou o valor é desconhecido', () => {
    expect(storedTheme()).toBe('Dark')
    localStorage.setItem(THEME_STORAGE_KEY, 'Roxo')
    expect(storedTheme()).toBe('Dark')
  })
})
