# Branavan Backend - Command Prompt Validation, Commits and Push

Use Windows Command Prompt from a normal development folder. Do not push
directly to `develop` or `main`.

## 1. Verify the local tools

```cmd
git --version
gh auth status
dotnet --list-sdks
dotnet ef --version
```

The SDK list must contain an `8.0.x` entry. If the EF command is unavailable,
install the repository-compatible CLI tool:

```cmd
dotnet tool install --global dotnet-ef --version 8.0.29
```

Close and reopen Command Prompt if a newly installed command is not found.

## 2. Clone and verify the approved baseline

```cmd
mkdir "%USERPROFILE%\source\repos"
cd /d "%USERPROFILE%\source\repos"
git clone https://github.com/rvabi/EventParkingReservationSystem.git
cd EventParkingReservationSystem
git fetch origin --prune
git switch develop
git pull --ff-only origin develop
git status --short
git rev-parse HEAD
```

For this package, the expected audited `develop` commit is:

```text
4826e0e4b0c035d8ac4319b2d3a5b46f1d311b35
```

Stop if `git status --short` prints anything or the commit is different. The
team repository changed and the package must be checked again before use.

Create the approved feature branch:

```cmd
git switch -c feature/booking-payment-notification
git branch --show-current
```

## 3. Apply the supplied change-only package

Place `Branavan_Backend_Package_CMD.zip` in your Downloads folder. From the
repository root run:

```cmd
tar -xf "%USERPROFILE%\Downloads\Branavan_Backend_Package_CMD.zip" -C .
git status --short
git diff --check
```

The package contains only Branavan's backend changes and documentation. It
does not contain `.git`, frontend files, or copies of unrelated team files.

## 4. Configure local secrets

```cmd
cd EventParkingReservationSystem\EventParking.Api
dotnet user-secrets set "Jwt:Key" "replace-with-a-random-development-key-at-least-32-characters"
```

The tracked default connection uses LocalDB. To use a default local SQL Server
2022 instance instead:

