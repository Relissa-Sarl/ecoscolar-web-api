# Ecoscolar Web Api

Ecoscolar backend environment setup guide.

---

## Tech Stack & Tools

Here are the main languages and tools used to build this project:

### Languages & Frameworks
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

### Backend & Database
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)

### Testing
- **xUnit** + **FluentAssertions** : tests unitaires et d'intégration
- Lancer via `dotnet test` (CLI) ou l'Explorateur de tests Visual Studio

## Tests

### Commandes générales

```powershell
# Toute la suite de tests
dotnet test EcoScolarWebApi.Tests/EcoScolarWebApi.Tests.csproj

# Tests d'intégration uniquement
dotnet test EcoScolarWebApi.Tests/EcoScolarWebApi.Tests.csproj --filter "FullyQualifiedName~Integration"
```

### Sprint T8 — tests par sous-tâche

| Tâche | Classe | Commande |
|-------|--------|----------|
| **T8-1** Login API | `LoginIntegrationTests` | `dotnet test --filter "FullyQualifiedName~LoginIntegrationTests"` |
| **T8-2** Route JWT | `JwtProtectedRouteIntegrationTests` | `dotnet test --filter "FullyQualifiedName~JwtProtectedRouteIntegrationTests"` |
| **T8-3** Création livre | `BookCreateIntegrationTests` | `dotnet test --filter "FullyQualifiedName~BookCreateIntegrationTests"` |
| **T8-4** Recherche livre | `AdvertSearchServiceIntegrationTests` | `dotnet test --filter "FullyQualifiedName~AdvertSearchServiceIntegrationTests"` |

Les tests T8-1 à T8-3 utilisent **EF InMemory** (pas de Docker requis).  
`AdvertSearchServiceIntegrationTests` (T8-4) utilise aussi InMemory.

#### T8-3 — choix vs `feature/creation-advert-book`

La branche `feature/creation-advert-book` (Bangumiii, Testcontainers + `HttpClient`) **n'est pas mergée** :

- namespaces et chemins API obsolètes par rapport à `develop`
- instabilité Docker/Testcontainers en local/CI
- couverture redondante avec `AdvertTest.cs` (unitaire) + `BookCreateIntegrationTests` (InMemory)

Le flux auth HTTP (login → JWT) est couvert par T8-1/T8-2 ; la création livre par `BookCreateIntegrationTests`.

Exemple avec chemin explicite du projet :

```powershell
dotnet test EcoScolarWebApi.Tests/EcoScolarWebApi.Tests.csproj --filter "FullyQualifiedName~AdvertSearchServiceIntegrationTests"
```

## C# Development & Naming Conventions

Coding practices and naming conventions for the project. Adhering to these rules ensures codebase consistency, readability, and clean architecture.

---

### 1. Data Transfer Objects (DTOs)
* **Standard:** Use **Records** instead of traditional classes for all DTOs.
* **Rationale:** Records are immutable by default, provide built-in value-based equality, and offer a concise syntax that is ideal for passing data between application layers.

### Example:
```csharp
namespace EcoScolarWebApi.DTOs;

// Attributs must me in PascalCase
public record UserDto(int Id, string FirstName, string LastName, string Email);
```

### 2. Namespaces
Use File-Scoped Namespaces in all .cs files.
```csharp
namespace EcoScolarWebApi.DTOs;
```

### 3. Database Models & Entity Framework Configuration
* Standard: * The file name and the class name must always be singular.
* Use the **[Table("PluralName")]** data annotation attribute to explicitly map the singular class to its corresponding pluralized database table.
---

### 4. Interfaces & Service Contracts
Standard: All application interfaces must be grouped together within the Services/Contracts/ directory.

## Features

- **Register** - Create a new user account with email and password
- **Login** - Authenticate users and generate session tokens

---

## Setup & Installation

Follow these steps to set up your local development environment.

### Step 1: Clone the repository

```bash
git clone https://github.com/Relissa-Sarl/ecoscolar-web-api.git
cd ecoscolar-web-api
```

### Step 2: Setup environment variables 
```bash
dotnet user-secrets init

# Setup the connection string (replace {{...}} with your local configuration)
dotnet user-secrets set "ConnectionStrings:Default" "Server={{server_name}};Database=db-ecoscolar;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

# Setup stripe secret key
dotnet user-secrets set "Stripe:SecretKey" "{{sk_here}}"

# Open swagger
http://localhost:5001/swagger/index.html

```

