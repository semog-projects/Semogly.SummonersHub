import { buttonVariants } from '@/components/ui/button'
import { EXTERNAL_LOGIN_ERRORS, externalLoginUrl, useExternalProviders } from './useAuth'

/**
 * Login com Google, quando configurado na API. É uma navegação completa: o Google volta para a API, que
 * cria o cookie de login e redireciona para <paramref name="returnUrl"/>.
 * @param error Código do ?erro= devolvido pela API quando o login externo falha.
 */
export function ExternalLogin({ returnUrl, error }: { returnUrl: string; error: string | null }) {
  const { data: providers } = useExternalProviders()

  return (
    <>
      {error && (
        <p role="alert" className="mt-4 text-sm text-destructive">
          {EXTERNAL_LOGIN_ERRORS[error] ?? EXTERNAL_LOGIN_ERRORS.failed}
        </p>
      )}

      {providers?.includes('Google') && (
        <div className="mt-4 space-y-4">
          <div className="flex items-center gap-3 text-xs text-muted-foreground">
            <span className="h-px flex-1 bg-border" />
            ou
            <span className="h-px flex-1 bg-border" />
          </div>
          <a href={externalLoginUrl('Google', returnUrl)} className={buttonVariants({ variant: 'outline', className: 'w-full' })}>
            Entrar com Google
          </a>
        </div>
      )}
    </>
  )
}
