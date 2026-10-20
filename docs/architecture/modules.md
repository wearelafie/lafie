# Catalogue des modules Lafie

Proposition de découpage fonctionnel du HIS/EMR en **bounded contexts** (modules). Statut : **proposition à valider/affiner**. Chaque module = 4 couches (`Domain`/`Application`/`Infrastructure`/`Api`), nommage `Lafie.<Module>.*`.

Légende : ✅ déjà validé · ➕ proposé · Phase = jalon de livraison (voir §Roadmap).

## A. Administration & identité

| Module | Périmètre | Ressources FHIR clés | Dépend de | Phase |
| --- | --- | --- | --- | --- |
| **Identity** ✅ | Utilisateurs, authentification/autorisation, professionnels de santé, rôles, habilitations | `Practitioner`, `PractitionerRole` | — | 0 |
| **Organization** ✅ | Établissements, services, unités de soins, salles, **lits**, localisations | `Organization`, `Location`, `HealthcareService` | — | 0 |
| **Patient** ✅ | Démographie, **MPI** (index maître), identifiants (INS/NIF/MRN), proches, couverture | `Patient`, `RelatedPerson` | Terminology | 0 |

## B. Parcours clinique

| Module | Périmètre | Ressources FHIR clés | Dépend de | Phase |
| --- | --- | --- | --- | --- |
| **Scheduling** ➕ | Rendez-vous, créneaux, ressources, files d'attente | `Appointment`, `Slot`, `Schedule` | Patient, Organization | 1 |
| **Encounter (ADT)** ➕ | Admission / sortie / transfert, affectation de lit, gestion des rencontres | `Encounter`, `EpisodeOfCare` | Patient, Organization | 1 |
| **Clinical** ✅ | Cœur EMR : observations/signes vitaux, diagnostics (conditions), problèmes, allergies, notes cliniques, actes, plans de soins | `Observation`, `Condition`, `AllergyIntolerance`, `Procedure`, `CarePlan`, `ClinicalImpression` | Patient, Encounter, Terminology | 1 |
| **Orders (CPOE)** ➕ | Prescription connectée : orchestration des demandes (labo, imagerie, médicaments, adressage) | `ServiceRequest`, `MedicationRequest`, `Task` | Clinical | 1 |
| **Pharmacy** ✅ | Livret thérapeutique, prescription, dispensation, administration (MAR), stock médicaments | `Medication`, `MedicationRequest`, `MedicationDispense`, `MedicationAdministration` | Orders, Terminology | 1 |
| **Laboratory** ✅ | Demandes labo, prélèvements, résultats, comptes rendus, interop LIS | `ServiceRequest`, `Specimen`, `Observation`, `DiagnosticReport` | Orders, Terminology | 1 |
| **Radiology / Imaging** ➕ | Demandes d'imagerie, **DICOM / DICOMweb**, intégration PACS/VNA, comptes rendus. Lib .NET : **fo-dicom**. Voir [`standards/dicom.md`](../standards/dicom.md) | `ServiceRequest`, `ImagingStudy`, `DiagnosticReport` | Orders | 2 |
| **Nursing / Care** ➕ | Évaluations infirmières, tâches de soins, tournées de constantes, exécution MAR | `Task`, `Observation`, `CarePlan` | Clinical, Encounter | 2 |

## C. Support & logistique

| Module | Périmètre | Ressources FHIR clés | Dépend de | Phase |
| --- | --- | --- | --- | --- |
| **Inventory / Stock** ➕ | Fournitures médicales, entrepôt, mouvements, seuils (mutualisable avec Pharmacy) | `InventoryItem`, `SupplyRequest`, `SupplyDelivery` | Organization | 2 |
| **Documents (DMS)** ➕ | Gestion documentaire, pièces scannées, stockage des comptes rendus | `DocumentReference`, `Binary` | — | 2 |

## D. Financier

| Module | Périmètre | Ressources FHIR clés | Dépend de | Phase |
| --- | --- | --- | --- | --- |
| **Billing** ✅ | Tarification, facturation, encaissement | `ChargeItem`, `Invoice` | Encounter, Clinical | 1 |
| **Insurance / Coverage** ➕ | Payeurs, couverture (**AMU** Togo), prises en charge, réclamations (X12 US) | `Coverage`, `Claim`, `ClaimResponse` | Patient, Billing | 2 |

## E. Interopérabilité & pilotage (plateforme)

