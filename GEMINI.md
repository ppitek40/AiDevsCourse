# AI DEVS Course Solutions Project

## Project Overview

This is a full-stack project designed for implementing and executing tasks for the AI DEVS course. It consists of an ASP.NET Core Web API backend and an Angular frontend. The backend is structured to allow scalable and isolated implementations of various AI tasks, integrating with external AI models via OpenRouter.

### Architecture

The repository is organized as a C# solution (`AiDevs.slnx`) containing the backend projects, along with a separate directory for the frontend application.

#### Backend (.NET)
*   **AiDevs:** The main ASP.NET Core Web API project. It exposes endpoints to execute task solutions (e.g., `POST /api/solutions/{taskId}`).
*   **AiDevs.Core:** Contains core domain interfaces (`ITaskSolution`) and models (`SolutionResult`) that are shared across the application.
*   **AiDevs.Infrastructure:** Handles external service integrations, primarily the `OpenRouterService` for communicating with AI models. Includes function calling support.
*   **AiDevs.Solutions:** The directory where individual task solutions are implemented. Each task is isolated in its own folder (e.g., `Task01`, `Task02`) and implements the `ITaskSolution` interface.
*   **AiDevs.Tests:** Contains architecture and unit tests. Architecture tests ensure task isolation.

#### Frontend (Angular)
*   **AiDevs.Frontend:** An Angular 21 web application designed to interact with the backend. It uses Tailwind CSS for styling and Vitest for unit testing.

## Building and Running

### Backend

1.  **Configuration:** You need to configure your OpenRouter API key. Add it to `AiDevs/appsettings.json` or `AiDevs/appsettings.Development.json` under `OpenRouter:ApiKey`:
    ```json
    {
      "OpenRouter": {
        "ApiKey": "YOUR_ACTUAL_API_KEY"
      }
    }
    ```
2.  **Restore and Build:**
    ```bash
    dotnet restore
    dotnet build
    ```
3.  **Run:**
    ```bash
    cd AiDevs
    dotnet run
    ```
    The API runs on `https://localhost:5001` (and `http://localhost:5000`). Swagger UI is available at `https://localhost:5001/swagger`.

### Frontend

1.  **Install Dependencies:**
    ```bash
    cd AiDevs.Frontend
    npm install
    ```
2.  **Run Development Server:**
    ```bash
    cd AiDevs.Frontend
    npm start
    ```
    The application will be available at `http://localhost:4200/`.

### Testing
*   **Backend:** Run `dotnet test` from the root directory to execute all C# tests (unit and architecture tests).
*   **Frontend:** Run `npm test` inside `AiDevs.Frontend` to execute unit tests using Vitest.

## Development Conventions

*   **Task Isolation:** Each AI DEV task must be implemented in its own isolated folder under `AiDevs.Solutions/` (e.g., `Task03`).
*   **Interface Implementation:** Every new task must implement the `ITaskSolution` interface from `AiDevs.Core.Interfaces`.
*   **Dependency Injection:** Task implementations should be registered in the Dependency Injection container in `AiDevs/Program.cs` as transient services. They can inject `OpenRouterService` or other required services via their constructors.
*   **Standardized Responses:** Tasks should return a `SolutionResult` (defined in `AiDevs.Core.Models`) for consistent API behavior.
*   **AI Integration:** The `OpenRouterService` provides methods for both simple completion (`CompleteAsync`) and chat with history (`ChatAsync`).
*   **Frontend Styling:** Tailwind CSS is used for styling the Angular components.
