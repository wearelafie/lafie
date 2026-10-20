import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'

// i18n — FR par défaut (cible Togo), EN. Ressources inline au démarrage ;
// à découper en namespaces par feature et/ou chargement paresseux plus tard.
const resources = {
  fr: {
    translation: {
      appTitle: 'Lafie Navigator',
      appSubtitle: 'Plateforme HIS/EMR',
      search: 'Rechercher un patient, un dossier…',
      switchLang: 'English',
      nav: {
        home: 'Accueil',
        patients: 'Patients',
        agenda: 'Agenda',
        clinical: 'Clinique',
        pharmacy: 'Pharmacie',
        lab: 'Laboratoire',
        billing: 'Facturation',
        admin: 'Administration',
        sub: {
          patients: {
            list: 'Liste des patients',
            new: 'Nouveau patient',
            admissions: 'Admissions (ADT)',
            merge: 'Fusion de dossiers',
          },
          agenda: {
            planning: 'Planning',
            appointments: 'Rendez-vous',
            rooms: 'Salles & ressources',
          },
          clinical: {
            consultations: 'Consultations',
            observations: 'Observations',
            prescriptions: 'Prescriptions',
            history: 'Antécédents',
            documents: 'Documents',
          },
          pharmacy: {
            dispensing: 'Dispensation',
            stock: 'Stock',
            orders: 'Commandes',
            interactions: 'Interactions',
          },
          lab: {
            requests: 'Demandes',
            results: 'Résultats',
            samples: 'Prélèvements',
          },
          billing: {
            invoices: 'Factures',
            payments: 'Paiements',
            insurance: 'Assurances',
            pricing: 'Tarifs',
          },
          admin: {
            facilities: 'Établissements',
            users: 'Utilisateurs & rôles',
            terminology: 'Terminologies',
            interop: 'Interop / FHIR',
            audit: 'Audit',
            settings: 'Paramètres',
          },
        },
      },
      launcher: {
        title: 'Tous les modules',
        search: 'Rechercher un module…',
        open: 'Ouvrir le menu des modules',
        noResult: 'Aucun module trouvé.',
      },
      common: { comingSoon: 'Module en cours de construction.' },
      command: { newPatient: 'Nouveau patient', refresh: 'Actualiser' },
      dashboard: {
        welcome: 'Bienvenue',
        subtitle: "Vue d'ensemble de l'activité",
        today: "Agenda du jour",
        empty: 'Aucun élément.',
        kpi: {
          patientsToday: 'Patients du jour',
          appointments: 'Rendez-vous',
          pendingResults: 'Résultats en attente',
          alerts: 'Alertes',
        },
        system: 'Système',
        api: 'API',
        database: 'Base de données',
        reachable: 'joignable',
        unreachable: 'injoignable',
        updatedAt: 'Mis à jour à {{time}}',
      },
      settings: {
        title: 'Paramètres',
        close: 'Fermer',
        comingSoon: 'À venir.',
        sections: {
          language: 'Langue et heure',
          appearance: 'Apparence',
          notifications: 'Notifications',
          privacy: 'Confidentialité et données',
          search: 'Recherche',
        },
        appearance: {
          chooseMode: 'Choisir votre mode',
          mode: { light: 'Clair', dark: 'Sombre', system: 'Paramètres système' },
          themes: 'Thèmes',
          color: 'Couleur',
          navbar: 'Barre de navigation',
          navbarHint: 'Personnaliser la façon dont vos applications sont affichées',
          coloredIcons: "Utiliser des icônes d'application colorées",
          showNames: "Afficher les noms d'appli",
        },
        language: { label: 'Langue', french: 'Français', english: 'English' },
      },
    },
  },
  en: {
    translation: {
      appTitle: 'Lafie Navigator',
      appSubtitle: 'HIS/EMR platform',
      search: 'Search a patient, a record…',
      switchLang: 'Français',
      nav: {
        home: 'Home',
        patients: 'Patients',
        agenda: 'Schedule',
        clinical: 'Clinical',
        pharmacy: 'Pharmacy',
        lab: 'Laboratory',
        billing: 'Billing',
        admin: 'Administration',
        sub: {
          patients: {
            list: 'Patient list',
            new: 'New patient',
            admissions: 'Admissions (ADT)',
            merge: 'Record merge',
          },
          agenda: {
            planning: 'Planning',
            appointments: 'Appointments',
            rooms: 'Rooms & resources',
          },
          clinical: {
            consultations: 'Consultations',
            observations: 'Observations',
            prescriptions: 'Prescriptions',
            history: 'History',
            documents: 'Documents',
          },
          pharmacy: {
            dispensing: 'Dispensing',
            stock: 'Stock',
            orders: 'Orders',
            interactions: 'Interactions',
          },
          lab: {
            requests: 'Requests',
            results: 'Results',
            samples: 'Samples',
          },
          billing: {
            invoices: 'Invoices',
            payments: 'Payments',
            insurance: 'Insurance',
            pricing: 'Pricing',
          },
          admin: {
            facilities: 'Facilities',
            users: 'Users & roles',
            terminology: 'Terminologies',
            interop: 'Interop / FHIR',
            audit: 'Audit',
            settings: 'Settings',
          },
        },
      },
      launcher: {
        title: 'All modules',
        search: 'Search a module…',
        open: 'Open the module menu',
        noResult: 'No module found.',
      },
      common: { comingSoon: 'Module under construction.' },
      command: { newPatient: 'New patient', refresh: 'Refresh' },
      dashboard: {
        welcome: 'Welcome',
        subtitle: 'Activity overview',
        today: "Today's schedule",
        empty: 'No item.',
        kpi: {
          patientsToday: 'Patients today',
          appointments: 'Appointments',
          pendingResults: 'Pending results',
          alerts: 'Alerts',
        },
        system: 'System',
        api: 'API',
        database: 'Database',
        reachable: 'reachable',
        unreachable: 'unreachable',
        updatedAt: 'Updated at {{time}}',
      },
      settings: {
        title: 'Settings',
        close: 'Close',
        comingSoon: 'Coming soon.',
        sections: {
          language: 'Language and time',
          appearance: 'Appearance',
          notifications: 'Notifications',
          privacy: 'Privacy and data',
          search: 'Search',
        },
        appearance: {
          chooseMode: 'Choose your mode',
          mode: { light: 'Light', dark: 'Dark', system: 'System settings' },
          themes: 'Themes',
          color: 'Color',
          navbar: 'Navigation bar',
          navbarHint: 'Customize how your apps are displayed',
          coloredIcons: 'Use colored app icons',
          showNames: 'Show app names',
        },
        language: { label: 'Language', french: 'Français', english: 'English' },
      },
    },
  },
}

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: 'fr',
    supportedLngs: ['fr', 'en'],
    interpolation: { escapeValue: false },
  })

export default i18n
