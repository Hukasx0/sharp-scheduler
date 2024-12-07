# Sharp scheduler

Sharp-Scheduler is a web application built with ASP.NET and Angular for scheduling and executing terminal commands using [Quartz.NET](https://www.quartz-scheduler.net/). It provides an admin interface for managing jobs, setting execution intervals, and viewing detailed logs. The application supports anti-brute-force protection and works with a PostgreSQL database.

![webapp screenshot](https://raw.githubusercontent.com/Hukasx0/sharp-scheduler/main/webapp-screenshot.png)

![swagger screenshot](https://raw.githubusercontent.com/Hukasx0/sharp-scheduler/main/swagger-screenshot.png)

## Features
- Admin Authentication with anti-brute-force protection.
- Job Scheduling: Add, edit, and delete scheduled terminal commands.
- Scheduling functionality: Execute commands at defined intervals using cron expressions.
- Execution Logs: Track job execution and view detailed outputs.
- Cross-Platform: Works on both Windows and Linux.
- Automatic Admin account creation: The admin account is created automatically on application startup if it doesn't exist.

## Technologies used
The application is built using the following technologies:
- Frontend: Angular 17
- Backend: .NET 8
- Task Scheduling: Quartz.NET
- ORM: Entity Framework Core
- Database: PostgreSQL
- IDE: Visual Studio

## Requirements
- .NET SDK 8.0 (for backend)
- Node.js (for Angular frontend)
- PostgreSQL (for database)

## Setup Instructions

### 1. Clone the Repository
```sh
git clone https://github.com/Hukasx0/sharp-scheduler.git
cd sharp-scheduler
```

### 2. Configure Database
Update the appsettings.json file with your PostgreSQL connection details:
```json
"ConnectionStrings": {
    "DatabaseConnection": "Host=localhost;Port=5432;Database=SharpScheduler;Username=postgres;Password=root"
}
```

### 3. Set JWT Secret Key and Admin Credentials
In **appsettings.json** (sharp-scheduler/sharp-scheduler.Server), update the JWT key and admin credentials:
```json
"Jwt": {
    "Key": "your-very-secret-key-here-which-is-at-least-32-characters-long",
    "Issuer": "sharp_scheduler",
    "Audience": "sharp_scheduler_users"
},
"AdminAccount": {
    "Username": "admin",
    "Password": "admin123"
}
```

On application startup, the admin account specified in appsettings.json will be automatically created if it doesn't already exist in the database.
If the admin account already exists, this step will be skipped.

No manual steps are required to create the admin account.

### 4. Apply Database Migrations
Run the following commands to apply migrations using Entity Framework Core:
```sh
cd sharp-scheduler.Server/ 
dotnet ef database update
```
If dotnet ef is not installed, you can install it with:
```sh
dotnet tool install --global dotnet-ef
```

### 5. Install Dependencies
```sh
# Backend
cd sharp-scheduler.Server/ 
dotnet restore

# Exit the server folder
cd ..

# Frontend
cd sharp-scheduler.client/
npm install
```

### 6. Run the Application
Start the backend and frontend servers:
```sh
# Backend
cd sharp-scheduler.Server/ 
dotnet run

# Frontend
cd sharp-scheduler.client/
npm start
```

### 7. Access the App
The application will be available at:
```http
http://localhost:4200
```

# License
This project is licensed under the [MIT License](https://github.com/Hukasx0/sharp-scheduler/blob/main/LICENSE).
