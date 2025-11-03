# CineReserv - Système de Réservation de Cinéma

Application web ASP.NET Core pour la réservation de billets de cinéma avec paiement en ligne via Stripe.

## 📋 Table des matières

- [Description](#description)
- [Fonctionnalités](#fonctionnalités)
- [Prérequis](#prérequis)
- [Installation](#installation)
- [Configuration](#configuration)
- [Exécution](#exécution)
- [Structure du projet](#structure-du-projet)

## 📖 Description

CineReserv permet aux clients de :
- Rechercher et réserver des places de cinéma
- Payer en ligne via Stripe
- Consulter leurs réservations et factures

Les fournisseurs peuvent :
- Voir leurs statistiques (revenus, nombre de clients, etc.)
- Consulter leurs factures

## ✨ Fonctionnalités

### Pour les clients :
- Recherche de films par genre
- Réservation de billets (choix de la catégorie : Enfant, Général, Aîné)
- Sélection des sièges dans la salle
- Panier de réservations
- Paiement en ligne avec Stripe
- Consultation des réservations et factures

### Pour les fournisseurs :
- Tableau de bord avec statistiques (revenus totaux, places vendues, taux d'occupation, clients actifs)
- Consultation des factures
- Statistiques de facturation

## 🔧 Prérequis

1. **.NET 8.0 SDK** 
   - Télécharger : https://dotnet.microsoft.com/download/dotnet/8.0
   - Vérifier : `dotnet --version`

2. **MySQL Server 8.0 ou plus**
   - Télécharger : https://dev.mysql.com/downloads/mysql/

3. **Visual Studio 2022** ou **Visual Studio Code**

## 📥 Installation

### Étape 1 : Cloner le projet

```bash
git clone https://github.com/Seck2000/CineReserv.git
cd CineReserv
```

### Étape 2 : Installer MySQL

1. Télécharger et installer MySQL Server
2. Noter le mot de passe root (nécessaire pour la suite)

### Étape 3 : Créer la base de données

1. Ouvrir **MySQL Command Line Client** ou **MySQL Workbench**

2. Se connecter avec `root` et votre mot de passe

3. Créer la base de données :
   ```sql
   CREATE DATABASE CineReservDB;
   ```

4. (Optionnel) Créer un utilisateur :
   ```sql
   CREATE USER 'cinereservuser'@'localhost' IDENTIFIED BY 'VotreMotDePasse123!';
   GRANT ALL PRIVILEGES ON CineReservDB.* TO 'cinereservuser'@'localhost';
   FLUSH PRIVILEGES;
   ```

### Étape 4 : Configurer la connexion

Ouvrir `CineReserv/appsettings.json` et modifier :

**Avec l'utilisateur root :**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CineReservDB;User=root;Password=VotreMotDePasseMySQL;"
  }
}
```

**Avec un utilisateur créé :**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CineReservDB;User=cinereservuser;Password=VotreMotDePasse123!;"
  }
}
```

> ⚠️ Remplacer `VotreMotDePasseMySQL` ou `VotreMotDePasse123!` par votre vrai mot de passe MySQL.

### Étape 5 : Configurer Stripe (pour tester)

1. Créer un compte sur https://dashboard.stripe.com/register (mode test)

2. Récupérer les clés API :
   - Clé publique : `pk_test_...`
   - Clé secrète : `sk_test_...`

3. Modifier `CineReserv/appsettings.json` :
   ```json
   {
     "Stripe": {
       "PublishableKey": "pk_test_votre_cle_publique",
       "SecretKey": "sk_test_votre_cle_secrete"
     }
   }
   ```

   > 💡 Pour tester sans payer : utiliser la carte `4242 4242 4242 4242` (expiration future, CVC quelconque)

### Étape 6 : Installer les packages

```bash
cd CineReserv
dotnet restore
```

## 🚀 Exécution

### Avec Visual Studio

1. Ouvrir `CineReserv.sln` dans Visual Studio 2022
2. Appuyer sur **F5** pour démarrer
3. La base de données sera créée automatiquement au premier lancement

### Avec la ligne de commande

1. Ouvrir un terminal dans le dossier `CineReserv`

2. Créer la base de données :
   ```bash
   dotnet ef database update
   ```
   > Si la commande échoue, installer EF Core : `dotnet tool install --global dotnet-ef`

3. Lancer l'application :
   ```bash
   dotnet run
   ```

4. Ouvrir le navigateur à l'URL affichée (généralement `https://localhost:5001`)

## 📁 Structure du projet

```
CineReserv/
├── Controllers/          # Contrôleurs
│   ├── AuthController.cs      # Inscription, connexion
│   ├── FilmsController.cs    # Films et réservations
│   ├── PanierController.cs    # Panier
│   ├── PaymentController.cs  # Paiements Stripe
│   ├── ReservationsController.cs  # Réservations
│   ├── DashboardController.cs     # Tableau de bord fournisseur
│   └── FacturationController.cs   # Factures fournisseur
├── Models/               # Modèles de données
│   ├── ApplicationUser.cs    # Utilisateur
│   ├── Film.cs              # Film
│   ├── Seance.cs            # Séance
│   ├── Reservation.cs       # Réservation
│   ├── Facture.cs           # Facture
│   ├── Siege.cs             # Siège
│   ├── Salle.cs             # Salle
│   └── PanierItem.cs        # Article panier
├── Views/                # Pages web
├── Data/                 # Base de données
│   └── ApplicationDbContext.cs
├── Services/             # Services
│   └── ApiService.cs     # Données initiales
├── Program.cs            # Point d'entrée
└── appsettings.json     # Configuration
```

## 🔍 Résolution de problèmes

### Erreur de connexion MySQL

**Solution** :
1. Vérifier que MySQL Server est démarré (Services Windows → MySQL80)
2. Vérifier la chaîne de connexion dans `appsettings.json`

### Migration échoue

**Solution** :
1. Installer les outils EF Core : `dotnet tool install --global dotnet-ef`
2. Vérifier que la base de données existe : `CREATE DATABASE IF NOT EXISTS CineReservDB;`

### Erreur Stripe

**Solution** :
1. Vérifier que les clés sont en mode test (`pk_test_...` et `sk_test_...`)
2. Utiliser la carte de test : `4242 4242 4242 4242`

## 📝 Notes

- ⚠️ Ne pas commiter `appsettings.json` avec vos vraies clés API dans un dépôt public
- 💡 En développement, utiliser des clés Stripe en mode test
- 🔐 En production, utiliser des variables d'environnement pour les secrets

## 📄 Licence

Ce projet est sous licence MIT.

## 👤 Auteur

- GitHub : [@Seck2000](https://github.com/Seck2000)
