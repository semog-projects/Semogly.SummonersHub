import { Route, Routes } from 'react-router'
import { HomePage } from '@/pages/HomePage'

export function App() {
  return (
    <div className="min-h-svh bg-background text-foreground">
      <header className="border-b">
        <div className="mx-auto flex h-14 max-w-6xl items-center px-4">
          <span className="font-heading text-lg font-semibold">SummonersHub</span>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8">
        <Routes>
          <Route index element={<HomePage />} />
        </Routes>
      </main>
    </div>
  )
}
