// Os textos de skill-up do SWARFARM são poucos e fixos ("Damage +10%", "Cooltime Turn -1"...): traduzidos por regra
const PERCENT_EFFECTS: Record<string, string> = {
  Damage: 'Dano',
  'Effect Rate': 'Chance de efeito',
  Recovery: 'Recuperação',
  Shield: 'Escudo',
  'Attack Bar Recovery': 'Recuperação da Barra de Ataque',
}

/** Traduz um skill-up para pt-BR; textos desconhecidos voltam como vieram (sem espaços sobrando). */
export function translateSkillUp(text: string): string {
  const value = text.trim()

  const percent = /^(.+?) \+(\d+)%$/.exec(value)
  if (percent && PERCENT_EFFECTS[percent[1]]) return `${PERCENT_EFFECTS[percent[1]]} +${percent[2]}%`

  const cooldown = /^Cooltime Turn -(\d+)$/.exec(value)
  if (cooldown) return `Recarga -${cooldown[1]} turno${cooldown[1] === '1' ? '' : 's'}`

  const harmful = /^Harmful Effect Rate \+(\d+) Turns?$/.exec(value)
  if (harmful) return `Duração dos efeitos nocivos +${harmful[1]} turno${harmful[1] === '1' ? '' : 's'}`

  return value
}
