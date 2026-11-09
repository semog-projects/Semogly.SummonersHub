import { Download } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { downloadCsv } from '@/lib/csv'

/** Exporta a lista já filtrada (todas as linhas, não só a página visível). */
export function ExportCsvButton({ fileName, build }: { fileName: string; build: () => string }) {
  return (
    <Button variant="outline" size="sm" onClick={() => downloadCsv(fileName, build())}>
      <Download aria-hidden /> Exportar CSV
    </Button>
  )
}
