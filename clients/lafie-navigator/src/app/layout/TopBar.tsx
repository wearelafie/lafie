import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from '@tanstack/react-router'
import { useQueryClient } from '@tanstack/react-query'
import {
  Avatar,
  Button,
  CounterBadge,
  Divider,
  Menu,
  MenuDivider,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  SearchBox,
  Text,
  Tooltip,
  makeStyles,
  mergeClasses,
  tokens,
} from '@fluentui/react-components'
import {
  Alert24Regular,
  Apps24Regular,
  Building20Regular,
  Checkmark16Regular,
  Globe20Regular,
  Info20Regular,
  Navigation24Regular,
  Person20Regular,
  QuestionCircle20Regular,
  Settings20Regular,
  ShieldKeyhole20Regular,
  SignOut20Regular,
} from '@fluentui/react-icons'
import { SettingsDialog } from '@features/settings'
import { useConnectionStatus } from '@shared/hooks'
import { clearToken, selectEstablishment, setToken, useCurrentUser } from '@features/auth'
import { AppLauncherDialog } from './AppLauncherDialog'

const useStyles = makeStyles({
  root: {
    display: 'flex',
    alignItems: 'center',
    columnGap: tokens.spacingHorizontalS,
    height: '48px',
    padding: `0 ${tokens.spacingHorizontalM}`,
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  logoBtn: {
    padding: 0,
    minWidth: 0,
    border: 'none',
    background: 'transparent',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    borderRadius: tokens.borderRadiusMedium,
    ':hover': { opacity: 0.85 },
  },
  logo: { height: '28px', width: 'auto', display: 'block' },
  searchWrap: { flex: 1, display: 'flex', justifyContent: 'center' },
  search: { width: '100%', maxWidth: '480px' },
  right: { display: 'flex', alignItems: 'center', columnGap: tokens.spacingHorizontalXS },
  onBrand: {
    color: tokens.colorNeutralForegroundOnBrand,
    ':hover': {
      backgroundColor: 'rgba(255,255,255,0.18)',
      color: tokens.colorNeutralForegroundOnBrand,
    },
    ':hover:active': {
      backgroundColor: 'rgba(255,255,255,0.28)',
      color: tokens.colorNeutralForegroundOnBrand,
    },
  },
  bell: { position: 'relative' },
  badge: { position: 'absolute', top: '4px', right: '4px' },
  hideOnMobile: { '@media (max-width: 768px)': { display: 'none' } },
  // Pastille de statut de connexion
  status: {
    display: 'inline-flex',
    alignItems: 'center',
    columnGap: tokens.spacingHorizontalXS,
    padding: `2px ${tokens.spacingHorizontalS}`,
    borderRadius: tokens.borderRadiusCircular,
    backgroundColor: 'rgba(255,255,255,0.15)',
    fontSize: tokens.fontSizeBase200,
    lineHeight: '16px',
    whiteSpace: 'nowrap',
  },
  dot: { width: '8px', height: '8px', borderRadius: '50%', flexShrink: 0 },
  dotOnline: { backgroundColor: '#2ecc71' },
  dotOffline: { backgroundColor: '#ff5a5a' },
  dotConnecting: { backgroundColor: '#f5c451' },
  langLabel: { '@media (max-width: 768px)': { display: 'none' } },
  // Menu profil
  profileMenu: { minWidth: '320px', maxWidth: '360px' },
  profileHeader: {
    display: 'flex',
    alignItems: 'center',
    columnGap: tokens.spacingHorizontalM,
    padding: `${tokens.spacingVerticalL} ${tokens.spacingHorizontalL}`,
  },
  profileInfo: { display: 'flex', flexDirection: 'column', rowGap: '2px', minWidth: 0 },
  profileName: { color: tokens.colorNeutralForeground1 },
  profileUser: { color: tokens.colorNeutralForeground3 },
  profileRole: { color: tokens.colorBrandForeground1 },
  profileItem: { paddingTop: tokens.spacingVerticalM, paddingBottom: tokens.spacingVerticalM },
  avatarBtn: {
    padding: 0,
    minWidth: 0,
    border: 'none',
    background: 'transparent',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    borderRadius: tokens.borderRadiusCircular,
  },
})

export function TopBar({ onMenuClick }: { onMenuClick: () => void }) {
  const styles = useStyles()
  const { t, i18n } = useTranslation()
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [launcherOpen, setLauncherOpen] = useState(false)
  const status = useConnectionStatus()
  const isFr = i18n.language.startsWith('fr')
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { me } = useCurrentUser()
  const displayName = me?.displayName ?? me?.username ?? '—'
  const usernameLabel = me ? `@${me.username}` : ''
  const roleLabel = me?.roles?.[0] ?? ''
  const onLogout = () => {
    clearToken()
    queryClient.removeQueries({ queryKey: ['me'] })
    void navigate({ to: '/login' as never })
  }
  const onSwitchEstablishment = async (facilityId: string) => {
    if (facilityId === me?.activeEstablishment?.facilityId) return
    const res = await selectEstablishment(facilityId)
    setToken(res.token)
    queryClient.setQueryData(['me'], res.user)
    await queryClient.invalidateQueries()
    void navigate({ to: '/' as never })
  }

  const statusDot = mergeClasses(
    styles.dot,
    status === 'online' ? styles.dotOnline : status === 'offline' ? styles.dotOffline : styles.dotConnecting,
  )

  return (
    <header className={styles.root}>
      <Button
        appearance="transparent"
        className={styles.onBrand}
        icon={<Navigation24Regular />}
        onClick={onMenuClick}
        aria-label="Menu"
      />
      <Button
        appearance="transparent"
        className={mergeClasses(styles.onBrand, styles.hideOnMobile)}
        icon={<Apps24Regular />}
        aria-label={t('launcher.open')}
        onClick={() => setLauncherOpen(true)}
      />
      <button
        type="button"
        className={styles.logoBtn}
        onClick={() => setLauncherOpen(true)}
        aria-label={t('launcher.open')}
      >
        <img src="/logo/lafie-white.svg" alt="Lafie" className={styles.logo} />
      </button>

      <div className={styles.searchWrap}>
        <SearchBox className={styles.search} placeholder={t('search')} />
      </div>

      <div className={styles.right}>
        {/* Statut de connexion (avant la langue) */}
        <span className={styles.status} role="status" aria-live="polite">
          <span className={statusDot} />
          <span>{t(`topbar.status.${status}`)}</span>
        </span>

        {/* Établissement actif — sélecteur (multi-établissements) */}
        {me && me.establishments.length > 0 && (
          <Menu>
            <MenuTrigger disableButtonEnhancement>
              <Button
                appearance="transparent"
                className={styles.onBrand}
                icon={<Building20Regular />}
                aria-label={t('auth.establishment')}
              >
                <span className={styles.langLabel}>{me.activeEstablishment?.name ?? t('auth.establishment')}</span>
              </Button>
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                {me.establishments.map((e) => (
                  <MenuItem
                    key={e.facilityId}
                    icon={e.facilityId === me.activeEstablishment?.facilityId ? <Checkmark16Regular /> : undefined}
                    onClick={() => onSwitchEstablishment(e.facilityId)}
                  >
                    {e.name}
                  </MenuItem>
                ))}
              </MenuList>
            </MenuPopover>
          </Menu>
        )}

        {/* Langue — menu déroulant */}
        <Menu>
          <MenuTrigger disableButtonEnhancement>
            <Button
              appearance="transparent"
              className={styles.onBrand}
              icon={<Globe20Regular />}
              aria-label={t('topbar.language')}
            >
              <span className={styles.langLabel}>{isFr ? 'FR' : 'EN'}</span>
            </Button>
          </MenuTrigger>
          <MenuPopover>
            <MenuList>
              <MenuItem
                icon={isFr ? <Checkmark16Regular /> : undefined}
                onClick={() => i18n.changeLanguage('fr')}
              >
                Français
              </MenuItem>
              <MenuItem
                icon={!isFr ? <Checkmark16Regular /> : undefined}
                onClick={() => i18n.changeLanguage('en')}
              >
                English
              </MenuItem>
            </MenuList>
          </MenuPopover>
        </Menu>

        <Tooltip content="Notifications" relationship="label">
          <span className={styles.bell}>
            <Button
              appearance="transparent"
              className={styles.onBrand}
              icon={<Alert24Regular />}
              aria-label="Notifications"
            />
            <CounterBadge className={styles.badge} count={3} size="small" color="danger" />
          </span>
        </Tooltip>

        {/* Profil — menu */}
        <Menu>
          <MenuTrigger disableButtonEnhancement>
            <button type="button" className={styles.avatarBtn} aria-label={displayName}>
              <Avatar
                name={displayName}
                size={28}
                color="colorful"
                badge={{ status: status === 'online' ? 'available' : 'offline' }}
              />
            </button>
          </MenuTrigger>
          <MenuPopover className={styles.profileMenu}>
            <div className={styles.profileHeader}>
              <Avatar name={displayName} size={48} color="colorful" />
              <div className={styles.profileInfo}>
                <Text className={styles.profileName} weight="semibold" size={400}>
                  {displayName}
                </Text>
                <Text className={styles.profileUser} size={200}>
                  {usernameLabel}
                </Text>
                <Text className={styles.profileRole} size={200}>
                  {roleLabel}
                </Text>
              </div>
            </div>
            <Divider />
            <MenuList>
              <MenuItem className={styles.profileItem} icon={<Person20Regular />}>
                {t('topbar.profile.myProfile')}
              </MenuItem>
              <MenuItem className={styles.profileItem} icon={<ShieldKeyhole20Regular />}>
                {t('topbar.profile.accountSecurity')}
              </MenuItem>
              <MenuItem className={styles.profileItem} icon={<Settings20Regular />} onClick={() => setSettingsOpen(true)}>
                {t('topbar.profile.settings')}
              </MenuItem>
              <MenuDivider />
              <MenuItem className={styles.profileItem} icon={<QuestionCircle20Regular />}>
                {t('topbar.profile.help')}
              </MenuItem>
              <MenuItem className={styles.profileItem} icon={<Info20Regular />}>
                {t('topbar.profile.systemInfo')}
              </MenuItem>
              <MenuDivider />
              <MenuItem className={styles.profileItem} icon={<SignOut20Regular />} onClick={onLogout}>
                {t('topbar.profile.logout')}
              </MenuItem>
            </MenuList>
          </MenuPopover>
        </Menu>
      </div>

      <SettingsDialog open={settingsOpen} onOpenChange={setSettingsOpen} />
      <AppLauncherDialog open={launcherOpen} onOpenChange={setLauncherOpen} />
    </header>
  )
}
