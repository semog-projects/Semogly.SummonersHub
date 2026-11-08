import { useId, useState, type DragEvent } from 'react'
import { Link } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import { ExportParseError, MAX_EXPORT_SIZE_BYTES, parseExportText, SENSITIVE_FIELDS } from './parseExport'
import type { AccountImport } from './types'
import { useImportAccount } from './useImportAccount'

type ImportState =
  | { status: 'idle' }
  | { status: 'reading' }
  | { status: 'ready'; fileName: string; data: AccountImport; readAt: number }
  | { status: 'error'; message: string }

export function ImportCard() {
  const inputId = useId()
  const [state, setState] = useState<ImportState>({ status: 'idle' })
  const [isDragging, setIsDragging] = useState(false)

  async function handleFile(file: File | undefined) {
    if (!file) return

    if (file.size > MAX_EXPORT_SIZE_BYTES) {
      setState({ status: 'error', message: 'Arquivo grande demais para um export do SW Exporter.' })
      return
    }

    setState({ status: 'reading' })
    try {
      // Todo o processamento acontece no navegador: o arquivo original nunca é enviado
      const data = parseExportText(await file.text())
      setState({ status: 'ready', fileName: file.name, data, readAt: Date.now() })
    } catch (error) {
      const message = error instanceof ExportParseError ? error.message : 'Não foi possível ler o arquivo.'
      setState({ status: 'error', message })
    }
  }

  function handleDrop(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
    setIsDragging(false)
    void handleFile(event.dataTransfer.files[0])
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Importar dados</CardTitle>
        <CardDescription>
          Exporte sua conta com o SW Exporter e selecione o arquivo JSON. Chaves de sessão e token são removidos no seu
          navegador: só monstros, runas, artefatos e itens de craft saem dele.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <label
          htmlFor={inputId}
          onDragOver={(event) => {
            event.preventDefault()
            setIsDragging(true)
          }}
          onDragLeave={() => setIsDragging(false)}
          onDrop={handleDrop}
          className={cn(
            'flex cursor-pointer flex-col items-center justify-center gap-1 rounded-lg border border-dashed p-8 text-center text-sm transition-colors hover:bg-muted/50',
            isDragging && 'border-primary bg-muted/50',
          )}
        >
          <span className="font-medium">Arraste o arquivo aqui ou clique para selecionar</span>
          <span className="text-muted-foreground">Arquivo .json gerado pelo SW Exporter</span>
          <input
            id={inputId}
            type="file"
            accept=".json,application/json"
            className="sr-only"
            onChange={(event) => {
              void handleFile(event.target.files?.[0])
              // Permite selecionar o mesmo arquivo de novo
              event.target.value = ''
            }}
          />
        </label>

        {state.status === 'reading' && <p className="text-sm text-muted-foreground">Lendo arquivo…</p>}

        {state.status === 'error' && (
          <p role="alert" className="text-sm text-destructive">
            {state.message}
          </p>
        )}

        {state.status === 'ready' && (
          // key: cada arquivo lido começa sem o resultado do envio anterior
          <ImportSummary key={state.readAt} fileName={state.fileName} data={state.data} />
        )}
      </CardContent>
    </Card>
  )
}

function ImportSummary({ fileName, data }: { fileName: string; data: AccountImport }) {
  const importAccount = useImportAccount()
  const equippedRunes = data.runes.filter((rune) => rune.equippedMonsterId !== null).length

  const items = [
    { label: 'Monstros', value: data.monsters.length },
    { label: 'Runas', value: data.runes.length, detail: `${equippedRunes} equipadas` },
    { label: 'Artefatos', value: data.artifacts.length },
    { label: 'Itens de craft', value: data.craftItems.length },
  ]

  return (
    <section aria-label="Resumo do export" className="space-y-3 rounded-lg border p-4">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <div>
          <p className="font-medium">
            {data.wizard.name} <span className="text-muted-foreground">· nível {data.wizard.level}</span>
          </p>
          <p className="text-xs text-muted-foreground">{fileName}</p>
        </div>
        <Badge variant="secondary">Dados sensíveis removidos</Badge>
      </div>

      <dl className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {items.map((item) => (
          <div key={item.label} className="rounded-md bg-muted/50 p-3">
            <dt className="text-xs text-muted-foreground">{item.label}</dt>
            <dd className="text-xl font-semibold tabular-nums">{item.value}</dd>
            {item.detail && <dd className="text-xs text-muted-foreground">{item.detail}</dd>}
          </div>
        ))}
      </dl>

      <p className="text-xs text-muted-foreground">
        Removidos: {SENSITIVE_FIELDS.join(', ')} e todos os demais campos que o painel não usa.
      </p>

      <div className="flex flex-wrap items-center gap-3">
        <Button onClick={() => importAccount.mutate(data)} disabled={importAccount.isPending || importAccount.isSuccess}>
          {importAccount.isPending ? 'Enviando…' : 'Enviar'}
        </Button>

        {importAccount.isSuccess && (
          <p role="status" className="text-sm">
            {importAccount.data.created
              ? `Conta importada (versão ${importAccount.data.version}).`
              : `Nada mudou desde a versão ${importAccount.data.version}: nenhum import novo foi criado.`}{' '}
            <Link to={`/contas/${importAccount.data.wizardId}`} className="font-medium underline underline-offset-4">
              Ver painel
            </Link>
          </p>
        )}

        {importAccount.isError && (
          <p role="alert" className="text-sm text-destructive">
            Não foi possível enviar: {importAccount.error.message}
          </p>
        )}
      </div>
    </section>
  )
}
