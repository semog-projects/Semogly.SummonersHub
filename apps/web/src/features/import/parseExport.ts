import type {
  AccountImport,
  Artifact,
  ArtifactSubstat,
  CraftItem,
  Monster,
  Rune,
  RuneSubstat,
  Stat,
} from './types'

/** Tamanho máximo aceito. Um export típico tem de 2 a 10 MB. */
export const MAX_EXPORT_SIZE_BYTES = 30 * 1024 * 1024

/** Campos do export que carregam credenciais ou identificam o dispositivo. Nunca são enviados. */
export const SENSITIVE_FIELDS = ['session_key', 'session_key_node', 'account_info'] as const

export class ExportParseError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'ExportParseError'
  }
}

// Em runas, `class` acima de 10 indica runa ancient (16 = 6★ ancient); o mesmo vale para `extra`
const ANCIENT_OFFSET = 10

type RawObject = Record<string, unknown>

/** Lê o texto do arquivo exportado e devolve somente os dados do painel (sem dados sensíveis). */
export function parseExportText(text: string): AccountImport {
  let raw: unknown
  try {
    raw = JSON.parse(text)
  } catch {
    throw new ExportParseError('O arquivo não é um JSON válido.')
  }

  return parseExport(raw)
}

export function parseExport(raw: unknown): AccountImport {
  if (!isObject(raw) || raw.command !== 'HubUserLogin') {
    throw new ExportParseError('O arquivo não parece ser um export do SW Exporter (HubUserLogin).')
  }

  const wizardInfo = raw.wizard_info
  if (!isObject(wizardInfo) || typeof wizardInfo.wizard_id !== 'number') {
    throw new ExportParseError('O export não contém os dados do invocador (wizard_info).')
  }

  if (!Array.isArray(raw.unit_list)) {
    throw new ExportParseError('O export não contém a lista de monstros (unit_list).')
  }

  const units = raw.unit_list.filter(isObject)

  // Runas e artefatos equipados ficam dentro de cada monstro; os demais, no inventário
  const equippedRunes = units.flatMap((unit) => toArray(unit.runes))
  const equippedArtifacts = units.flatMap((unit) => toArray(unit.artifacts))

  return {
    schemaVersion: 1,
    exportedAt: typeof raw.tvalue === 'number' ? new Date(raw.tvalue * 1000).toISOString() : null,
    wizard: {
      id: wizardInfo.wizard_id,
      name: String(wizardInfo.wizard_name ?? ''),
      level: num(wizardInfo.wizard_level),
      serverId: typeof raw.this_server_id === 'number' ? raw.this_server_id : null,
    },
    monsters: units.map(toMonster),
    runes: uniqueBy([...equippedRunes, ...toArray(raw.runes)].map(toRune), (rune) => rune.id),
    artifacts: uniqueBy([...equippedArtifacts, ...toArray(raw.artifacts)].map(toArtifact), (a) => a.id),
    craftItems: toArray(raw.rune_craft_item_list).map(toCraftItem),
  }
}

function toMonster(unit: RawObject): Monster {
  return {
    id: num(unit.unit_id),
    masterId: num(unit.unit_master_id),
    level: num(unit.unit_level),
    stars: num(unit.class),
    attribute: num(unit.attribute),
    baseStats: {
      // O jogo guarda o HP base dividido por 15
      hp: num(unit.con) * 15,
      attack: num(unit.atk),
      defense: num(unit.def),
      speed: num(unit.spd),
      resistance: num(unit.resist),
      accuracy: num(unit.accuracy),
      criticalRate: num(unit.critical_rate),
      criticalDamage: num(unit.critical_damage),
    },
    skills: tuples(unit.skills).map(([id, level]) => ({ id: num(id), level: num(level) })),
    isHomunculus: num(unit.homunculus) === 1,
    createdAt: typeof unit.create_time === 'string' ? unit.create_time : null,
  }
}

function toRune(rune: RawObject): Rune {
  const grade = num(rune.class)
  const originalGrade = num(rune.extra)
  const innate = toStat(rune.prefix_eff)

  return {
    id: num(rune.rune_id),
    slot: num(rune.slot_no),
    stars: grade > ANCIENT_OFFSET ? grade - ANCIENT_OFFSET : grade,
    isAncient: grade > ANCIENT_OFFSET,
    setId: num(rune.set_id),
    level: num(rune.upgrade_curr),
    rarity: num(rune.rank),
    originalRarity: originalGrade > ANCIENT_OFFSET ? originalGrade - ANCIENT_OFFSET : originalGrade,
    mainStat: toStat(rune.pri_eff),
    // Runa sem inata vem com [0, 0]
    innateStat: innate.type === 0 ? null : innate,
    substats: tuples(rune.sec_eff).map(
      ([type, value, gemmed, grind]): RuneSubstat => ({
        type: num(type),
        value: num(value),
        isGemmed: num(gemmed) === 1,
        grindValue: num(grind),
      }),
    ),
    // occupied_type 1 = equipada em monstro (2 = inventário)
    equippedMonsterId: num(rune.occupied_type) === 1 && num(rune.occupied_id) > 0 ? num(rune.occupied_id) : null,
  }
}

function toArtifact(artifact: RawObject): Artifact {
  return {
    id: num(artifact.rid),
    type: num(artifact.type),
    attribute: num(artifact.attribute),
    archetype: num(artifact.unit_style),
    rarity: num(artifact.rank),
    originalRarity: num(artifact.natural_rank),
    level: num(artifact.level),
    mainStat: toStat(artifact.pri_effect),
    substats: tuples(artifact.sec_effects).map(
      ([type, value, upgrades]): ArtifactSubstat => ({ type: num(type), value: num(value), upgrades: num(upgrades) }),
    ),
    equippedMonsterId: num(artifact.occupied_id) > 0 ? num(artifact.occupied_id) : null,
  }
}

function toCraftItem(item: RawObject): CraftItem {
  return {
    id: num(item.craft_item_id),
    craftType: num(item.craft_type),
    craftTypeId: num(item.craft_type_id),
    amount: num(item.amount),
  }
}

function toStat(value: unknown): Stat {
  const [type, amount] = Array.isArray(value) ? value : []
  return { type: num(type), value: num(amount) }
}

/** Versões antigas do exporter gravam runas/artefatos como objeto indexado pelo slot. */
function toArray(value: unknown): RawObject[] {
  if (Array.isArray(value)) return value.filter(isObject)
  if (isObject(value)) return Object.values(value).filter(isObject)
  return []
}

function tuples(value: unknown): unknown[][] {
  return Array.isArray(value) ? value.filter(Array.isArray) : []
}

function uniqueBy<T>(items: T[], key: (item: T) => number): T[] {
  const seen = new Map<number, T>()
  for (const item of items) {
    if (!seen.has(key(item))) seen.set(key(item), item)
  }
  return [...seen.values()]
}

function num(value: unknown): number {
  return typeof value === 'number' && Number.isFinite(value) ? value : 0
}

function isObject(value: unknown): value is RawObject {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
