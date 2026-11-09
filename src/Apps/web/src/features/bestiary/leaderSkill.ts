import { ELEMENT_NAME_LABELS, type ElementName } from '@/lib/game/labels'
import type { LeaderSkill } from './useBestiary'

const ATTRIBUTES: Record<string, string> = {
  'Attack Power': 'o ATK',
  Defense: 'a DEF',
  HP: 'o HP',
  'Attack Speed': 'a VEL',
  'Critical Rate': 'a Taxa Crít.',
  'Critical DMG': 'o Dano Crít.',
  Resistance: 'a RES',
  Accuracy: 'a PRE',
}

const AREAS: Record<string, string> = {
  General: '',
  Arena: ' na Arena',
  Guild: ' no conteúdo de guilda',
  Dungeon: ' nas dungeons',
  Raid: ' nas raids',
}

/** "Aumenta o ATK dos aliados em 25% na Arena"; atributos e áreas desconhecidos aparecem como vêm do SWARFARM. */
export function leaderSkillText(leader: LeaderSkill): string {
  const attribute = ATTRIBUTES[leader.attribute] ?? leader.attribute
  const allies =
    leader.area === 'Element' && leader.element
      ? `dos aliados de ${ELEMENT_NAME_LABELS[leader.element as ElementName]?.toLowerCase() ?? leader.element}`
      : 'dos aliados'
  const area = leader.area === 'Element' ? '' : (AREAS[leader.area] ?? ` (${leader.area})`)
  return `Aumenta ${attribute} ${allies} em ${leader.amount}%${area}`
}
