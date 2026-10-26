import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useMutation } from '@tanstack/react-query'
import { Link, useNavigate } from '@tanstack/react-router'
import {
  Button, Field, Input, Menu, MenuItem, MenuList, MenuPopover, MenuTrigger, Spinner,
  makeStyles, tokens,
} from '@fluentui/react-components'
import { ChevronDown16Regular, Eye20Regular, EyeOff20Regular, Globe20Regular } from '@fluentui/react-icons'
import { login, setToken } from '../api'

const useStyles = makeStyles({
  root: { display: 'flex', minHeight: '100vh', backgroundColor: tokens.colorNeutralBackground1 },
  form: {
    width: '25%',
    minWidth: '360px',
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'center',
    padding: `${tokens.spacingVerticalXXL} ${tokens.spacingHorizontalXXXL}`,
    position: 'relative',
    '@media (max-width: 768px)': { width: '100%', minWidth: 0, padding: tokens.spacingHorizontalXL },
  },
  inner: { width: '100%', maxWidth: '340px', margin: '0 auto', display: 'flex', flexDirection: 'column' },
  logo: { display: 'flex', alignItems: 'center', columnGap: '9px', marginBottom: tokens.spacingVerticalXXXL },
  wordmark: { fontSize: '23px', fontWeight: tokens.fontWeightSemibold, color: tokens.colorBrandForeground1, letterSpacing: '.3px' },
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
  return (
    <div className={styles.logo}>
      <svg width="26" height="26" viewBox="0 0 26 26" aria-hidden="true">
        <rect x="10.5" y="2" width="5" height="22" rx="2.5" fill="#00728b" />
        <rect x="2" y="10.5" width="22" height="5" rx="2.5" fill="#009664" />
      </svg>
      <span className={styles.wordmark}>lafie</span>
    </div>
  )
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
  email: z.string().trim().email(),
  password: z.string().min(1),
})
type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const styles = useStyles()
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [show, setShow] = useState(false)

  const { handleSubmit, setValue, watch, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  })
  const v = watch()

  const mutation = useMutation({
    mutationFn: (vals: FormValues) => login(vals),
    onSuccess: (res) => { setToken(res.token); void navigate({ to: '/' }) },
  })

  const onSubmit = handleSubmit((vals) => mutation.mutate(vals))

  return (
    <div className={styles.root}>
      <div className={styles.form}>
        <div className={styles.langMobile}><LanguageMenu className={styles.langBtnMobile} /></div>
        <form className={styles.inner} onSubmit={onSubmit}>
          <LafieLogo />
          <h1 className={styles.title}>{t('auth.title')}</h1>

          <Field className={styles.field} label={t('auth.email')}
            validationMessage={formState.errors.email ? t('auth.emailInvalid') : undefined}>
            <Input type="email" autoComplete="username" value={v.email}
              onChange={(_, d) => setValue('email', d.value, { shouldValidate: formState.isSubmitted })} />
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
