import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from '@tanstack/react-router'
import {
  Button, Field, Input, Menu, MenuItem, MenuList, MenuPopover, MenuTrigger, Spinner,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { ChevronDown16Regular, Eye20Regular, EyeOff20Regular, Globe20Regular } from '@fluentui/react-icons'
import { useThemeMode } from '@app/providers/ThemeModeProvider'
import { login, setToken } from '../api'
import logoFull from '../../../assets/logos/lafie-full.svg'
import logoFullDark from '../../../assets/logos/lafie-full-dark.svg'

const useStyles = makeStyles({
  root: { display: 'flex', minHeight: '100vh', backgroundColor: tokens.colorNeutralBackground1 },
  form: {
    width: '25%',
    minWidth: '360px',
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'flex-start',
    padding: `0 ${tokens.spacingHorizontalXXXL}`,
    paddingTop: '9vh',
    position: 'relative',
    '@media (max-width: 768px)': { width: '100%', minWidth: 0, padding: tokens.spacingHorizontalXL, paddingTop: '8vh' },
  },
  inner: { width: '100%', maxWidth: '340px', margin: '0 auto', display: 'flex', flexDirection: 'column' },
  logo: { height: '52px', width: 'auto', alignSelf: 'flex-start', marginBottom: tokens.spacingVerticalXXXL },
  title: { fontSize: '28px', fontWeight: tokens.fontWeightSemibold, color: tokens.colorNeutralForeground1, margin: `0 0 ${tokens.spacingVerticalXL}` },
  field: { marginBottom: tokens.spacingVerticalL },
  submit: { marginTop: tokens.spacingVerticalM, height: '44px' },
  error: { color: tokens.colorPaletteRedForeground1, fontSize: tokens.fontSizeBase200, marginTop: tokens.spacingVerticalS },
  forgot: { marginTop: tokens.spacingVerticalXXL, color: tokens.colorBrandForeground1, textDecoration: 'none', fontSize: tokens.fontSizeBase300 },
  reveal: { background: 'none', border: 'none', padding: 0, cursor: 'pointer', display: 'inline-flex', color: tokens.colorNeutralForeground3 },
  brand: {
    flex: 1,
    backgroundColor: tokens.colorBrandBackground,
    position: 'relative',
    '@media (max-width: 768px)': { display: 'none' },
  },
  lang: {
    position: 'absolute',
    top: tokens.spacingVerticalL,
    right: tokens.spacingHorizontalL,
  },
  langMobile: {
    position: 'absolute',
    top: tokens.spacingVerticalL,
    right: tokens.spacingHorizontalL,
    '@media (min-width: 769px)': { display: 'none' },
  },
  langBtn: { color: tokens.colorNeutralForegroundOnBrand },
  langBtnMobile: { color: tokens.colorNeutralForeground2 },
})

function LafieLogo() {
  const styles = useStyles()
  const { isDark } = useThemeMode()
  return <img className={styles.logo} src={isDark ? logoFullDark : logoFull} alt="Lafie" />
}

function LanguageMenu({ className }: { className?: string }) {
  const { t, i18n } = useTranslation()
  const isFr = i18n.language.startsWith('fr')
  return (
    <Menu>
      <MenuTrigger disableButtonEnhancement>
        <Button className={className} appearance="transparent" icon={<Globe20Regular />} iconPosition="before" aria-label={t('topbar.language')}>
          {isFr ? 'FR' : 'EN'} <ChevronDown16Regular />
        </Button>
      </MenuTrigger>
      <MenuPopover>
        <MenuList>
          <MenuItem onClick={() => i18n.changeLanguage('fr')}>Français</MenuItem>
          <MenuItem onClick={() => i18n.changeLanguage('en')}>English</MenuItem>
        </MenuList>
      </MenuPopover>
    </Menu>
  )
}

const schema = z.object({
  login: z.string().trim().min(1),
  password: z.string().min(1),
})
type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [show, setShow] = useState(false)

  const { handleSubmit, setValue, watch, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { login: '', password: '' },
  })
  const v = watch()

  const mutation = useMutation({
    mutationFn: (vals: FormValues) => login({ login: vals.login, password: vals.password }),
    onSuccess: async (res) => {
      setToken(res.token)
      queryClient.setQueryData(['me'], res.user)
      void navigate({ to: '/' })
    },
  })

  const onSubmit = handleSubmit((vals) => mutation.mutate(vals))

  return (
    <div className={styles.root}>
      <div className={styles.form}>
        <div className={styles.langMobile}><LanguageMenu className={styles.langBtnMobile} /></div>
        <form className={styles.inner} onSubmit={onSubmit}>
          <LafieLogo />
          <h1 className={styles.title}>{t('auth.title')}</h1>

          <Field className={styles.field} label={t('auth.login')}
            validationMessage={formState.errors.login ? t('auth.loginRequired') : undefined}>
            <Input type="text" autoComplete="username" value={v.login}
              onChange={(_, d) => setValue('login', d.value, { shouldValidate: formState.isSubmitted })} />
          </Field>

          <Field className={styles.field} label={t('auth.password')}
            validationMessage={formState.errors.password ? t('auth.passwordRequired') : undefined}>
            <Input type={show ? 'text' : 'password'} autoComplete="current-password" value={v.password}
              onChange={(_, d) => setValue('password', d.value, { shouldValidate: formState.isSubmitted })}
              contentAfter={
                <button type="button" className={styles.reveal} onClick={() => setShow((s) => !s)}
                  aria-label={t(show ? 'auth.hidePassword' : 'auth.showPassword')}>
                  {show ? <EyeOff20Regular /> : <Eye20Regular />}
                </button>
              } />
          </Field>

          <Button className={styles.submit} appearance="primary" type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? <Spinner size="tiny" label={t('auth.signingIn')} /> : t('auth.signIn')}
          </Button>

          {mutation.isError && <span className={styles.error}>{t('auth.invalidCredentials')}</span>}

          <Link to={'/forgot-password' as never} className={styles.forgot}>{t('auth.forgotPassword')}</Link>
        </form>
      </div>

      <div className={styles.brand}>
        <div className={styles.lang}><LanguageMenu className={styles.langBtn} /></div>
      </div>
    </div>
  )
}
