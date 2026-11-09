import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getJson, postJson, request } from '@/lib/api'
import { applyTheme, type ThemePreference } from '@/lib/theme'

export interface Preferences {
  theme: ThemePreference
}

/** Backup das configurações (GET/POST /api/settings/backup); o conteúdo é tratado como opaco no front. */
export interface SettingsBackup {
  format: string
  version: number
  exportedAt: string
  [key: string]: unknown
}

export interface RestoreSettingsResult {
  presets: number
  marks: number
  skippedPresets: number
  skippedMarks: number
}

const preferencesKey = ['settings', 'preferences']

export function usePreferences(enabled: boolean) {
  return useQuery({
    queryKey: preferencesKey,
    queryFn: ({ signal }) => getJson<Preferences>('/settings/preferences', signal),
    enabled,
  })
}

/** Salva o tema; aplica na hora, sem esperar o servidor. */
export function useUpdatePreferences() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (preferences: Preferences) =>
      request<Preferences>('/settings/preferences', {
        method: 'PUT',
        body: JSON.stringify(preferences),
        headers: { 'Content-Type': 'application/json' },
      }),
    onMutate: (preferences) => {
      applyTheme(preferences.theme)
      queryClient.setQueryData(preferencesKey, preferences)
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: preferencesKey }),
  })
}

export function useExportSettings() {
  return useMutation({ mutationFn: () => getJson<SettingsBackup>('/settings/backup') })
}

/** Restaura o backup e recarrega tudo (presets, marcações e tema mudam). */
export function useRestoreSettings() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (backup: unknown) => postJson<RestoreSettingsResult>('/settings/backup', backup),
    onSuccess: () => queryClient.invalidateQueries(),
  })
}