| Module | Périmètre | Ressources FHIR clés | Dépend de | Phase |
| --- | --- | --- | --- | --- |
| **Terminology** ✅ | Service multi-code (LOINC/SNOMED/ICD/ATC), validation de binding, expansions | `CodeSystem`, `ValueSet`, `ConceptMap` | — | 0 |
| **Interop** ✅ | FHIR Gateway (IPS/Bundle), **connecteur DHIS2**, HL7v2/DICOM ingress, échange documents, **adapters de conformité par pays** | `Bundle`, `MessageHeader`, `Consent` | (ports) | 0 |
| **DataProtection** ➕ | **Consentement** (`Consent` FHIR), droits des personnes (accès/rectification/effacement/portabilité), politique de rétention, pilotage de l'audit d'accès | `Consent`, `AuditEvent` | (transverse) | 1 |
| **Reporting & Analytics** ➕ | Indicateurs, tableaux de bord, agrégats **SNIS/DHIS2** | `Measure`, `MeasureReport` | Interop | 2 |
| **Notifications** ➕ | SMS / e-mail / push (rappels de RDV — clé mobile Togo) | `Communication`, `CommunicationRequest` | Scheduling | 1 |
| **Quality & Vigilance** ✅ | Déclaration d'incidents, événements indésirables, **pharmaco/matériovigilance**, actions correctives/préventives (CAPA) | `AdverseEvent`, `DetectedIssue` | (transverse) | 3 |

## F. Back-office & gestion administrative (non-clinique)

| Module | Périmètre | Ressources FHIR clés | Dépend de | Phase |
| --- | --- | --- | --- | --- |
| **HumanResources (RH)** ➕ | Dossier agent (employés), contrats, congés/absences, présence & pointage, **planning du personnel** (gardes/roulements), paie | `Practitioner` *(lien)* | Organization, Identity | 1 |
| **Accounting (Comptabilité)** ➕ | Comptabilité générale (grand livre, écritures en partie double), **tiers fournisseurs**, trésorerie/banque/caisse, immobilisations, clôtures & **états financiers** ; consomme les flux **Billing** (factures / encaissements) | — | Billing | 1 |
| **FrontDesk (Secrétariat / Accueil)** ➕ | Accueil & enregistrement, **check-in**, **file d'attente**/guichets, prise de RDV au comptoir, courrier entrant/sortant, **édition de documents administratifs** (attestations, certificats) | `Appointment`, `Task` | Patient, Scheduling, Encounter | 1 |

**Frontières (éviter les doublons) :**
- **HR vs Identity** : `HumanResources` = cycle de vie de l'agent (RH) et source du **dossier employé** ; `Identity` = comptes/authentification + `Practitioner`/`PractitionerRole` cliniques. Identity **consomme** (par événement) le dossier RH pour provisionner les comptes.
- **Accounting vs Billing** : `Billing` = facturation **patient** (`ChargeItem`/`Invoice`, recouvrement) ; `Accounting` = comptabilité **de l'établissement** (grand livre, fournisseurs, trésorerie). Accounting **consomme** les événements Billing (facture émise, encaissement) pour générer les écritures.
- **FrontDesk** **réutilise** `Scheduling` (RDV), `Encounter` (check-in) et `Documents` (édition) ; il **orchestre** l'accueil, il ne duplique pas ces domaines.

## Transverse (BuildingBlocks, pas des modules)

- **Audit ATNA** — écriture du journal inaltérable des accès → `Lafie.Infrastructure` (bas niveau). La **consultation/politique** d'audit est exposée par le module **DataProtection**.
- Auth OIDC/SMART, config, télémétrie → `Lafie.Infrastructure`.

> Décision : **Consent** est désormais dans un **module `DataProtection` dédié** (RGPD/HIPAA/loi 2019-014), pas dans Interop.

## Spécialités (phase ultérieure — pas encore des modules)

Candidats phase 3, pertinents selon la cible : **Emergency/Triage**, **Surgery / Bloc opératoire**, **Maternité/Obstétrique** (santé maternelle Togo), **Immunisation/PEV** (vaccination OMS), **Banque du sang**.

## Candidats supplémentaires (revue des docs — à valider)

Issus d'une revue des docs (back-office ci-dessus, logistique, mobile/Togo, conformité, SAMD). **Non figés** — à retenir selon la cible.

| Module | Périmètre | Ressources FHIR clés | Complète | Phase proposée |
| --- | --- | --- | --- | --- |
| **Procurement / Achats** ➕ | Demandes d'achat, fournisseurs, appels d'offres, bons de commande, réception | `SupplyRequest`, `SupplyDelivery` | Inventory, Accounting | 2 |
| **FixedAssets / Immobilisations** ➕ | Parc d'équipements, amortissements, inventaire physique *(peut être un sous-domaine d'Accounting)* | `Device` | Accounting, Biomedical | 2 |
| **Biomedical / GMAO** ➕ | Maintenance des équipements médicaux (préventive/curative), étalonnage, matériovigilance | `Device`, `Task` | Organization, Inventory | 2 |
| **Teleconsultation** ➕ | Téléconsultation / télé-expertise (pertinence rurale **Togo**), RDV & rencontres virtuelles | `Appointment` *(virtual)*, `Encounter` | Scheduling, Clinical | 2 |
| **PatientPortal / Engagement** ➕ | Portail/app patient : RDV en ligne, résultats, rappels, **USSD/SMS** (mobile Togo) | `Patient`, `Communication` | Scheduling, Notifications | 2–3 |
| **Services hôteliers** ➕ | Diététique/Restauration, Lingerie/Hygiène, **Morgue/Mortuaire**, Transport/Ambulance, Brancardage | `NutritionOrder`, `Task` | Organization | 3 |

