import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import type { InventoryStats } from '../types'
import { InventoryStatsView } from './InventoryStatsView'

const inventory: InventoryStats = {
  runes: {
    total: 829,
    equipped: 349,
    maxLevel: 400,
    ancient: 13,
    bySet: [
      { set: 'Violent', total: 120, equipped: 60, maxLevel: 70 },
      { set: 'Swift', total: 90, equipped: 45, maxLevel: 50 },
    ],
    bySlot: [1, 2, 3, 4, 5, 6].map((key) => ({ key, count: 100 + key })),
    byRarity: [1, 2, 3, 4, 5].map((key) => ({ key, count: key * 10 })),
    byStars: [1, 2, 3, 4, 5, 6].map((key) => ({ key, count: key })),
    topSpeed: [
      {
        runeId: 1, slot: 4, set: 'Swift', stars: 6, level: 15, isAncient: true, originalRarity: 5,
        speed: 32, grind: 5, isGemmed: true, isInnate: false, equippedMonsterId: 99, equippedMonsterName: 'Konamiya',
      },
      {
        runeId: 2, slot: 1, set: 'Violent', stars: 6, level: 12, isAncient: false, originalRarity: 4,
        speed: 18, grind: 0, isGemmed: false, isInnate: true, equippedMonsterId: null, equippedMonsterName: null,
      },
    ],
  },
  artifacts: {
    total: 184,
    equipped: 100,
    maxLevel: 90,
    byType: [
      { key: 'Element', count: 90 },
      { key: 'Archetype', count: 94 },
    ],
    byRarity: [1, 2, 3, 4, 5].map((key) => ({ key, count: key })),
  },
  craft: {
    gems: 3,
    grinds: 7,
    items: [
      { type: 'EnchantedGem', set: 'Will', stat: 9, rarity: 5, amount: 1 },
      { type: 'AncientGem', set: 'Swift', stat: 8, rarity: 5, amount: 2 },
      { type: 'ImmemorialGrindstone', set: 'Immemorial', stat: 4, rarity: 4, amount: 7 },
    ],
  },
}

function renderView() {
  render(
    <MemoryRouter initialEntries={['/contas/1/inventario']}>
      <Routes>
        <Route path="/contas/:wizardId/inventario" element={<InventoryStatsView inventory={inventory} />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('InventoryStatsView', () => {
  it('mostra os totais de runas, artefatos e itens de craft', () => {
    renderView()

    expect(screen.getByText('349 equipadas · 480 no inventário')).toBeInTheDocument()
    expect(screen.getByText('13 ancient')).toBeInTheDocument()
    expect(screen.getByText('100 equipados · 90 em +15')).toBeInTheDocument()
    expect(screen.getByText('3 gems · 7 grinds')).toBeInTheDocument()
  })

  it('lista os sets com total, equipadas e +15', () => {
    renderView()

    const violent = within(screen.getByRole('table', { name: 'Runas por set' })).getByRole('row', { name: /Violent/ })
    expect(violent).toHaveTextContent('1206070')
  })

  it('mostra o top de velocidade com grind, gem, inata e onde está equipada', () => {
    renderView()

    const rows = within(screen.getByRole('table', { name: 'Top runas por velocidade' })).getAllByRole('row').slice(1)
    expect(rows[0]).toHaveTextContent('Swift (4) +15')
    expect(rows[0]).toHaveTextContent('32(5 grind)gem')
    expect(within(rows[0]).getByRole('link', { name: 'Konamiya' })).toHaveAttribute('href', '/contas/1/monstros/99')
    expect(rows[1]).toHaveTextContent('inata')
    expect(rows[1]).toHaveTextContent('Inventário')
  })

  it('separa gems e grinds com o tipo e o atributo', () => {
    renderView()

    const gems = within(screen.getByRole('list', { name: 'Gems disponíveis' })).getAllByRole('listitem')
    expect(gems[0]).toHaveTextContent('Will · Taxa Crít.×1')
    expect(gems[1]).toHaveTextContent('Swift · VELGem Ancient×2')

    const grinds = within(screen.getByRole('list', { name: 'Grinds disponíveis' })).getAllByRole('listitem')
    expect(grinds[0]).toHaveTextContent('Qualquer set · ATK %Grind Immemorial×7')
  })
})
