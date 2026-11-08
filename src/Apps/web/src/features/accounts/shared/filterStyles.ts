import { cn } from '@/lib/utils'

export const controlClass =
  'h-8 rounded-md border bg-transparent px-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/50'

// Fundo explícito: a lista aberta do select é desenhada pelo navegador e não herda o fundo transparente
export const selectClass = cn(controlClass, 'bg-background text-foreground')
export const optionClass = 'bg-popover text-popover-foreground'
