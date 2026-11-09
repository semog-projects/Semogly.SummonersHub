import { Plus, Trash2 } from 'lucide-react'
import { useState, type FormEvent, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { selectClass, optionClass } from '@/features/accounts/shared/filterStyles'
import { mainStatLabel } from '@/features/accounts/optimizer/optimizerOptions'
import { ManualBadge } from '@/features/accounts/manual/ManualBadge'
import { CRAFT_SETS, CRAFT_TYPE_OPTIONS, statsFor } from '@/features/accounts/manual/manualOptions'
import {
  useCreateManualCraftItem,
  useCreateManualMonster,
  useDeleteManualItem,
  useManualItems,
  useMonsterSearch,
  type ManualCraftType,
} from '@/features/accounts/manual/useManualItems'
import { ELEMENT_TEXT_CLASSES } from '@/features/accounts/overview/elementStyles'
import { AWAKEN_LABELS, ELEMENT_NAME_LABELS, RARITY_LABELS } from '@/lib/game/labels'
import { cn } from '@/lib/utils'

/** Monstros e gems/grinds criados à mão para planejar; somados à conta e mantidos entre imports. */
export function ManualItemsPage() {
  const wizardId = Number(useParams().wizardId)

  return (
    <div className="space-y-6">
      <p className="text-sm text-muted-foreground">
        Itens criados à mão entram na conta junto do último import (monstros, otimizador, dano, speed tune, inventário e
        sugestões de gems e grinds) e continuam depois de novos imports. Aparecem com a marca <ManualBadge />.
      </p>
      <ManualMonstersCard wizardId={wizardId} />
      <ManualCraftItemsCard wizardId={wizardId} />
    </div>
  )
}

function ManualMonstersCard({ wizardId }: { wizardId: number }) {
  const [search, setSearch] = useState('')
  const items = useManualItems(wizardId)
  const results = useMonsterSearch(search)
  const create = useCreateManualMonster(wizardId)
  const remove = useDeleteManualItem(wizardId)
  const monsters = items.data?.monsters ?? []

  return (
    <Card>
      <CardHeader>
        <CardTitle>Monstros</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4 text-sm">
        <p className="text-xs text-muted-foreground">
          Criados em 6★ nível 40, com os atributos base do bestiário e skills no máximo, sem runas. Não podem ser
          compartilhados por link.
        </p>
        <div className="space-y-2">
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Buscar monstro pelo nome (ex.: Lapis)"
            aria-label="Buscar monstro"
            className="w-full max-w-sm rounded-md border bg-transparent px-3 py-1.5"
          />
          {search.trim().length >= 2 && (
            <ul aria-label="Resultados da busca" className="max-w-xl space-y-1">
              {results.data?.length === 0 && <li className="text-xs text-muted-foreground">Nenhum monstro encontrado.</li>}
              {results.data?.map((monster) => (
                <li key={monster.com2usId} className="flex items-center gap-3">
                  {monster.imageUrl && <img src={monster.imageUrl} alt="" className="size-8 rounded" loading="lazy" />}
                  <span className={cn('font-medium', ELEMENT_TEXT_CLASSES[monster.element])}>{monster.name}</span>
                  <span className="text-xs text-muted-foreground">
                    {ELEMENT_NAME_LABELS[monster.element]} · {AWAKEN_LABELS[monster.awakenLevel] ?? ''} · nat {monster.naturalStars}★
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    className="ml-auto"
                    disabled={create.isPending}
                    onClick={() => create.mutate(monster.com2usId)}
                    aria-label={`Adicionar ${monster.name} (${ELEMENT_NAME_LABELS[monster.element]}, ${AWAKEN_LABELS[monster.awakenLevel] ?? ''})`}
                  >
                    <Plus aria-hidden /> Adicionar
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>

        {monsters.length === 0 ? (
          <p className="text-xs text-muted-foreground">Nenhum monstro manual.</p>
        ) : (
          <ul aria-label="Monstros manuais" className="space-y-1">
            {monsters.map((monster) => (
              <li key={monster.gameId} className="flex items-center gap-3">
                <Link
                  to={`/contas/${wizardId}/monstros/${monster.gameId}`}
                  className={cn('font-medium hover:underline', monster.element && ELEMENT_TEXT_CLASSES[monster.element])}
                >
                  {monster.name ?? `Monstro ${monster.masterId}`}
                </Link>
                <span className="text-xs text-muted-foreground">6★ nível 40</span>
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={remove.isPending}
                  onClick={() => remove.mutate(monster.gameId)}
                  aria-label={`Excluir ${monster.name ?? 'monstro'}`}
                >
                  <Trash2 aria-hidden />
                </Button>
              </li>
            ))}
          </ul>
        )}
        <ErrorMessage error={create.error ?? remove.error} />
      </CardContent>
    </Card>
  )
}

function ManualCraftItemsCard({ wizardId }: { wizardId: number }) {
  const items = useManualItems(wizardId)
  const create = useCreateManualCraftItem(wizardId)
  const remove = useDeleteManualItem(wizardId)
  const [type, setType] = useState<ManualCraftType>('EnchantedGem')
  const [set, setSet] = useState(CRAFT_SETS[0])
  const [stat, setStat] = useState(8)
  const [rarity, setRarity] = useState(5)
  const [amount, setAmount] = useState('1')
  const craftItems = items.data?.craftItems ?? []

  // Grind não tem Taxa/Dano Crít., RES nem PRE: troca para VEL ao mudar o tipo
  function changeType(next: ManualCraftType) {
    setType(next)
    if (!statsFor(next).includes(stat)) setStat(8)
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    create.mutate({ type, set, stat, rarity, amount: Number(amount) })
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Gems e grinds</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4 text-sm">
        <form onSubmit={submit} className="flex flex-wrap items-end gap-2" aria-label="Nova gem ou grind">
          <Field label="Tipo">
            <select aria-label="Tipo" value={type} onChange={(e) => changeType(e.target.value as ManualCraftType)} className={selectClass}>
              {CRAFT_TYPE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value} className={optionClass}>
                  {option.label}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Set">
            <select aria-label="Set" value={set} onChange={(e) => setSet(e.target.value)} className={selectClass}>
              {CRAFT_SETS.map((name) => (
                <option key={name} value={name} className={optionClass}>
                  {name}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Atributo">
            <select aria-label="Atributo" value={stat} onChange={(e) => setStat(Number(e.target.value))} className={selectClass}>
              {statsFor(type).map((code) => (
                <option key={code} value={code} className={optionClass}>
                  {mainStatLabel(code)}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Raridade">
            <select aria-label="Raridade" value={rarity} onChange={(e) => setRarity(Number(e.target.value))} className={selectClass}>
              {[5, 4, 3, 2, 1].map((value) => (
                <option key={value} value={value} className={optionClass}>
                  {RARITY_LABELS[value]}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Quantidade">
            <input
              type="number"
              aria-label="Quantidade"
              min={1}
              max={999}
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className="w-20 rounded-md border bg-transparent px-2 py-1"
            />
          </Field>
          <Button type="submit" disabled={create.isPending || !(Number(amount) >= 1)}>
            <Plus aria-hidden /> Adicionar
          </Button>
        </form>

        {craftItems.length === 0 ? (
          <p className="text-xs text-muted-foreground">Nenhuma gem ou grind manual.</p>
        ) : (
          <ul aria-label="Gems e grinds manuais" className="space-y-1">
            {craftItems.map((item) => (
              <li key={item.gameId} className="flex items-center gap-3">
                <span>
                  {item.type === 'Grindstone' ? 'Grind' : 'Gem'} {item.set} · {mainStatLabel(item.stat)} · {RARITY_LABELS[item.rarity]}
                </span>
                <span className="text-xs text-muted-foreground">×{item.amount}</span>
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={remove.isPending}
                  onClick={() => remove.mutate(item.gameId)}
                  aria-label={`Excluir ${item.type === 'Grindstone' ? 'grind' : 'gem'} ${item.set} ${mainStatLabel(item.stat)}`}
                >
                  <Trash2 aria-hidden />
                </Button>
              </li>
            ))}
          </ul>
        )}
        <ErrorMessage error={create.error ?? remove.error} />
      </CardContent>
    </Card>
  )
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="space-y-1">
      <span className="block text-xs text-muted-foreground" aria-hidden>
        {label}
      </span>
      {children}
    </div>
  )
}

function ErrorMessage({ error }: { error: Error | null }) {
  if (!error) return null
  return (
    <p role="alert" className="text-xs text-destructive">
      {error.message}
    </p>
  )
}
