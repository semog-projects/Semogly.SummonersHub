import { useId, useState, type FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useLogin, useRegister } from './useAuth'

type Mode = 'login' | 'register'

const inputClass =
  'h-9 w-full rounded-md border bg-transparent px-3 text-sm outline-none focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/50'

/** Formulário de login e cadastro (alterna entre os dois modos). */
export function AuthForm({ onSuccess }: { onSuccess?: () => void }) {
  const emailId = useId()
  const passwordId = useId()
  const [mode, setMode] = useState<Mode>('login')
  const login = useLogin()
  const register = useRegister()
  const mutation = mode === 'login' ? login : register

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const credentials = { email: String(form.get('email')).trim(), password: String(form.get('password')) }
    mutation.mutate(credentials, { onSuccess })
  }

  function switchMode(next: Mode) {
    setMode(next)
    login.reset()
    register.reset()
  }

  return (
    <Card className="mx-auto w-full max-w-sm">
      <CardHeader>
        <CardTitle>{mode === 'login' ? 'Entrar' : 'Criar conta'}</CardTitle>
        <CardDescription>
          {mode === 'login'
            ? 'Entre para importar e acompanhar suas contas do jogo.'
            : 'A senha precisa de 8 caracteres, com letra maiúscula, minúscula e número.'}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={handleSubmit}>
          <div className="space-y-1.5">
            <label htmlFor={emailId} className="text-sm font-medium">
              E-mail
            </label>
            <input id={emailId} name="email" type="email" autoComplete="email" required className={inputClass} />
          </div>
          <div className="space-y-1.5">
            <label htmlFor={passwordId} className="text-sm font-medium">
              Senha
            </label>
            <input
              id={passwordId}
              name="password"
              type="password"
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
              minLength={mode === 'register' ? 8 : undefined}
              required
              className={inputClass}
            />
          </div>

          {mutation.error && (
            <p role="alert" className="text-sm text-destructive">
              {mutation.error.message}
            </p>
          )}

          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mode === 'login' ? 'Entrar' : 'Criar conta'}
          </Button>
        </form>

        <p className="mt-4 text-center text-sm text-muted-foreground">
          {mode === 'login' ? 'Ainda não tem conta? ' : 'Já tem conta? '}
          <button
            type="button"
            className="font-medium text-foreground underline-offset-4 hover:underline"
            onClick={() => switchMode(mode === 'login' ? 'register' : 'login')}
          >
            {mode === 'login' ? 'Criar conta' : 'Entrar'}
          </button>
        </p>
      </CardContent>
    </Card>
  )
}
