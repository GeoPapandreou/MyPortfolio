# MyPortfolio

MyPortfolio is a full-stack portfolio builder that helps users create a personal portfolio website through a guided workflow. Users sign in, fill out a structured portfolio form, optionally provide a design reference image, and generate a downloadable portfolio package built around their content.

## Tech Stack

- Frontend: React, Vite, Tailwind CSS, React Router, Axios
- Backend: ASP.NET Core Web API, Entity Framework Core, SQL Server
- Authentication: JWT bearer tokens
- AI generation: Google Gemini

## Project Structure

```text
/MyPortfolioUI   Frontend application
/MyPortfolioAPI  Backend API
/sample-data     Example profile payloads for local testing
```

## Current Workflow

1. A user registers or signs in.
2. The user completes the portfolio wizard and saves their answers.
3. The frontend sends the profile to the API.
4. The backend stores the profile, asks Gemini to generate the frontend package, combines it with a generated backend package, and returns a `.zip`.
5. The generated version is also saved to the user's account so it can be downloaded again later.

The generated download file names follow the pattern `MyPortfolio_X.zip`.

## Prerequisites

- Node.js 20+
- npm 10+
- .NET 8 SDK
- SQL Server or SQL Server Express
- A Gemini API key

## Backend Setup

1. Copy `MyPortfolioAPI/appsettings.example.json` to `MyPortfolioAPI/appsettings.Development.json`.
2. Update these values locally:
   - `ConnectionStrings:DefaultConnection`
   - `Jwt:Secret`
   - `Gemini:ApiKey`
3. Restore and run migrations:

```powershell
dotnet restore MyPortfolioAPI/MyPortfolioAPI.csproj
dotnet ef database update --project MyPortfolioAPI
```

4. Start the API:

```powershell
dotnet run --project MyPortfolioAPI
```

The API is configured for `http://localhost:5000` in local development.

## Frontend Setup

1. Copy `MyPortfolioUI/.env.example` to `MyPortfolioUI/.env`.
2. Confirm the API URL:

```env
VITE_API_URL=http://localhost:5000
```

3. Install dependencies and start the frontend:

```powershell
cd MyPortfolioUI
npm install
npm run dev
```

The frontend runs at `http://localhost:5173`.

## Main Features

- Email/password registration and login
- Account settings page for saved personal details
- Multi-step portfolio wizard
- Portfolio draft save/load flow
- AI-generated frontend package using Gemini
- Saved generated versions with later download support
- Optional reference image support for design direction

## Main API Routes

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/account`
- `PUT /api/account`
- `DELETE /api/account`
- `GET /api/portfolio`
- `PUT /api/portfolio`
- `POST /api/portfolio/generate`
- `GET /api/portfolio/versions/{versionId}/download`
- `DELETE /api/account/versions/{versionId}`

## Notes

- `MyPortfolioAPI/appsettings.Development.json` is ignored by git and should stay local.
- Generated artifacts are stored under `MyPortfolioAPI/GeneratedArtifacts/`.
- Frontend generation is AI-only. If Gemini fails, the build request fails instead of falling back to a hardcoded frontend template.

## Sample Data

You can use [graphic-designer-profile.json](sample-data/graphic-designer-profile.json) as example portfolio data when testing or documenting the request shape.
