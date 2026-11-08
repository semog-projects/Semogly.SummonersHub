import type { ReactNode } from 'react'
import { Link, NavLink, Route, Routes, useLocation } from 'react-router'
import { RequireAuth } from '@/features/auth/RequireAuth'
import { useCurrentUser } from '@/features/auth/useAuth'
import { UserMenu } from '@/features/auth/UserMenu'
import { cn } from '@/lib/utils'
import { AccountLayout } from '@/pages/AccountLayout'
import { AccountOverviewPage } from '@/pages/AccountOverviewPage'
import { ArtifactListPage } from '@/pages/ArtifactListPage'
import { HomePage } from '@/pages/HomePage'
import { ImportPage } from '@/pages/ImportPage'
import { InventoryPage } from '@/pages/InventoryPage'
import { LoginPage } from '@/pages/LoginPage'
import { MonsterDetailPage } from '@/pages/MonsterDetailPage'
import { MonsterListPage } from '@/pages/MonsterListPage'
import { RuneListPage } from '@/pages/RuneListPage'

export function App() {
  return (
    <div className="min-h-svh bg-background text-foreground">
      <header className="border-b">
        <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-4 px-4">
          <div className="flex items-center gap-6">
            <Link to="/" className="font-heading text-lg font-semibold">
              SummonersHub
            </Link>
            <MainNav />
          </div>
          <UserMenu />
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8">
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route index element={<Protected><HomePage /></Protected>} />
          <Route path="/contas/:wizardId" element={<Protected><AccountLayout /></Protected>}>
            <Route index element={<AccountOverviewPage />} />
            <Route path="monstros" element={<MonsterListPage />} />
            <Route path="monstros/:monsterId" element={<MonsterDetailPage />} />
            <Route path="runas" element={<RuneListPage />} />
            <Route path="artefatos" element={<ArtifactListPage />} />
            <Route path="inventario" element={<InventoryPage />} />
          </Route>
          <Route path="/importar" element={<Protected><ImportPage /></Protected>} />
        </Routes>
      </main>
    </div>
  )
}

function Protected({ children }: { children: ReactNode }) {
  return <RequireAuth>{children}</RequireAuth>
}

function MainNav() {
  const { pathname } = useLocation()
  const { data: user } = useCurrentUser()
  if (!user) return null

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    cn('text-sm text-muted-foreground transition-colors hover:text-foreground', isActive && 'text-foreground')

  return (
    <nav aria-label="Principal" className="flex items-center gap-4">
      {/* "Painel" fica ativo em qualquer conta (/contas/...) */}
      <NavLink to="/" end className={({ isActive }) => linkClass({ isActive: isActive || pathname.startsWith('/contas/') })}>
        Painel
      </NavLink>
      <NavLink to="/importar" className={linkClass}>
        Importar
      </NavLink>
    </nav>
  )
}
