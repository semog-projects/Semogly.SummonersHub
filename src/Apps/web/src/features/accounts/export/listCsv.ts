import { toCsv } from '@/lib/csv'
import {
  ARCHETYPE_NAME_LABELS,
  ARTIFACT_MAIN_STAT_LABELS,
  AWAKEN_LABELS,
  ELEMENT_NAME_LABELS,
  formatArtifactEffect,
  formatRuneStat,
  RARITY_LABELS,
} from '@/lib/game/labels'
import { artifactKindLabel } from '../artifacts/artifactFilters'
import { STAT_COLUMNS } from '../monsters/statColumns'
import { runeSpeed } from '../runes/efficiency'
import type { ArtifactListItem, EquippedMonster, MonsterListItem, RuneDetail, RuneListItem } from '../types'

/** Monstros com atributos totais, como na lista (sem bônus de líder, torres ou buffs). */
export function monstersCsv(monsters: MonsterListItem[]): string {
  return toCsv(
    [
      'Id',
      'Monstro',
      'Elemento',
      'Arquétipo',
      'Estrelas',
      'Nível',
      'Despertar',
      ...STAT_COLUMNS.map((c) => c.label),
      'Ef. runas (%)',
      'Sets',
      'Runas',
      'Artefatos',
    ],
    monsters.map((m) => [
      m.monsterId,
      m.name ?? `Monstro ${m.masterId}`,
      ELEMENT_NAME_LABELS[m.element],
      m.archetype ? ARCHETYPE_NAME_LABELS[m.archetype] : null,
      m.stars,
      m.level,
      m.awakenLevel === null ? null : AWAKEN_LABELS[m.awakenLevel],
      ...STAT_COLUMNS.map((c) => m.total[c.key]),
      m.averageRuneEfficiency,
      m.activeSets.join(', '),
      m.runes,
      m.artifacts,
    ]),
  )
}

/** Runas com principal, inata e as 4 subs (valor com grind, marcando grind e gem). */
export function runesCsv(runes: RuneListItem[]): string {
  return toCsv(
    [
      'Id',
      'Set',
      'Slot',
      'Estrelas',
      'Ancient',
      'Nível',
      'Raridade',
      'Principal',
      'Inata',
      'Sub 1',
      'Sub 2',
      'Sub 3',
      'Sub 4',
      'VEL (subs)',
      'Eficiência (%)',
      'Eficiência máx. (%)',
      'Equipada em',
    ],
    runes.map(({ rune, equippedOn }) => [
      rune.runeId,
      rune.set,
      rune.slot,
      rune.stars,
      rune.isAncient,
      rune.level,
      RARITY_LABELS[rune.originalRarity],
      formatRuneStat(rune.mainStat.type, rune.mainStat.value),
      rune.innateStat && formatRuneStat(rune.innateStat.type, rune.innateStat.value),
      ...fourSubs(rune),
      runeSpeed(rune),
      rune.efficiency,
      rune.maxEfficiency,
      monsterName(equippedOn),
    ]),
  )
}

export function artifactsCsv(artifacts: ArtifactListItem[]): string {
  return toCsv(
    ['Id', 'Artefato', 'Nível', 'Raridade', 'Principal', 'Sub 1', 'Sub 2', 'Sub 3', 'Sub 4', 'Equipado em'],
    artifacts.map(({ artifact, equippedOn }) => [
      artifact.artifactId,
      artifactKindLabel(artifact),
      artifact.level,
      RARITY_LABELS[artifact.originalRarity],
      `${ARTIFACT_MAIN_STAT_LABELS[artifact.mainStat.type] ?? 'Atributo'} +${artifact.mainStat.value}`,
      ...[0, 1, 2, 3].map((i) => {
        const sub = artifact.substats[i]
        return sub && formatArtifactEffect(sub.type, sub.value)
      }),
      monsterName(equippedOn),
    ]),
  )
}

function fourSubs(rune: RuneDetail): Array<string | null> {
  return [0, 1, 2, 3].map((i) => {
    const sub = rune.substats[i]
    if (!sub) return null
    const marks = [sub.grindValue > 0 && `grind ${sub.grindValue}`, sub.isGemmed && 'gem'].filter(Boolean).join(', ')
    return `${formatRuneStat(sub.type, sub.value + sub.grindValue)}${marks ? ` (${marks})` : ''}`
  })
}

function monsterName(monster: EquippedMonster | null): string {
  return monster ? (monster.name ?? `Monstro ${monster.masterId}`) : 'Inventário'
}
