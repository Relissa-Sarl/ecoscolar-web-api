# Ecoscolar Web Api

Purchasing process prototype's backend environment setup guide.

---

## Tech Stack & Tools

Here are the main languages and tools used to build this project:

### Languages & Frameworks
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

### Backend & Database
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)

---

## Features

- **Create a Stripe Checkout** - Initialize payment sessions for purchasing transactions
- **Create a Transfer** - Process fund transfers between Stripe accounts
- **Create a Connected Account** - Set up connected Stripe accounts for vendors/merchants
- **Create an Account Link** - Generate setup links for connected account onboarding

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
dotnet user-secrets set "Stripe:SecretKey" "sk_here"
```

### Step 3: Start the program