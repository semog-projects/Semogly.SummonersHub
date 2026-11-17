import type { ReactNode } from 'react'

/** E-mail público para dúvidas e pedidos de exclusão de dados. */
export const PRIVACY_CONTACT_EMAIL = 'fnd.gomes02@gmail.com'

/** Data da última revisão do texto: atualize sempre que a política mudar. */
const LAST_UPDATED = '16 de novembro de 2026'

/** Política de privacidade (pública). Também é o link exigido pelo Google na tela de consentimento OAuth. */
export function PrivacyPage() {
  return (
    <article className="mx-auto max-w-3xl space-y-8">
      <header className="space-y-1">
        <h1 className="font-heading text-2xl font-semibold">Política de privacidade</h1>
        <p className="text-sm text-muted-foreground">Última atualização: {LAST_UPDATED}</p>
      </header>

      <p className="text-sm leading-relaxed">
        O SummonersHub é um painel não oficial para jogadores de Summoners War: Sky Arena, sem vínculo com a Com2uS.
        Esta página explica quais dados o site guarda, para que servem e como pedir a exclusão deles.
      </p>

      <Section title="Dados que coletamos">
        <ul className="list-disc space-y-2 pl-5">
          <li>
            <strong>Conta no site:</strong> seu e-mail e, se você criar uma senha, o hash dela. A senha em si nunca é
            guardada.
          </li>
          <li>
            <strong>Login com Google:</strong> o e-mail da sua conta Google, a confirmação de que ele foi verificado e o
            identificador da conta Google, usado para reconhecer você nos próximos logins. O Google também envia nome e
            foto do perfil, mas o site não guarda esses dados. O site não tem acesso a Gmail, Drive, contatos nem a
            nenhum outro serviço do Google.
          </li>
          <li>
            <strong>Dados da conta do jogo:</strong> o que você importa do arquivo do SW Exporter: dados do invocador
            (ID, nome e nível), monstros, runas, artefatos e itens de craft. Antes do envio, o próprio navegador
            remove a chave de sessão e os dados do dispositivo que vêm no arquivo; eles nunca chegam ao servidor.
          </li>
          <li>
            <strong>O que você cria no site:</strong> presets do otimizador, marcações e bloqueios de itens, itens
            manuais, builds compartilhadas e preferências (como o tema).
          </li>
        </ul>
      </Section>

      <Section title="Como usamos os dados">
        <p>
          Os dados servem apenas para fazer o site funcionar: identificar você no login e mostrar, analisar e otimizar
          a sua conta do jogo. Não vendemos nem compartilhamos dados com terceiros para publicidade, e não usamos
          ferramentas de rastreamento ou de anúncios.
        </p>
        <p>
          <strong>Builds compartilhadas</strong> são públicas: qualquer pessoa com o link vê o monstro, com as runas e os
          artefatos equipados, e a data do import. Você pode revogar o link quando quiser.
        </p>
      </Section>

      <Section title="Cookies e armazenamento local">
        <ul className="list-disc space-y-2 pl-5">
          <li>
            <strong>summonershub.auth:</strong> cookie de sessão que mantém você conectado por até 14 dias. É essencial
            e não serve para rastreamento.
          </li>
          <li>
            <strong>Tema:</strong> a escolha de tema claro ou escuro fica no armazenamento local do navegador.
          </li>
        </ul>
      </Section>

      <Section title="Onde os dados ficam">
        <p>
          O site é hospedado na <strong>Vercel</strong> e o banco de dados fica no <strong>Neon</strong> (PostgreSQL),
          ambos na região de São Paulo. Esses provedores processam os dados apenas para hospedar o serviço. A Vercel
          pode registrar dados técnicos de acesso, como endereço IP, em logs de operação.
        </p>
      </Section>

      <Section title="Seus direitos e exclusão de dados">
        <p>
          Conforme a LGPD, você pode pedir acesso, correção ou exclusão dos seus dados. Para excluir a conta e tudo que
          está associado a ela (imports, presets, marcações e builds compartilhadas), envie um e-mail para{' '}
          <a className="font-medium underline underline-offset-4" href={`mailto:${PRIVACY_CONTACT_EMAIL}`}>
            {PRIVACY_CONTACT_EMAIL}
          </a>{' '}
          a partir do e-mail cadastrado. A exclusão é feita em até 15 dias.
        </p>
        <p>
          Se você entrou com o Google, também pode remover o acesso do SummonersHub a qualquer momento em{' '}
          <a
            className="font-medium underline underline-offset-4"
            href="https://myaccount.google.com/connections"
            target="_blank"
            rel="noreferrer"
          >
            myaccount.google.com/connections
          </a>
          .
        </p>
      </Section>

      <Section title="Alterações">
        <p>
          Quando esta política mudar, a data no topo da página será atualizada. Dúvidas podem ser enviadas para{' '}
          <a className="font-medium underline underline-offset-4" href={`mailto:${PRIVACY_CONTACT_EMAIL}`}>
            {PRIVACY_CONTACT_EMAIL}
          </a>
          .
        </p>
      </Section>
    </article>
  )
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="space-y-3 text-sm leading-relaxed">
      <h2 className="font-heading text-lg font-semibold">{title}</h2>
      {children}
    </section>
  )
}
