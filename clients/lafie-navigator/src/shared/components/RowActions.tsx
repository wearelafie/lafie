import { useState, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle,
  Menu, MenuDivider, MenuItem, MenuList, MenuPopover, MenuTrigger, Tooltip, makeStyles, tokens,
} from '@fluentui/react-components'
import { Delete20Regular, MoreHorizontal20Regular } from '@fluentui/react-icons'

export type RowAction = {
  key: string
  label: string
  icon: ReactElement
  onClick: () => void
  /** Couleur de l'icône (token ou hex). */
  color?: string
}

const useStyles = makeStyles({
  row: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXXS },
  danger: { color: tokens.colorPaletteRedForeground1 },
  error: { color: tokens.colorPaletteRedForeground1 },
})

/**
 * Actions d'une ligne de liste : icônes primaires colorées (avec tooltips)
 * + menu overflow « ⋯ » pour les actions secondaires et la suppression (confirmée).
 */
export function RowActions({
  primary,
  menu = [],
  onDelete,
  deleteLabel,
  confirmTitle,
  confirmMessage,
}: {
  primary: RowAction[]
  menu?: RowAction[]
  onDelete?: () => Promise<void> | void
  deleteLabel?: string
  confirmTitle?: string
  confirmMessage?: string
}) {
  const styles = useStyles()
  const { t } = useTranslation()
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const hasOverflow = menu.length > 0 || !!onDelete

  const runDelete = async () => {
    if (!onDelete) return
    setBusy(true)
    setError(null)
    try {
      await onDelete()
      setConfirmOpen(false)
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className={styles.row} onClick={(e) => e.stopPropagation()}>
      {primary.map((a) => (
        <Tooltip key={a.key} content={a.label} relationship="label">
          <Button size="small" appearance="subtle" aria-label={a.label} onClick={a.onClick}
            icon={<span style={{ color: a.color, display: 'flex' }}>{a.icon}</span>} />
        </Tooltip>
      ))}

      {hasOverflow && (
        <Menu>
          <MenuTrigger disableButtonEnhancement>
            <Tooltip content={t('actions.more')} relationship="label">
              <Button size="small" appearance="subtle" icon={<MoreHorizontal20Regular />} aria-label={t('actions.more')} />
            </Tooltip>
          </MenuTrigger>
          <MenuPopover>
            <MenuList>
              {menu.map((a) => (
                <MenuItem key={a.key} icon={<span style={{ color: a.color, display: 'flex' }}>{a.icon}</span>} onClick={a.onClick}>
                  {a.label}
                </MenuItem>
              ))}
              {onDelete && (
                <>
                  {menu.length > 0 && <MenuDivider />}
                  <MenuItem className={styles.danger} icon={<Delete20Regular className={styles.danger} />}
                    onClick={() => { setError(null); setConfirmOpen(true) }}>
                    {deleteLabel ?? t('actions.delete')}
                  </MenuItem>
                </>
              )}
            </MenuList>
          </MenuPopover>
        </Menu>
      )}

      <Dialog open={confirmOpen} onOpenChange={(_, d) => setConfirmOpen(d.open)} modalType="alert">
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{confirmTitle ?? t('actions.confirmDeleteTitle')}</DialogTitle>
            <DialogContent>
              {confirmMessage ?? t('actions.confirmDeleteMessage')}
              {error && <div className={styles.error} style={{ marginTop: 8 }}>{error}</div>}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setConfirmOpen(false)}>{t('actions.cancel')}</Button>
              <Button appearance="primary" disabled={busy} onClick={runDelete}>
                {busy ? t('actions.deleting') : (deleteLabel ?? t('actions.delete'))}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  )
}
