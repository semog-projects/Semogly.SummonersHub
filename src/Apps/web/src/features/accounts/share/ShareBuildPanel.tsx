import { Check, Copy, Share2 } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatDateTime } from '@/lib/format'
import { shareUrl, useCreateShareLink, useRevokeShareLink, useShareLinks } from './useShareLinks'

/** Links públicos da build do monstro: criar (versão atual do import), copiar e revogar. */
export function ShareBuildPanel({ wizardId, monsterId }: { wizardId: number; monsterId: number }) {
  const links = useShareLinks(wizardId, monsterId)
  const create = useCreateShareLink(wizardId, monsterId)
  const revoke = useRevokeShareLink(wizardId, monsterId)
  const [copied, setCopied] = useState<string | null>(null)

  async function copy(token: string) {
    try {
      await navigator.clipboard.writeText(shareUrl(token))
      setCopied(token)
    } catch {
      // Sem permissão de área de transferência: o endereço continua visível para copiar à mão
      setCopied(null)
    }
  }

  return (
    <Card aria-label="Compartilhar build">
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center justify-between gap-2">
          <span>Compartilhar build</span>
          <Button
            variant="outline"
            size="sm"
            disabled={create.isPending}
            onClick={() => create.mutate(undefined, { onSuccess: (link) => void copy(link.token) })}
          >
            <Share2 aria-hidden /> Gerar link
          </Button>
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-2 text-sm">
        <p className="text-xs text-muted-foreground">
          Quem tiver o link vê atributos, runas e artefatos deste monstro na versão do import em que o link foi gerado,
          sem login e sem o nome do invocador.
        </p>
        {(links.data ?? []).length === 0 ? (
          <p className="text-xs text-muted-foreground">Nenhum link gerado.</p>
        ) : (
          <ul className="space-y-2" aria-label="Links gerados">
            {links.data!.map((link) => (
              <li key={link.token} className="flex flex-wrap items-center gap-2">
                <code className="rounded bg-muted px-2 py-1 text-xs break-all">{shareUrl(link.token)}</code>
                <span className="text-xs text-muted-foreground">
                  versão {link.version} · {formatDateTime(link.createdAt)}
                </span>
                <Button variant="ghost" size="sm" onClick={() => void copy(link.token)} aria-label={`Copiar link da versão ${link.version}`}>
                  {copied === link.token ? <Check aria-hidden /> : <Copy aria-hidden />}
                  {copied === link.token ? 'Copiado' : 'Copiar'}
                </Button>
                <Button variant="ghost" size="sm" disabled={revoke.isPending} onClick={() => revoke.mutate(link.token)}>
                  Revogar
                </Button>
              </li>
            ))}
          </ul>
        )}
        {(create.error ?? revoke.error) && (
          <p role="alert" className="text-xs text-destructive">
            {(create.error ?? revoke.error)!.message}
          </p>
        )}
      </CardContent>
    </Card>
  )
}
