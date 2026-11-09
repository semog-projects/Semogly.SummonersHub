import { toCsv } from './csv'

describe('toCsv', () => {
  it('usa ponto e vírgula, vírgula decimal e linhas CRLF', () => {
    expect(toCsv(['Nome', 'Eficiência', 'Gem'], [['Lapis', 85.71, true], ['Tyron', 100, false]])).toBe(
      'Nome;Eficiência;Gem\r\nLapis;85,71;Sim\r\nTyron;100;Não',
    )
  })

  it('não agrupa milhares (o Excel leria o ponto como decimal)', () => {
    expect(toCsv(['HP'], [[14281]])).toBe('HP\r\n14281')
  })

  it('escapa separador, aspas e quebras de linha; vazio para nulo', () => {
    expect(toCsv(['A', 'B', 'C'], [['x;y', 'diz "oi"', null]])).toBe('A;B;C\r\n"x;y";"diz ""oi""";')
  })
})
