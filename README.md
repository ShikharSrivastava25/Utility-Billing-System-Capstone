# Utility Billing System

A comprehensive full-stack web application designed to streamline utility billing operations for service providers (Electricity, Water, Gas, Internet, etc.). The system automates meter reading entry, bill generation, payment tracking, and provides detailed reports and analytics.

## 📋 Overview

This system helps utility service providers manage thousands of consumer connections and monthly billing cycles efficiently. It eliminates manual errors, speeds up bill generation, and provides better visibility into outstanding dues and consumption patterns.

### Key Features

- **User & Security Management**: JWT-based authentication with role-based access control (RBAC)
- **Consumer & Connection Management**: Manage consumer accounts and utility connections
- **Meter Reading Management**: Monthly meter reading entry with automatic consumption calculation
- **Automated Billing**: Auto-generate bills based on consumption and tariff rates
- **Payment Tracking**: Record payments and track outstanding balances
- **Reports & Dashboards**: Revenue reports, outstanding dues, and consumption analytics
- **Notifications**: Bill generation and due date reminders

## 🛠️ Technology Stack

### Backend
- **.NET 8.0** - ASP.NET Core Web API
- **Entity Framework Core 8.0** - ORM for database operations
- **SQL Server** - Database
- **JWT Bearer Authentication** - Secure API authentication
- **Swagger/OpenAPI** - API documentation

### Frontend
- **Angular 20.3** - Frontend framework
- **TypeScript** - Programming language
- **Angular Material** - UI component library
- **RxJS** - Reactive programming
- **D3.js** - Data visualization

### Testing
- **xUnit** - Unit testing framework
- **Integration Tests** - API endpoint testing

## 👥 User Roles

The system supports four distinct user roles:

1. **Admin**
   - Manage users, utility types, tariffs, and billing cycles
   - Full system access and configuration

2. **Billing Officer**
   - Enter meter readings
   - Generate bills for consumers

3. **Account Officer**
   - Track payments
   - Monitor outstanding balances

4. **Consumer**
   - View bills and payment history
   - Check consumption details

