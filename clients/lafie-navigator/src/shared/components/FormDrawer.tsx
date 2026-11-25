import type { FormEvent, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Button, DrawerBody, DrawerHeader, DrawerHeaderTitle, OverlayDrawer,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { Dismiss24Regular } from '@fluentui/react-icons'

const useStyles = makeStyles({
  // Le corps occupe toute la hauteur pour coller le pied de page en bas.
  body: { display: 'flex', flexDirection: 'column', height: '100%', paddingBottom: 0 },
  form: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, flex: 1, minHeight: 0 },
  content: { display: 'flex', flexDirection: 'column', rowGap: tokens.spacingVerticalM, flex: 1, overflowY: 'auto' },
  footer: {
    display: 'flex', justifyContent: 'flex-end', columnGap: tokens.spacingHorizontalM,
    paddingTop: tokens.spacingVerticalM, paddingBottom: tokens.spacingVerticalM, marginTop: 'auto',
    position: 'sticky', bottom: 0, backgroundColor: tokens.colorNeutralBackground1,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
})

/**
 * Tiroir (Drawer) latéral pour les formulaires de création / édition — remplace les modals de
 * formulaire. En-tête avec titre + bouton fermer, corps défilant, pied de page collant pour les
 * actions. Si `onSubmit` est fourni, le contenu est enveloppé dans un `<form>` (Entrée = soumettre) ;
 * le bouton principal du pied doit alors être `type="submit"`.
 */
export function FormDrawer({
  open, onOpenChange, title, children, footer, onSubmit, size = 'medium',
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: ReactNode
  children: ReactNode
  footer: ReactNode
  onSubmit?: () => void
  size?: 'small' | 'medium' | 'large' | 'full'
}) {
  const styles = useStyles()
  const { t } = useTranslation()

  const inner = (
    <>
      <div className={styles.content}>{children}</div>
      <div className={styles.footer}>{footer}</div>
    </>
  )

  return (
    <OverlayDrawer open={open} position="end" size={size} onOpenChange={(_, d) => onOpenChange(d.open)}>
      <DrawerHeader>
        <DrawerHeaderTitle
          action={<Button appearance="subtle" icon={<Dismiss24Regular />} aria-label={t('actions.close')} onClick={() => onOpenChange(false)} />}>
          {title}
        </DrawerHeaderTitle>
      </DrawerHeader>
      <DrawerBody className={styles.body}>
        {onSubmit
          ? <form className={styles.form} onSubmit={(e: FormEvent) => { e.preventDefault(); onSubmit() }}>{inner}</form>
          : <div className={styles.form}>{inner}</div>}
      </DrawerBody>
    </OverlayDrawer>
  )
}
