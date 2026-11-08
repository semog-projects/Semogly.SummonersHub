import sampleText from './__fixtures__/export-sample.json?raw'
import { ExportParseError, parseExport, parseExportText } from './parseExport'

// Fixture anonimizada (wizard fictício e credenciais falsas): nunca usar um export real nos testes
const sample = () => JSON.parse(sampleText)

describe('parseExport', () => {
  it('não inclui credenciais nem dados do dispositivo no payload', () => {
    const payload = JSON.stringify(parseExportText(sampleText))

    expect(payload).not.toContain('FAKE-SESSION-KEY')
    expect(payload).not.toContain('FAKE-ACCOUNT-TOKEN')
    expect(payload).not.toContain('FAKE-DEVICE-ID')
    expect(payload).not.toMatch(/session_key|token|channel_uid/)
  })

  it('não inclui dados de terceiros (amigos)', () => {
    expect(JSON.stringify(parseExportText(sampleText))).not.toContain('SomeFriend')
  })

  it('extrai o invocador', () => {
    const { wizard } = parseExportText(sampleText)

    expect(wizard).toMatchObject({ id: 12345678, name: 'TestWizard', level: 50 })
  })

  it('extrai monstros com HP base real (con × 15)', () => {
    const { monsters } = parseExportText(sampleText)
    const raw = sample().unit_list[0]

    expect(monsters).toHaveLength(2)
    expect(monsters[0]).toMatchObject({ id: raw.unit_id, masterId: raw.unit_master_id, stars: raw.class })
    expect(monsters[0].baseStats.hp).toBe(raw.con * 15)
    expect(monsters[0].skills[0]).toEqual({ id: raw.skills[0][0], level: raw.skills[0][1] })
  })

  it('junta runas equipadas e do inventário, indicando o monstro', () => {
    const raw = sample()
    const { runes } = parseExport(raw)
    const equippedCount = raw.unit_list.reduce((total: number, unit: { runes: unknown[] }) => total + unit.runes.length, 0)

    expect(runes).toHaveLength(equippedCount + raw.runes.length)
    expect(runes.filter((rune) => rune.equippedMonsterId === raw.unit_list[0].unit_id)).toHaveLength(6)
    expect(runes.filter((rune) => rune.equippedMonsterId === null)).toHaveLength(raw.runes.length)
  })

  it('normaliza runas ancient e inata vazia', () => {
    const { runes } = parseExportText(sampleText)
    const ancient = runes.find((rune) => rune.isAncient)

    expect(ancient).toBeDefined()
    expect(ancient!.stars).toBe(6)
    expect(ancient!.originalRarity).toBeLessThanOrEqual(5)
    expect(runes.some((rune) => rune.innateStat === null)).toBe(true)
  })

  it('mapeia grind e gem das subs', () => {
    const raw = sample()
    raw.runes[0].sec_eff[0] = [8, 5, 1, 3]

    const rune = parseExport(raw).runes.find((r) => r.id === raw.runes[0].rune_id)!

    expect(rune.substats[0]).toEqual({ type: 8, value: 5, isGemmed: true, grindValue: 3 })
  })

  it('junta artefatos equipados e do inventário', () => {
    const raw = sample()
    const { artifacts } = parseExport(raw)

    expect(artifacts).toHaveLength(raw.unit_list[0].artifacts.length + raw.artifacts.length)
    expect(artifacts.filter((artifact) => artifact.equippedMonsterId === raw.unit_list[0].unit_id)).toHaveLength(2)
  })

  it('aceita runas no formato antigo (objeto indexado pelo slot)', () => {
    const raw = sample()
    raw.unit_list[0].runes = Object.fromEntries(raw.unit_list[0].runes.map((rune: unknown, i: number) => [i + 1, rune]))

    expect(parseExport(raw).runes.filter((rune) => rune.equippedMonsterId !== null)).toHaveLength(12)
  })

  it('extrai itens de craft', () => {
    expect(parseExportText(sampleText).craftItems).toHaveLength(3)
  })

  it.each([
    ['texto que não é JSON', 'isso não é json'],
    ['JSON de outro tipo', JSON.stringify({ command: 'Other' })],
    ['export sem wizard_info', JSON.stringify({ command: 'HubUserLogin', unit_list: [] })],
    ['export sem unit_list', JSON.stringify({ command: 'HubUserLogin', wizard_info: { wizard_id: 1 } })],
  ])('rejeita %s', (_, text) => {
    expect(() => parseExportText(text)).toThrow(ExportParseError)
  })
})
