import { useState } from 'react'

/**
 * Paginação incremental ("mostrar mais") para listas longas filtradas no front. Quando a chave dos
 * filtros muda, volta para a primeira página.
 */
export function usePaging(filtersKey: string, pageSize: number) {
  const [limit, setLimit] = useState(pageSize)
  const [lastKey, setLastKey] = useState(filtersKey)

  // Ajuste durante a renderização (padrão do React para estado derivado de props)
  if (filtersKey !== lastKey) {
    setLastKey(filtersKey)
    setLimit(pageSize)
  }

  return { limit, showMore: () => setLimit(limit + pageSize) }
}
