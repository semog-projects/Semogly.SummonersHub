import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { MonsterSkills } from '@/features/accounts/types'
import { formatNumber } from '@/lib/format'
import { DamageCalculatorPage } from './DamageCalculatorPage'

const monster: MonsterSkills = {
  monsterId: 11,
  masterId: 19811,
  name: 'Lapis',
  element: 'Water',
  level: 40,
  total: { hp: 15000, attack: 2000, defense: 700, speed: 150, criticalRate: 80, criticalDamage: 150, resistance: 15, accuracy: 0, effectiveHp: 40000 },
  skills: [
    { skillId: 1, name: 'Magic Shot', slot: 2, level: 5, maxLevel: 5, hits: 4, passive: false, aoe: false, formulaText: '1.9*{ATK}', formula: '[["ATK", "*", 1.9]]', damageBonus: 35 },
    { skillId: 2, name: 'Aura', slot: 3, level: 1, maxLevel: 1, hits: 0, passive: true, aoe: false, formulaText: null, formula: null, damageBonus: 0 },
    { skillId: 3, name: 'Contagem', slot: 4, level: 1, maxLevel: 1, hits: 1, passive: false, aoe: false, formulaText: 'x', formula: '[["ABSORPTION_TOT_CNT", "*", 2]]', damageBonus: 0 },
  ],
}

function renderPage() {
  vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(monster), { status: 200 })))
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/contas/1/monstros/11/dano']}>
        <Routes>
          <Route path="/contas/:wizardId/monstros/:monsterId/dano" element={<DamageCalculatorPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const rowOf = (name: string) => screen.getByRole('row', { name: new RegExp(name) })

afterEach(() => vi.unstubAllGlobals())

describe('DamageCalculatorPage', () => {
  it('calcula o dano por tipo de golpe e recalcula com a DEF do alvo', async () => {
    renderPage()
    await screen.findByRole('heading', { name: 'Dano: Lapis' })

    // 2000 × 1,9 × 1,35 × 1000 ÷ 1140 por golpe; 4 golpes
    const normal = Math.round(2000 * 1.9 * 1.35 * (1000 / 1140) * 4)
    const critical = Math.round(2000 * 1.9 * 1.35 * 2.5 * (1000 / 1140) * 4)
    const cells = within(rowOf('Magic Shot')).getAllByRole('cell')
    expect(cells[1]).toHaveTextContent(formatNumber(normal))
    expect(cells[2]).toHaveTextContent(formatNumber(critical))
    expect(rowOf('Magic Shot')).toHaveTextContent('skill-ups +35% · 4 golpes')

    fireEvent.change(screen.getByLabelText('DEF do alvo'), { target: { value: '1000' } })

    const withDefense = Math.round(2000 * 1.9 * 1.35 * (1000 / 4640) * 4)
    expect(within(rowOf('Magic Shot')).getAllByRole('cell')[1]).toHaveTextContent(formatNumber(withDefense))
  })

  it('indica habilidades sem dano e fórmulas não suportadas', async () => {
    renderPage()
    await screen.findByRole('heading', { name: 'Dano: Lapis' })

    expect(rowOf('Aura')).toHaveTextContent('Sem dano')
    expect(rowOf('Contagem')).toHaveTextContent('Fórmula ainda não suportada')
  })
})
