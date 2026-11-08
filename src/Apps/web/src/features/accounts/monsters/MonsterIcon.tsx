import { cn } from '@/lib/utils'

/** Ícone do monstro (SWARFARM). Sem ícone conhecido, mostra um quadro neutro do mesmo tamanho. */
export function MonsterIcon({ imageUrl, className }: { imageUrl?: string | null; className?: string }) {
  return imageUrl ? (
    <img src={imageUrl} alt="" loading="lazy" className={cn('size-8 rounded-md', className)} />
  ) : (
    <span className={cn('block size-8 rounded-md bg-muted', className)} />
  )
}
