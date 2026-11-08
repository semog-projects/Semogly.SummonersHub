import { STAT_COLUMNS } from '../monsters/statColumns'
import type { DungeonName, PveStatName, StatValues } from '../types'

/** Nomes das dungeons como a comunidade usa (em inglês, como no jogo). */
export const DUNGEON_LABELS: Record<DungeonName, string> = {
  GiantsKeep: "Giant's Keep",
  DragonsLair: "Dragon's Lair",
  Necropolis: 'Necropolis',
  SpiritualRealm: 'Spiritual Realm',
  PunishersCrypt: "Punisher's Crypt",
}

/** "VEL 180", "PRE 45%". */
export function formatPveStat(stat: PveStatName, value: number): string {
  const key = (stat.charAt(0).toLowerCase() + stat.slice(1)) as keyof StatValues
  const column = STAT_COLUMNS.find((c) => c.key === key)
  return column ? `${column.label} ${value}${column.percent ? '%' : ''}` : `${stat} ${value}`
}
