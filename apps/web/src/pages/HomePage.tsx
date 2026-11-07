import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ApiStatusBadge } from '@/features/system/ApiStatusBadge'

export function HomePage() {
  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="font-heading text-2xl font-semibold">Sua conta de Summoners War</h1>
        <ApiStatusBadge />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Importar dados</CardTitle>
          <CardDescription>
            Exporte sua conta com o SW Exporter e envie o arquivo JSON aqui. Os dados sensíveis (chaves de sessão e
            token) são removidos no seu navegador antes do envio.
          </CardDescription>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">O upload chega na próxima etapa.</CardContent>
      </Card>
    </div>
  )
}
