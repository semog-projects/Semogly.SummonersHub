import type { ArtifactListItem, MonsterListItem, RuneListItem } from '../types'
import { artifactsCsv, monstersCsv, runesCsv } from './listCsv'

const lapis = { monsterId: 11, masterId: 19811, name: 'Lapis', element: 'Water' as const }

describe('listCsv', () => {
  it('monstros: atributos totais, HP efetivo e eficiência média', () => {
    const monster: MonsterListItem = {
      monsterId: 11,
      masterId: 19811,
      name: 'Lapis',
      element: 'Water',
      archetype: 'Attack',
      stars: 6,
      level: 40,
      awakenLevel: 1,
      total: { hp: 14281, attack: 1500, defense: 700, speed: 151, criticalRate: 85, criticalDamage: 150, resistance: 15, accuracy: 0, effectiveHp: 51251 },
      activeSets: ['Vampire', 'Revenge'],
      runes: 6,
      artifacts: 2,
      averageRuneEfficiency: 85.5,
    }

    const [header, row] = monstersCsv([monster]).split('\r\n')
    expect(header).toBe(
      'Id;Monstro;Elemento;Arquétipo;Estrelas;Nível;Despertar;HP;ATK;DEF;VEL;Taxa Crít.;Dano Crít.;RES;PRE;HP Efet.;Ef. runas (%);Sets;Runas;Artefatos',
    )
    expect(row).toBe('11;Lapis;Água;Ataque;6;40;Desperto;14281;1500;700;151;85;150;15;0;51251;85,5;Vampire, Revenge;6;2')
  })

  it('runas: subs com grind e gem, eficiência e quem equipa', () => {
    const rune: RuneListItem = {
      rune: {
        runeId: 1,
        slot: 2,
        stars: 6,
        isAncient: true,
        set: 'Swift',
        level: 15,
        rarity: 5,
        originalRarity: 5,
        mainStat: { type: 8, value: 42 },
        innateStat: { type: 9, value: 6 },
        substats: [
          { type: 4, value: 8, isGemmed: false, grindValue: 5 },
          { type: 10, value: 7, isGemmed: true, grindValue: 0 },
        ],
        efficiency: 92.86,
        maxEfficiency: 92.86,
      },
      equippedOn: lapis,
    }

    const row = runesCsv([rune]).split('\r\n')[1]
    expect(row).toBe('1;Swift;2;6;Sim;15;Lendária;VEL +42;Taxa Crít. +6%;ATK +13% (grind 5);Dano Crít. +7% (gem);;;0;92,86;92,86;Lapis')
  })

  it('artefatos: principal, subs e inventário', () => {
    const artifact: ArtifactListItem = {
      artifact: {
        artifactId: 7,
        type: 'Element',
        element: 'Water',
        archetype: null,
        level: 15,
        rarity: 5,
        originalRarity: 5,
        mainStat: { type: 101, value: 100 },
        substats: [{ type: 221, value: 31, upgrades: 2 }],
      },
      equippedOn: null,
    }

    expect(artifactsCsv([artifact]).split('\r\n')[1]).toBe('7;Elemento Água;15;Lendária;ATK +100;Dano adicional de 31% da VEL;;;;Inventário')
  })
})