### Step 3: Database Setup with Seeds (Development)

For development purposes, the application includes seed data that populates the database with test users, adverts, books, services, and other entities.

To use the seed data:

1. **Drop the existing database** (if it exists):
   ```powershell
   Drop-Database
   ```
   Run this command in the **Package Manager Console**.

2. **Apply migrations**:
   ```powershell
   Update-Database
   ```
   Run this command in the **Package Manager Console**.

3. **Start the application**:
   ```bash
   dotnet run
   ```
   The seeds will automatically populate the database on startup when running in Development mode.

### Step 4: Create a docker container for mailpit (Development)
To test the email sending functionality, you can use Mailpit, a local SMTP server. Run the following command to start a Mailpit container:

You need to use the ports `1026` for SMTP and `8026` for the web interface because of the configuration in `appsettings.Development.json`:

**This only works in development mode when running the backend locally using the IDE**

*Note: When running the backend using docker, a mailpit container will be integrated with the other containers*

```bash
docker run -d --name mailpit -p 1026:1025 -p 8026:8025 axllent/mailpit
```

#### Test User Credentials
Use the following credentials to test the application:

- **Email**: `albert@einstein.ch`
- **Password**: `P@ssw0rd!`

### Step 4: Start the program

## Tester le flux de paiement (Stripe)

Le checkout est **piloté serveur** : le client n'envoie que des IDs d'annonces, le serveur calcule le prix et la commission, crée une transaction `PENDING`, puis un **webhook Stripe signé** confirme le paiement. Pour tester en local, il faut le [Stripe CLI](https://stripe.com/docs/stripe-cli) (mode test).

### 1. Configurer les secrets

```bash
# Clé secrète de test (Dashboard Stripe → Developers → API keys)
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
```

### 2. Lancer le relais webhook (terminal dédié, à laisser tourner)

```bash
stripe login
stripe listen --forward-to https://localhost:7267/api/v1/payments/webhook --skip-verify
```
`--skip-verify` est nécessaire (certificat HTTPS auto-signé). Copier le `whsec_...` affiché et le configurer :
```bash
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```
> Le `whsec_` à utiliser est celui imprimé par `stripe listen`, **pas** celui du Dashboard. Les user-secrets ne sont lus qu'au démarrage → **redémarrer l'API** après les avoir posés.

### 3. Dérouler un achat

1. Démarrer l'API, ouvrir `https://localhost:7267/swagger`.
2. `POST /api/v1/auth/login` → récupérer l'`accessToken` → bouton **Authorize** (`Bearer <token>`).
3. `POST /api/v1/payments/checkout` avec une annonce **ACTIVE** qui n'est pas la vôtre :
   ```json
   { "productIds": [10], "shippingMethod": "handToHand" }
   ```
   → réponse `{ "url", "orderNumber" }`. En base : une transaction `PENDING`, l'annonce en `PAUSED`.
4. Ouvrir l'`url`, payer avec la carte test `4242 4242 4242 4242` (date future, CVC au hasard).
5. Le terminal `stripe listen` affiche `checkout.session.completed [200]`. En base : transaction `PAID_WAITING_SHIPPING`, `StripePaymentIntentId` rempli, annonce `SOLD`.

> Créer la session ne déclenche **aucun** webhook : l'événement n'est émis qu'au **paiement**.

> ⚠️ **À adapter côté front (`ecoscolar-web-ui`)** : `success.vue` ne doit plus créer les transactions ni passer l'annonce à `SOLD` — l'état vient désormais du webhook. Et le type `CheckoutRequest` ne doit plus envoyer `productPrice` (le serveur l'ignore, le prix est calculé serveur).

## Docker container

This project can be build into a docker container.

The Docker infrastructure uses a `.env` file to manage database credentials and port mapping. You must create this file at the project root (same directory as `docker-compose.yaml`).

Create a `.env` file or modify the `sample.env` one and paste the following configuration:

```env
# ===== NGINX =====
NGINX_PORT=8080

# ===== MSSQL Server =====
MSSQL_SA_PASSWORD=Ec0Scolar!12345

STRIPE_SECRET_KEY=
```

### Step 1: Build the Environment

You can build the container with this command line

```bash
docker compose build
```

### Step 2: Start the container

Since the build is finish, write this command line for start the container
```bash
docker compose up -d
```

### Step 3: Close the container

When you're done using the container, you can write this command line for close the container

```bash
docker compose down
```
