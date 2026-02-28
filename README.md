# Mon Portfolio - Portfolio Personnel React

Application web Single Page Application (SPA) développée avec React et Vite pour présenter mon profil, mes compétences et mes projets en informatique.

🚀 **Démo en ligne :** [https://mon-portfolio-iota-ivory.vercel.app](https://mon-portfolio-iota-ivory.vercel.app)

## 📋 Table des matières

- [Description](#description)
- [Fonctionnalités](#fonctionnalités)
- [Prérequis](#prérequis)
- [Installation](#installation)
- [Structure du projet](#structure-du-projet)
- [Déploiement](#déploiement)
- [Auteur](#auteur)

## 📖 Description

Ce portfolio est une vitrine professionnelle interactive permettant de :
- Consulter mon parcours et mon CV.
- Visualiser mes projets GitHub via une interface soignée.
- Me contacter via un formulaire interactif.
- Localiser ma position géographique sur une carte dynamique.

Il est conçu pour être performant, responsive et facile à maintenir.

## ✨ Fonctionnalités

### Navigation et Interface
- **SPA (Single Page Application)** : Navigation fluide sans rechargement grâce à `react-router-dom`.
- **Responsive Design** : Interface adaptée aux mobiles, tablettes et bureaux.
- **Thème sombre/clair** : Design moderne avec une palette de couleurs professionnelle.

### Pages et Composants
- **Accueil** : Présentation générale avec profil et call-to-action.
- **À propos** : Parcours académique et professionnel détaillé.
- **Projets** : Galerie de projets avec liens vers GitHub et démos.
- **Compétences** : Visualisation des compétences techniques (badges).
- **CV** : Visualiseur PDF intégré et bouton de téléchargement.
- **Contact** : 
  - Formulaire de contact fonctionnel.
  - Carte interactive (Leaflet) montrant la localisation.
  - Mascotte animée réagissant aux actions de l'utilisateur.

## 🔧 Prérequis

Avant de commencer, assurez-vous d'avoir installé :

- **Node.js** (v18.0.0 ou supérieur)
- **npm** (v9.0.0 ou supérieur) ou **yarn**

## 📥 Installation

### Étape 1 : Cloner le projet

```bash
git clone https://github.com/Seck2000/Mon-Portfolio.git
cd Mon-Portfolio
```

### Étape 2 : Installer les dépendances

```bash
npm install
```

### Étape 3 : Lancer le serveur de développement

```bash
npm run dev
```

L'application sera accessible à l'adresse `http://localhost:5173`.

## 📁 Structure du projet

```
Mon-Portfolio/
├── public/               # Fichiers statiques (images, CV, robots.txt)
├── src/                  # Code source
│   ├── assets/           # Ressources (images, icônes)
│   ├── components/       # Composants réutilisables
│   │   ├── Navbar.jsx    # Barre de navigation responsive
│   │   ├── Footer.jsx    # Pied de page
│   │   ├── SEO.jsx       # Gestion des méta-données (Head)
│   │   └── ...
│   ├── pages/            # Pages de l'application
│   │   ├── Home.jsx      # Page d'accueil
│   │   ├── About.jsx     # Page À propos
│   │   ├── Projects.jsx  # Page Projets
│   │   ├── Skills.jsx    # Page Compétences
│   │   ├── CV.jsx        # Page CV (PDF viewer)
│   │   └── Contact.jsx   # Page Contact (Formulaire + Map)
│   ├── App.jsx           # Composant racine et routing
│   ├── main.jsx          # Point d'entrée React
│   └── index.css         # Styles globaux
├── .npmrc                # Configuration NPM (legacy-peer-deps pour Vercel)
├── index.html            # Point d'entrée HTML
├── package.json          # Dépendances et scripts
└── vite.config.js        # Configuration Vite
```

## 🚀 Déploiement

Le projet est configuré pour être déployé facilement sur **Vercel**.

1. Pousser le code sur GitHub.
2. Connecter le dépôt à Vercel.
3. Le déploiement se fait automatiquement à chaque push.

> Un fichier `vercel.json` est inclus pour gérer la réécriture des URL (SPA routing).

## 👤 Auteur

**Aissatou Seck**

- **GitHub** : [@Seck2000](https://github.com/Seck2000)
- **LinkedIn** : [Aissatou Seck](https://www.linkedin.com/in/aissatou-seck-70a550393)
- **Email** : aminabalde200@gmail.com
