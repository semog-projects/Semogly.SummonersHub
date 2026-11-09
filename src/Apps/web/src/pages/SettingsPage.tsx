import { Download, Monitor, Moon, Sun, Upload } from 'lucide-react'
import { useRef, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { usePreferences, useExportSettings, useRestoreSettings, useUpdatePreferences } from '@/features/settings/useSettings'
import { storedTheme, THEME_OPTIONS, type ThemePreference } from '@/lib/theme'
import { cn } from '@/lib/utils'

const THEME_ICONS: Record<ThemePreference, typeof Sun> = { Light: Sun, Dark: Moon, System: Monitor }

/** Tema e backup das configurações do usuário. */
export function SettingsPage() {
  return (
    <div className="space-y-6">
      <h1 className="font-heading text-2xl font-semibold">Configurações</h1>
      <ThemeCard />
      <BackupCard />
    </div>
  )
}

function ThemeCard() {
  const { data: preferences } = usePreferences(true)
  const update = useUpdatePreferences()
  const theme = preferences?.theme ?? storedTheme()

  return (
    <Card>
      <CardHeader>
        <CardTitle>Tema</CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        <div role="radiogroup" aria-label="Tema" className="flex flex-wrap gap-2">
          {THEME_OPTIONS.map(({ value, label }) => {
            const Icon = THEME_ICONS[value]
            const selected = theme === value
            return (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={selected}
                onClick={() => update.mutate({ theme: value })}
                className={cn(
                  'inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm transition-colors hover:bg-muted',
                  selected && 'border-primary bg-muted font-medium',
                )}
              >
                <Icon className="size-4" aria-hidden />
                {label}
              </button>
            )
          })}
        </div>
        <p className="text-xs text-muted-foreground">Salvo na sua conta: vale em qualquer dispositivo.</p>
        {update.error && (
          <p role="alert" className="text-xs text-destructive">
            {update.error.message}
          </p>
        )}
      </CardContent>
    </Card>
  )
}

function BackupCard() {
  const exportSettings = useExportSettings()
  const restore = useRestoreSettings()
  const fileInput = useRef<HTMLInputElement>(null)
  const [fileError, setFileError] = useState<string | null>(null)

  function download() {
    exportSettings.mutate(undefined, {
      onSuccess: (backup) => {
        const blob = new Blob([JSON.stringify(backup, null, 2)], { type: 'application/json' })
        const url = URL.createObjectURL(blob)
        const link = document.createElement('a')
        link.href = url
        link.download = `summonershub-configuracoes-${backup.exportedAt.slice(0, 10)}.json`
        link.click()
        URL.revokeObjectURL(url)
      },
    })
  }

  async function upload(file: File) {
    setFileError(null)
    restore.reset()
    let backup: unknown
    try {
      backup = JSON.parse(await file.text())
    } catch {
      setFileError('O arquivo não é um JSON válido.')
      return
    }
    restore.mutate(backup)
  }

  const result = restore.data
  const error = fileError ?? exportSettings.error?.message ?? restore.error?.message

  return (
    <Card>
      <CardHeader>
        <CardTitle>Backup das configurações</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3 text-sm">
        <p className="text-muted-foreground">
          Guarda num arquivo o tema, os presets do otimizador e os bloqueios e tags de runas, artefatos e monstros. Não inclui
          os dados do jogo: para eles, importe de novo o JSON do SW Exporter.
        </p>
        <p className="text-muted-foreground">
          Ao restaurar, os presets substituem os de mesmo nome e as marcações substituem as do mesmo item. Marcações de
          contas que não são suas são ignoradas.
        </p>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={download} disabled={exportSettings.isPending}>
            <Download aria-hidden /> Baixar backup
          </Button>
          <Button variant="outline" onClick={() => fileInput.current?.click()} disabled={restore.isPending}>
            <Upload aria-hidden /> {restore.isPending ? 'Restaurando…' : 'Restaurar backup'}
          </Button>
          <input
            ref={fileInput}
            type="file"
            accept="application/json,.json"
            className="sr-only"
            aria-label="Arquivo de backup"
            onChange={(event) => {
              const file = event.target.files?.[0]
              event.target.value = ''
              if (file) void upload(file)
            }}
          />
        </div>
        {result && (
          <p role="status">
            Restaurados {result.presets} preset(s) e {result.marks} marcação(ões).
            {result.skippedPresets > 0 && ` ${result.skippedPresets} preset(s) ignorado(s) pelo limite.`}
            {result.skippedMarks > 0 && ` ${result.skippedMarks} marcação(ões) de outras contas ignorada(s).`}
          </p>
        )}
        {error && (
          <p role="alert" className="text-destructive">
            {error}
          </p>
        )}
      </CardContent>
    </Card>
  )
}
