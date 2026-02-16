# DevBoard

DevBoard es una aplicacion Kanban con sincronizacion bidireccional con GitHub Issues.

- Backend: ASP.NET Core (.NET 9), EF Core, SQL Server, SignalR, JWT + refresh cookie.
- Frontend: Angular 21, NgRx, Tailwind, NG-ZORRO.
- Realtime: actualizacion de tareas por SignalR y webhooks de GitHub.

## Funcionalidades principales

- Registro e inicio de sesion con JWT.
- Creacion de proyectos vinculados a repositorios GitHub.
- Creacion de tareas y sincronizacion con Issues.
- Movimiento de estados Kanban (`Todo`, `InProgress`, `Done`).
- Cierre y reapertura de issue en GitHub segun estado de tarea.
- Webhook de GitHub para sincronizacion de cambios externos.
- Vista de detalle de issue (descripcion, labels, assignees, comentarios read-only).
- Cache de detalles por tarea para mejor rendimiento.

## Estructura del proyecto

```text
DevBoard/
  backend/
    src/
      DevBoard.Domain/
      DevBoard.Application/
      DevBoard.Infrastructure/
      DevBoard.Api/
    tests/
      DevBoard.Domain.Tests/
      DevBoard.IntegrationTests/
  frontend/
    src/
      app/
      environments/
  README.md
```

## Requisitos

- .NET SDK 9
- Node.js 20+ y npm
- SQL Server
- (Opcional) ngrok para pruebas reales de webhook

## Configuracion de entorno

### Backend

1. Copia el archivo de ejemplo:
   - `backend/src/DevBoard.Api/appsettings.Development.example.json`
   - como `backend/src/DevBoard.Api/appsettings.Development.json`
2. Ajusta:
   - `ConnectionStrings:DevBoardDb`
   - `GitHub:WebhookSecret`
   - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`

### Frontend

Crea los archivos locales desde los ejemplos:

- `frontend/src/environments/environment.example.ts` -> `environment.ts`
- `frontend/src/environments/environment.development.example.ts` -> `environment.development.ts`
- `frontend/src/environments/environment.production.example.ts` -> `environment.production.ts`

## Ejecucion local

### 1) Backend

```bash
dotnet run --project backend/src/DevBoard.Api
```

Health check:

```bash
GET http://localhost:5067/api/health
```

### 2) Frontend

```bash
cd frontend
npm install
npm run start
```

App:
- `http://localhost:4200`

## Build y tests

### Backend

```bash
dotnet build backend/DevBoard.slnx
dotnet test backend/DevBoard.slnx
```

### Frontend

```bash
cd frontend
npm run build
npm run test -- --watch=false
```

## Flujo GitHub/Webhook (resumen)

1. Crear proyecto en DevBoard con `repoOwner`, `repoName` y token.
2. Crear tarea desde Kanban -> se crea issue en GitHub.
3. Cambiar estado:
   - `Done` -> cierra issue
   - `Done -> InProgress` -> reabre issue
4. Cambios hechos en GitHub (close/reopen/edit) llegan por webhook y actualizan DevBoard.
5. SignalR notifica a clientes en tiempo real.

## Seguridad

- El token de GitHub se cifra en backend.
- El refresh token se maneja en cookie HttpOnly.
- No se versionan archivos locales sensibles (`appsettings.Development.json`, `environment*.ts` locales).

## Documentacion interna

- Contexto backend: `backend/guia/CONTEXTO_PROYECTO.md`
- Contexto frontend: `frontend/guia/CONTEXTO_FRONTEND.md`
