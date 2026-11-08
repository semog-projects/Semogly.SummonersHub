import { Button } from '@/components/ui/button'

/** Botão de "mostrar mais", só quando há itens além do limite. */
export function ShowMoreButton({ total, limit, onClick }: { total: number; limit: number; onClick: () => void }) {
  if (total <= limit) return null

  return (
    <div className="flex justify-center">
      <Button variant="outline" size="sm" onClick={onClick}>
        Mostrar mais ({total - limit} restantes)
      </Button>
    </div>
  )
}