## 📦 Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js** (v18 or higher) - [Download here](https://nodejs.org/)
- **npm** (comes with Node.js)
- **SQL Server** (Express edition is fine) - [Download here](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- **Visual Studio 2022** or **Visual Studio Code** (recommended)
- **Angular CLI** (will be installed globally during setup)

## 🚀 Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/ShikharSrivastava25/Utility-Billing-System
cd "Utility Billing System"
```

### 2. Database Setup

1. **Install SQL Server** (if not already installed)
2. **Update Connection String**: Open `Backend/UtilityBillingSystem/UtilityBillingSystem/appsettings.json`
3. Update the `DefaultConnection` string with your SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME\\SQLEXPRESS;Database=UtilityBillingDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Replace `YOUR_SERVER_NAME` with your SQL Server instance name (e.g., `localhost` or your computer name).

### 3. Backend Setup

1. Navigate to the backend directory:
```bash
cd Backend/UtilityBillingSystem/UtilityBillingSystem
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Apply database migrations:
```bash
dotnet ef database update
```

If you encounter issues with Entity Framework tools, install them first:
```bash
dotnet tool install --global dotnet-ef
```

4. (Optional) Verify the database was created in SQL Server Management Studio

### 4. Frontend Setup

1. Navigate to the frontend directory:
```bash
cd Frontend/UtilityBillingSystem
```

2. Install Angular CLI globally (if not already installed):
```bash
npm install -g @angular/cli
```

3. Install project dependencies:
```bash
npm install
```

## ▶️ How to Run

### Running the Backend

1. Navigate to the backend project directory:
```bash
cd Backend/UtilityBillingSystem/UtilityBillingSystem
```

2. Run the application:
```bash
dotnet run
```

Or use Visual Studio:
- Open the solution file `UtilityBillingSystem.slnx`
- Press `F5` or click the "Run" button

The API will start on:
- **HTTP**: `http://localhost:5055`
- **HTTPS**: `https://localhost:7251`

3. Access Swagger UI:
   - Open your browser and navigate to: `http://localhost:5055/swagger`
   - This provides interactive API documentation

### Running the Frontend

1. Navigate to the frontend directory:
```bash
cd Frontend/UtilityBillingSystem
```

2. Start the Angular development server:
```bash
ng serve
```

Or:
```bash
npm start
```

3. Access the application:
   - Open your browser and navigate to: `http://localhost:4200`

The frontend is configured to communicate with the backend API running on `http://localhost:5055`.

### Running Both Together

**Option 1: Separate Terminals**
- Open two terminal windows/command prompts
- Run backend in one terminal
- Run frontend in another terminal

**Option 2: PowerShell (Windows)**
```powershell
# Terminal 1
cd "Backend\UtilityBillingSystem\UtilityBillingSystem"
dotnet run

# Terminal 2
cd "Frontend\UtilityBillingSystem"
ng serve
```

## 📁 Project Structure

```
Utility Billing System/
├── Backend/
│   └── UtilityBillingSystem/
│       ├── UtilityBillingSystem/
│       │   ├── Controllers/          # API Controllers
│       │   ├── Services/             # Business logic services
│       │   ├── Models/               # Data models and DTOs
│       │   ├── Data/                 # DbContext and seed data
│       │   ├── Middleware/           # Global exception handling
│       │   ├── Migrations/           # EF Core database migrations
│       │   ├── Program.cs            # Application entry point
│       │   └── appsettings.json      # Configuration
│       └── UtilityBillingSystem.Tests/  # Unit and integration tests
│
└── Frontend/
    └── UtilityBillingSystem/
        ├── src/
        │   └── app/                  # Angular application code
        │       ├── core/             # Core modules (auth, guards)
        │       ├── shared/           # Shared components
        │       └── features/         # Feature modules
        ├── package.json              # Node dependencies
        └── angular.json              # Angular configuration
```

## 🔧 Configuration

### Backend Configuration

Edit `Backend/UtilityBillingSystem/UtilityBillingSystem/appsettings.json`:

- **ConnectionStrings**: Update SQL Server connection string
- **Jwt**: Configure JWT settings (Key, Issuer, Audience)
- **Logging**: Adjust log levels as needed

### Frontend Configuration

The frontend communicates with the backend API. Ensure the API base URL is correctly configured in your Angular services (typically `http://localhost:5055`).

## 🧪 Testing

### Backend Tests

Run unit and integration tests:

```bash
cd Backend/UtilityBillingSystem/UtilityBillingSystem.Tests
dotnet test
```

### Frontend Tests

Run Angular tests:

```bash
cd Frontend/UtilityBillingSystem
ng test
```

## 📚 API Documentation

Once the backend is running, access the Swagger UI at:
- `http://localhost:5055/swagger`

This interactive documentation allows you to:
- View all available endpoints
- Test API calls directly
- See request/response schemas
- Authenticate and test protected endpoints

## 🔐 Default Test Users (Development)

The system includes seed data for development. After running the application and migrations, the following default users are automatically created:

| Role | Email | Password | Full Name | Permissions |
|------|-------|----------|-----------|-------------|
| **Admin** | `admin@example.com` | `Test@123` | Admin User | Full system access - Manage users, utility types, tariffs, and billing cycles |
| **Billing Officer** | `billing@example.com` | `Test@123` | Bill Officer | Enter meter readings and generate bills |
| **Account Officer** | `account@example.com` | `Test@123` | Account Officer | Track payments and monitor outstanding balances |
| **Consumer** | `consumer@example.com` | `Test@123` | Connie Sumer | View bills, payment history, and consumption details |

> **Note:** All default users share the password `Test@123`. These credentials are automatically seeded when the application runs in Development mode. The consumer user has sample connections (Electricity, Water, Internet) with historical meter readings and bills for testing purposes.

## 🐛 Troubleshooting

### Database Connection Issues

- Verify SQL Server is running
- Check the connection string in `appsettings.json`
- Ensure SQL Server allows Windows Authentication or update connection string with SQL credentials
- Try using `localhost` instead of the computer name

### Port Already in Use

- Backend: Change port in `launchSettings.json`
- Frontend: Use `ng serve --port <port-number>`

### Migration Issues

- Delete existing migrations and recreate: `dotnet ef migrations add InitialCreate`
- Ensure database exists: `dotnet ef database update`

### Frontend Dependencies Issues

- Delete `node_modules` folder and `package-lock.json`
- Run `npm install` again

## 📝 Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Angular Documentation](https://angular.io/docs)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

