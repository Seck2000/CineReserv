# CineReserv - Système de Réservation de Cinéma

Application web ASP.NET Core pour la gestion et la réservation de séances de cinéma avec paiement électronique via Stripe.

## 📋 Table des matières

- [Description](#description)
- [Fonctionnalités](#fonctionnalités)
- [Prérequis](#prérequis)
- [Installation](#installation)
- [Configuration](#configuration)
- [Exécution](#exécution)
- [Structure du projet](#structure-du-projet)
- [Technologies utilisées](#technologies-utilisées)

## 📖 Description

CineReserv est une application web de réservation de billets de cinéma qui permet :
- Aux **clients** de rechercher des films, réserver des places, gérer leur panier et effectuer des paiements en ligne
- Aux **fournisseurs/organisateurs** de gérer leur catalogue de films, créer des séances et consulter leurs statistiques

## ✨ Fonctionnalités

### Pour les clients :
- 🔍 Recherche et filtrage de films par genre
- 🎫 Sélection du nombre de billets par catégorie d'âge (Enfant, Général, Aîné)
- 💺 Sélection interactive des sièges dans la salle
- 🛒 Gestion du panier de réservations
- 💳 Paiement sécurisé via Stripe
- 📄 Consultation de l'historique des réservations et des factures

### Pour les fournisseurs :
- 📊 Tableau de bord avec statistiques (revenus, taux d'occupation, clients actifs)
- 🎬 Gestion de leur catalogue de films (propre à chaque fournisseur)
- ⏰ Gestion des séances de projection
- 💰 Suivi des revenus et factures

## 🔧 Prérequis

Avant de commencer, assurez-vous d'avoir installé :

1. **.NET 8.0 SDK** (ou supérieur)
   - Télécharger depuis : https://dotnet.microsoft.com/download/dotnet/8.0
   - Vérifier l'installation : `dotnet --version`

2. **MySQL Server** (version 8.0 ou supérieure)
   - Télécharger depuis : https://dev.mysql.com/downloads/mysql/
   - Installer MySQL Server et MySQL Workbench (optionnel mais recommandé)

3. **Visual Studio 2022** ou **Visual Studio Code**
   - Visual Studio 2022 : https://visualstudio.microsoft.com/fr/downloads/
   - Visual Studio Code : https://code.visualstudio.com/

4. **Git** (pour cloner le dépôt)
   - Télécharger depuis : https://git-scm.com/downloads

5. **Compte Stripe** (pour les paiements)
   - Créer un compte test : https://dashboard.stripe.com/register

## 📥 Installation

### Étape 1 : Cloner le dépôt

```bash
git clone https://github.com/Seck2000/CineReserv.git
cd CineReserv
```

### Étape 2 : Installer MySQL Server

1. **Télécharger MySQL Server** depuis le site officiel
2. **Installer MySQL Server** avec les options par défaut
3. **Notez le mot de passe root** lors de l'installation (vous en aurez besoin)
4. **Vérifier l'installation** :
   - Ouvrir MySQL Command Line Client
   - Entrer le mot de passe root
   - Vous devriez voir `mysql>`

### Étape 3 : Créer la base de données MySQL

1. **Ouvrir MySQL Command Line Client** ou **MySQL Workbench**

2. **Se connecter** avec l'utilisateur `root` et votre mot de passe

3. **Créer la base de données** :
   ```sql
   CREATE DATABASE CineReservDB;
   ```

4. **Créer un utilisateur MySQL** (recommandé) :
   ```sql
   CREATE USER 'cinereservuser'@'localhost' IDENTIFIED BY 'VotreMotDePasse123!';
   GRANT ALL PRIVILEGES ON CineReservDB.* TO 'cinereservuser'@'localhost';
   FLUSH PRIVILEGES;
   ```
   
   > ⚠️ **Important** : Remplacez `VotreMotDePasse123!` par un mot de passe sécurisé de votre choix.

5. **Vérifier** :
   ```sql
   SHOW DATABASES;
   ```
   Vous devriez voir `CineReservDB` dans la liste.

### Étape 4 : Configurer la chaîne de connexion

1. **Ouvrir** le fichier `CineReserv/appsettings.json`

2. **Modifier** la chaîne de connexion `DefaultConnection` :

   **Si vous utilisez l'utilisateur root :**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=CineReservDB;User=root;Password=VotreMotDePasseMySQL;"
     }
   }
   ```

   **Si vous avez créé un utilisateur spécifique :**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=CineReservDB;User=cinereservuser;Password=VotreMotDePasse123!;"
     }
   }
   ```

   > ⚠️ **Important** : Remplacez `VotreMotDePasseMySQL` ou `VotreMotDePasse123!` par votre mot de passe MySQL réel.

### Étape 5 : Configurer Stripe (optionnel pour tester)

1. **Créer un compte Stripe** sur https://dashboard.stripe.com/register
2. **Accéder au tableau de bord** en mode test
3. **Récupérer vos clés API** :
   - Clé publique (Publishable Key) : Commence par `pk_test_...`
   - Clé secrète (Secret Key) : Commence par `sk_test_...`

4. **Modifier** le fichier `CineReserv/appsettings.json` :
   ```json
   {
     "Stripe": {
       "PublishableKey": "pk_test_votre_cle_publique_ici",
       "SecretKey": "sk_test_votre_cle_secrete_ici"
     }
   }
   ```

   > 💡 **Note** : Pour tester l'application sans payer réellement, utilisez les cartes de test Stripe :
   > - Carte de succès : `4242 4242 4242 4242`
   > - Date d'expiration : N'importe quelle date future (ex: `12/25`)
   > - CVC : N'importe quel nombre à 3 chiffres (ex: `123`)

### Étape 6 : Restaurer les packages NuGet

```bash
cd CineReserv
dotnet restore
```

## ⚙️ Configuration

### Structure de la chaîne de connexion MySQL

Format général :
```
Server=localhost;Database=CineReservDB;User=votre_utilisateur;Password=votre_mot_de_passe;
```

Exemple complet :
```
Server=localhost;Database=CineReservDB;User=cinereservuser;Password=MonMotDePasse123!;
```

### Paramètres MySQL courants

- **Server** : `localhost` (si MySQL est sur la même machine)
- **Database** : `CineReservDB` (nom de votre base de données)
- **User** : `root` ou `cinereservuser` (selon ce que vous avez créé)
- **Password** : Votre mot de passe MySQL

## 🚀 Exécution

### Méthode 1 : Via Visual Studio

1. **Ouvrir** le fichier `CineReserv.sln` dans Visual Studio 2022

2. **Vérifier** que le projet `CineReserv` est défini comme projet de démarrage :
   - Clic droit sur `CineReserv` → **Définir comme projet de démarrage**

3. **Appuyer sur F5** ou cliquer sur le bouton **Exécuter**

4. **Attendre** que l'application démarre :
   - La première fois, les migrations Entity Framework vont créer les tables automatiquement
   - La base de données sera peuplée avec des données de test (films, séances, etc.)

5. **L'application** s'ouvrira automatiquement dans votre navigateur :
   - URL par défaut : `https://localhost:XXXX` ou `http://localhost:XXXX`

### Méthode 2 : Via la ligne de commande

1. **Ouvrir** un terminal dans le dossier `CineReserv`

2. **Exécuter** les migrations de base de données :
   ```bash
   dotnet ef database update
   ```
   > ⚠️ **Note** : Si cette commande échoue, installez d'abord les outils EF Core :
   > ```bash
   > dotnet tool install --global dotnet-ef
   > ```

3. **Lancer** l'application :
   ```bash
   dotnet run
   ```

4. **Ouvrir** votre navigateur et aller à l'URL affichée (généralement `https://localhost:5001` ou `http://localhost:5000`)

### Vérification de l'installation

Une fois l'application lancée, vous devriez voir :
- ✅ La page d'accueil avec les films disponibles
- ✅ Un menu de navigation en haut
- ✅ Les options de connexion/inscription

## 📁 Structure du projet

```
CineReserv/
├── Controllers/          # Contrôleurs MVC
│   ├── AuthController.cs      # Authentification (inscription, connexion)
│   ├── FilmsController.cs    # Gestion des films et réservations
│   ├── PanierController.cs    # Gestion du panier
│   ├── PaymentController.cs  # Paiements Stripe
│   ├── ReservationsController.cs  # Gestion des réservations
│   ├── DashboardController.cs     # Tableau de bord fournisseur
│   └── FacturationController.cs   # Facturation
├── Models/               # Modèles de données
│   ├── ApplicationUser.cs    # Utilisateur (Client/Fournisseur)
│   ├── Film.cs              # Film
│   ├── Seance.cs            # Séance de projection
│   ├── Reservation.cs       # Réservation
│   ├── Facture.cs           # Facture
│   ├── Siege.cs             # Siège dans la salle
│   ├── Salle.cs             # Salle de cinéma
│   └── PanierItem.cs        # Article du panier
├── Views/                # Vues Razor
│   ├── Auth/             # Pages d'authentification
│   ├── Films/            # Pages de films
│   ├── Panier/           # Pages du panier
│   ├── Payment/          # Pages de paiement
│   └── Dashboard/        # Tableau de bord
├── Data/                 # Contexte de base de données
│   └── ApplicationDbContext.cs
├── Services/             # Services métier
│   └── ApiService.cs     # Service de peuplement des données
├── Migrations/           # Migrations Entity Framework
├── wwwroot/              # Fichiers statiques (CSS, JS, images)
├── Program.cs            # Point d'entrée de l'application
└── appsettings.json     # Configuration (chaîne de connexion, Stripe)
```

## 🛠️ Technologies utilisées

- **ASP.NET Core 8.0** - Framework web
- **Entity Framework Core 8.0** - ORM pour la base de données
- **MySQL** - Base de données (via Pomelo.EntityFrameworkCore.MySql)
- **ASP.NET Core Identity** - Authentification et gestion des utilisateurs
- **Stripe.NET** - Intégration des paiements électroniques
- **Bootstrap 5** - Framework CSS
- **jQuery** - Manipulation DOM et AJAX

## 🔍 Résolution de problèmes

### Problème : Erreur de connexion à MySQL

**Symptômes** : `Unable to connect to any of the specified MySQL hosts`

**Solutions** :
1. Vérifier que MySQL Server est démarré :
   - Windows : Services → MySQL80 → Démarrer
   - Ou via la ligne de commande : `net start MySQL80`

2. Vérifier la chaîne de connexion dans `appsettings.json`

3. Vérifier que l'utilisateur MySQL existe et a les droits :
   ```sql
   SELECT user, host FROM mysql.user;
   SHOW GRANTS FOR 'cinereservuser'@'localhost';
   ```

### Problème : Migration échoue

**Symptômes** : `dotnet ef database update` échoue

**Solutions** :
1. Installer les outils EF Core :
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. Vérifier que la base de données existe :
   ```sql
   CREATE DATABASE IF NOT EXISTS CineReservDB;
   ```

3. Supprimer et recréer la base de données (⚠️ **perte de données**):
   ```sql
   DROP DATABASE CineReservDB;
   CREATE DATABASE CineReservDB;
   ```
   Puis relancer : `dotnet ef database update`

### Problème : Erreur Stripe

**Symptômes** : Le paiement échoue même avec des cartes de test

**Solutions** :
1. Vérifier que les clés Stripe sont correctes dans `appsettings.json`
2. Vérifier que vous utilisez des clés en mode **test** (`pk_test_...` et `sk_test_...`)
3. Utiliser une carte de test Stripe : `4242 4242 4242 4242`

### Problème : Port déjà utilisé

**Symptômes** : `Failed to bind to address`

**Solutions** :
1. Fermer d'autres instances de l'application
2. Modifier le port dans `Properties/launchSettings.json`
3. Ou tuer le processus qui utilise le port :
   ```bash
   # Windows
   netstat -ano | findstr :5000
   taskkill /PID <PID> /F
   ```

## 📝 Notes importantes

- ⚠️ **Sécurité** : Ne commitez jamais votre fichier `appsettings.json` avec vos vraies clés API et mots de passe dans un dépôt public
- 💡 **Développement** : Utilisez `appsettings.Development.json` pour vos configurations locales
- 🔐 **Production** : Utilisez Azure Key Vault, User Secrets, ou des variables d'environnement pour stocker les secrets en production

## 📄 Licence

Ce projet est sous licence MIT.

## 👤 Auteur

- GitHub : [@Seck2000](https://github.com/Seck2000)

## 🙏 Remerciements

- Stripe pour l'API de paiement
- .NET Foundation pour ASP.NET Core
- La communauté open source

---

**Bon développement ! 🎬**