## Roadmap de livraison (Togo d'abord)

| Phase | Modules | Objectif |
| --- | --- | --- |
| **0 — Socle** | BuildingBlocks + Identity, Organization, Patient, Terminology, Interop | Fondations + interop/identité. Cible du **squelette**. |
| **1 — MVP soins (Togo)** | Scheduling, Encounter (ADT), Clinical, Orders, Pharmacy, Laboratory, Billing, DataProtection, Notifications, **HumanResources, Accounting, FrontDesk** | **Périmètre MVP confirmé.** Parcours de soins de bout en bout + facturation + conformité données + **back-office** (RH, comptabilité, secrétariat). |
| **2 — Extension** | Radiology, Nursing, Inventory, Documents, Insurance, Reporting | Ancillaires, logistique, couverture AMU, reporting SNIS. |
| **3 — Spécialités & qualité** | Emergency, Surgery, Maternité, Immunisation, Banque du sang, **Quality & Vigilance** | Services spécialisés + gestion de la qualité / vigilance. |

## Règles de dépendances

- Aucun module ne référence un autre directement. Les liens « Dépend de » se traduisent en **integration events** (Outbox) ou **API publiques** exposées via `Lafie.Shared`.
- `Orders (CPOE)` est un **orchestrateur** : il émet des demandes que Pharmacy/Laboratory/Radiology consomment par événement (chorégraphie), pas par appel direct.
- `Terminology` et `Interop` sont des **modules supports** transverses au parcours clinique.

## Décisions actées (2026-07-21)

- **Nursing** : ✅ module dédié (Phase 2).
- **Encounter (ADT)** : ✅ module dédié, séparé de `Clinical` (Phase 1).
- **Consent** : ✅ module **`DataProtection`** dédié (Phase 1), pas dans Interop.
- **Insurance / Coverage** : ✅ module séparé de `Billing` (Phase 2).
- **Périmètre MVP Togo** : ✅ = **Phase 1** (Scheduling, Encounter, Clinical, Orders, Pharmacy, Laboratory, Billing, DataProtection, Notifications), sur socle Phase 0.

## Décisions actées (2026-10-20)

- **Ajout au MVP (Phase 1)** : **HumanResources (RH)**, **Accounting (Comptabilité)**, **FrontDesk (Secrétariat/Accueil)** — demande utilisateur. Nouvelle catégorie **F. Back-office** (non-clinique).
- **Quality & Vigilance** : ✅ module retenu (transverse, Phase 3) — demande utilisateur.
- **Autres candidats** (revue docs, **à valider plus tard**) : Procurement/Achats, FixedAssets/Immobilisations, Biomedical/GMAO, Teleconsultation, PatientPortal, Services hôteliers.
- **Organization** : 🏗️ **premier module implémenté** en vertical slice (voir ci-dessous).

## État d'implémentation

| Module | Statut | Détail |
| --- | --- | --- |
| **Organization** | 🏗️ Facilities (F1) | Agrégat enrichi (**statut** Draft/Active/Inactive, nom court, contact, ville/pays, timezone, `UpdatedOnUtc`), CQRS Create/Update/Activate/Deactivate + **liste paginée/filtrée** (`GetOrganizations`), endpoints `/organizations` (GET liste+`{id}`, POST, PUT, POST activate/deactivate), **désactivation logique**. Frontend **Administration → Établissements** : recherche+filtres (type/statut) + bouton Rechercher, table (badge statut), création/édition, activer/désactiver, pagination, sélection (Zustand). Tests : **unit 12/12**, **intégration Testcontainers 5/5**. |
| Identity, Patient, Terminology, Interop | ⬜ squelette | 4 couches câblées, pas encore de feature. |

> **Décisions techniques** : `AddMediator(o => o.ServiceLifetime = ServiceLifetime.Scoped)` (handlers ↔ services scoped). Endpoints via `Map<Module>Endpoints(IEndpointRouteBuilder)` dans `Program.cs`. **Migrations SQL** versionnées (runner maison, ADR-014). **`CustomExceptionHandler`** mappe `CustomException` (NotFound…) → code HTTP + ProblemDetails. Pagination générique via `Core.Common.PagedResult<T>`.

## Catalogue figé

**Phase 0 (5)** : Identity, Organization, Patient, Terminology, Interop.
**Phase 1 (12)** : Scheduling, Encounter, Clinical, Orders, Pharmacy, Laboratory, Billing, DataProtection, Notifications, **HumanResources, Accounting, FrontDesk**.
**Phase 2 (6)** : Radiology, Nursing, Inventory, Documents, Insurance, Reporting.
**Phase 3** : Emergency, Surgery, Maternité, Immunisation/PEV, Banque du sang, **Quality & Vigilance**.

→ **23 modules** cadrés (Phases 0–2) + **6** en Phase 3 + **candidats à valider** (voir section dédiée).