```cmd
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=EventParkingReservationDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

For SQL Server Express, replace `localhost` with `.\SQLEXPRESS`. Use the actual
instance reported by SQL Server Configuration Manager.

Return to the solution folder:

```cmd
cd ..
```

## 5. Restore, build, migrate and test

```cmd
dotnet restore EventParkingReservationSystem.sln
dotnet build EventParkingReservationSystem.sln --no-restore
dotnet ef migrations list --project EventParking.DataAccess --startup-project EventParking.Api
dotnet ef database update --project EventParking.DataAccess --startup-project EventParking.Api
dotnet test EventParkingReservationSystem.sln --no-restore
```

Stop immediately if any command fails. Save the complete error output for
diagnosis; do not commit a known broken phase.

## 6. Run and inspect Swagger

```cmd
dotnet run --project EventParking.Api
```

Open the HTTPS Swagger URL shown in the terminal. Verify the new booking,
payment, receipt and notification endpoints are present. Register or verify a
customer, log in, select **Authorize**, and enter the JWT.

Minimum manual lifecycle:

1. Create a booking containing seats and no parking.
2. Create another booking containing seats and one parking slot.
3. Submit a failed simulated payment and retry successfully before expiry.
4. Confirm a paid booking cannot be cancelled.
5. Download its PDF receipt.
6. Leave a Pending booking unpaid for 15 minutes and confirm it becomes
   Expired within the next one-minute scan.
7. Confirm its seats and optional parking become Available again.
8. Confirm another customer cannot read the first customer's booking,
   payment, receipt or notifications.

Stop the API with `Ctrl+C` before continuing.

## 7. Confirm the Git author

```cmd
cd /d "%USERPROFILE%\source\repos\EventParkingReservationSystem"
git config user.name "branavanloganathan"
git config user.email "branavanloganathan@gmail.com"
git config --get user.name
git config --get user.email
```

## 8. Create five meaningful commits

Run each block only after the build and test checkpoint succeeds.

### Commit 1 - contracts and database migration

```cmd
git add -- EventParkingReservationSystem/EventParking.Models/Entities/BookingSeat.cs EventParkingReservationSystem/EventParking.Models/Enums/NotificationType.cs EventParkingReservationSystem/EventParking.DataAccess/Configurations/BookingSeatConfiguration.cs EventParkingReservationSystem/EventParking.DataAccess/Migrations/20260809093000_AddBookingPriceSnapshot.cs EventParkingReservationSystem/EventParking.DataAccess/Migrations/ApplicationDbContextModelSnapshot.cs EventParkingReservationSystem/EventParking.Business/EventParking.Business.csproj EventParkingReservationSystem/EventParking.Business/DTOs/Bookings EventParkingReservationSystem/EventParking.Business/DTOs/Payments EventParkingReservationSystem/EventParking.Business/DTOs/Notifications EventParkingReservationSystem/EventParking.Business/Options EventParkingReservationSystem/EventParking.Business/Interfaces/IBookingService.cs EventParkingReservationSystem/EventParking.Business/Interfaces/INotificationService.cs EventParkingReservationSystem/EventParking.DataAccess/Interfaces/IBookingRepository.cs EventParkingReservationSystem/EventParking.DataAccess/Interfaces/IRepositoryTransaction.cs
git diff --cached --check
git commit -m "feat: add booking payment notification contracts"
```

### Commit 2 - concurrency-safe data access

```cmd
git add -- EventParkingReservationSystem/EventParking.DataAccess/Repositories/BookingRepository.cs EventParkingReservationSystem/EventParking.DataAccess/Repositories/RepositoryTransaction.cs
git diff --cached --check
git commit -m "feat: add atomic booking repository transactions"
```

### Commit 3 - booking, payment, receipt and notification logic

```cmd
git add -- EventParkingReservationSystem/EventParking.Business/Services/BookingService.cs EventParkingReservationSystem/EventParking.Business/Services/NotificationService.cs
git diff --cached --check
git commit -m "feat: implement booking payment expiry and receipts"
```

### Commit 4 - secured endpoints and expiry worker

```cmd
git add -- EventParkingReservationSystem/EventParking.Api/BackgroundServices EventParkingReservationSystem/EventParking.Api/Controllers/BookingsController.cs EventParkingReservationSystem/EventParking.Api/Controllers/PaymentsController.cs EventParkingReservationSystem/EventParking.Api/Controllers/NotificationsController.cs EventParkingReservationSystem/EventParking.Api/Program.cs EventParkingReservationSystem/EventParking.Api/appsettings.json
git diff --cached --check
git commit -m "feat: expose secured booking lifecycle APIs"
```

### Commit 5 - tests and handoff documentation

```cmd
git add -- EventParkingReservationSystem/EventParking.Tests/Booking EventParkingReservationSystem/docs/booking-payment-notification.md EventParkingReservationSystem/docs/branavan-local-deployment.md
git diff --cached --check
git commit -m "test: cover booking payment cancellation and expiry rules"
```

## 9. Final verification and push

```cmd
git status --short
git log --oneline --decorate -5
cd EventParkingReservationSystem
dotnet build EventParkingReservationSystem.sln --no-restore
dotnet test EventParkingReservationSystem.sln --no-restore
cd ..
git fetch origin --prune
git log --oneline HEAD..origin/develop
```

If the final command prints commits, stop: `develop` changed again. Ask the
team lead before rebasing. If it prints nothing and all checks pass:

```cmd
git push -u origin feature/booking-payment-notification
```

Open a draft pull request from `feature/booking-payment-notification` into
`develop`. Do not merge it yourself unless the team lead explicitly instructs
you to.

## Pull-request evidence checklist

- Successful `dotnet build` and `dotnet test` output.
- Swagger evidence for create, hold status, failed and successful payment,
  cancellation rejection, history and notifications.
- A downloaded PDF receipt.
- Evidence that expiry released seats and parking.
- Concurrent same-seat or same-parking result: one success and one conflict.
- Payment-versus-expiry race result with one consistent final state.
- Migration name and database-update output.
- Link to `docs/booking-payment-notification.md`.
