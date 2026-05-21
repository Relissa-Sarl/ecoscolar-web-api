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
- **TUnit**: A modern unit testing framework for .NET. Tests are written using standard attributes and run via the .NET CLI (`dotnet test`) or Test Explorer.

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

# Setup strip secret key
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

#### Test User Credentials
Use the following credentials to test the application:

- **Email**: `albert@einstein.ch`
- **Password**: `P@ssw0rd!`

### Step 4: Start the program