import { render, screen } from '@testing-library/react'
import { PRIVACY_CONTACT_EMAIL, PrivacyPage } from './PrivacyPage'

describe('PrivacyPage', () => {
  it('mostra a política com o contato para exclusão de dados', () => {
    render(<PrivacyPage />)

    expect(screen.getByRole('heading', { level: 1, name: 'Política de privacidade' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Seus direitos e exclusão de dados' })).toBeInTheDocument()

    const contacts = screen.getAllByRole('link', { name: PRIVACY_CONTACT_EMAIL })
    expect(contacts[0]).toHaveAttribute('href', `mailto:${PRIVACY_CONTACT_EMAIL}`)
  })
})
