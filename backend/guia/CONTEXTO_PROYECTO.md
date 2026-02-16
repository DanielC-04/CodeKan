# DevBoard - Contexto Maestro del Proyecto (Backend)

## Que es DevBoard
DevBoard es un sistema Kanban con sincronizacion bidireccional con GitHub Issues. Permite gestionar tareas en un tablero (Todo, InProgress, Done) y mantenerlas sincronizadas con un repositorio GitHub en tiempo real.

## Objetivo funcional
- Crear y gestionar proyectos Kanban vinculados a repositorios GitHub.
- Crear tareas locales que se reflejan como Issues en GitHub.
- Cambiar estado de tareas y reflejar esos cambios en GitHub (por ejemplo cerrar issue al pasar a Done).
- Permitir reapertura de issue al mover tarea de `Done` a `InProgress`.
- Recibir Webhooks de GitHub para aplicar cambios externos al estado local.
- Notificar clientes conectados en tiempo real usando SignalR cuando haya cambios externos.

## Stack oficial
- Frontend: Angular 21 (Standalone Components, Signals, Typed Forms, NgRx).
- Backend: ASP.NET Core Web API (.NET 9).
- Persistencia: SQL Server + EF Core (Code First).
- Tiempo real: SignalR.
- Integracion GitHub: Octokit.

## Arquitectura
Se aplica Clean/Onion Architecture con dependencias dirigidas hacia adentro:

1. `DevBoard.Domain`
   - Entidades, Value Objects, reglas de negocio e invariantes.
   - Sin dependencias de infraestructura.

2. `DevBoard.Application`
   - Casos de uso, DTOs, contratos (interfaces), validaciones de aplicacion.
   - Depende solo de Domain.

3. `DevBoard.Infrastructure`
   - EF Core, DbContext, repositorios, integraciones externas (Octokit), cifrado.
   - Implementa contratos de Application.

4. `DevBoard.Api`
   - Endpoints HTTP, SignalR Hub, middleware y configuracion de host.
   - Orquesta Application/Infrastructure sin contener logica de negocio.

## Modelo de dominio planificado
### Project
- Id
- Name
- RepoOwner
- RepoName
- GitHubTokenEncrypted
- CreatedAt

### Task
- Id
- ProjectId
- Title
- Status (`Todo`, `InProgress`, `Done`)
- GitHubIssueNumber (nullable)
- CreatedAt
- CompletedAt (nullable)

## Seguridad definida
- El token de GitHub NO se guarda en plano.
- Se cifra en Infrastructure y se persiste como `GitHubTokenEncrypted`.
- El token nunca se devuelve en respuestas API.
- El token nunca debe aparecer en logs.
- La API usa JWT Bearer para endpoints protegidos.
- El refresh token se maneja en cookie `HttpOnly`.
- El registro publico crea usuarios con rol `Member` de forma fija.
- El rol `Admin` se reserva para gestion interna del sistema.

## Contrato de respuesta API
Todos los endpoints devolveran `ApiResponse<T>`:

```json
{
  "success": true,
  "data": {},
  "message": "Operacion exitosa"
}
```

## Endpoints MVP previstos
- `GET /api/health`
- `POST /api/projects`
- `GET /api/projects`
- `GET /api/projects/{projectId}`
- `PUT /api/projects/{projectId}`
- `DELETE /api/projects/{projectId}`
- `POST /api/projects/{projectId}/tasks`
- `GET /api/projects/{projectId}/tasks`
- `GET /api/tasks/{taskId}`
- `PATCH /api/tasks/{taskId}/status`
- `GET /api/tasks/{taskId}/issue-details`
- `GET /api/tasks/{taskId}/issue-comments`
- `PUT /api/tasks/{taskId}`
- `DELETE /api/tasks/{taskId}`
- `POST /api/webhooks/github`
- `GET /hubs/devboard/negotiate` (handshake SignalR)

## Flujo funcional completo del proyecto
1. Usuario crea un Project vinculado a un repo GitHub.
2. Usuario crea una Task en ese Project.
3. Backend crea un Issue en GitHub y guarda `GitHubIssueNumber`.
4. Usuario mueve Task de columna en Kanban.
5. Si Task llega a Done, backend cierra el Issue en GitHub.
6. Si Task vuelve de `Done` a `InProgress`, backend reabre el Issue en GitHub.
6. GitHub envia webhooks por cambios externos (ej. issue cerrada manualmente en GitHub).
7. Backend parsea webhook, actualiza Task local y emite `TaskUpdated` por SignalR.
8. Frontend escucha `TaskUpdated`, despacha accion NgRx y actualiza UI al instante.

## Flujo de usuario en DevBoard (simple)
- El usuario crea un `Project` dentro de DevBoard para un desarrollo concreto.
- Ese `Project` pertenece a la app y se vincula a un repositorio GitHub (owner/repo).
- Dentro del `Project`, el usuario crea `Tasks` en el tablero Kanban.
- Cada `Task` se sincroniza con una `Issue` de GitHub.
- Si se cierra la issue en GitHub, la tarea local pasa a `Done`.
- Si se reabre la issue en GitHub, la tarea local pasa a `InProgress`.

## Que son los webhooks y para que se usan
Un webhook es un aviso automatico que un sistema externo envia a nuestra API cuando ocurre un evento.
En este proyecto, GitHub envia esos avisos cuando cambia una issue (por ejemplo, cuando se cierra o se reabre).

Se usan para mantener DevBoard sincronizado en tiempo real con GitHub, sin tener que estar consultando manualmente.

## Como funcionan los webhooks en DevBoard
1. Ocurre un evento en GitHub (por ejemplo `issues.closed` o `issues.reopened`).
2. GitHub envia una solicitud HTTP `POST` a `POST /api/webhooks/github`.
3. El backend valida que la solicitud sea legitima usando la firma (`X-Hub-Signature-256`) y el `WebhookSecret`.
4. El backend procesa el payload del evento.
5. Busca la tarea local relacionada por repositorio + numero de issue (`GitHubIssueNumber`).
6. Actualiza la tarea local:
   - `issues.closed` -> `Done`
   - `issues.reopened` -> `InProgress`
7. Emite `TaskUpdated` por SignalR para actualizar clientes conectados en tiempo real.
8. Registra el `X-GitHub-Delivery` para evitar reprocesar el mismo evento (idempotencia).

## Beneficio para el usuario
Gracias a webhooks, los cambios hechos en GitHub se reflejan automaticamente en el tablero DevBoard.
Esto evita desincronizacion entre equipo tecnico (GitHub) y gestion visual del trabajo (Kanban).

## Politica de cierre por commit (GitHub nativo)
- Se adopta el comportamiento nativo de GitHub para cierre de issues por commit.
- Formato recomendado de commit: `fixes #<numero>` o `closes #<numero>`.
- GitHub cierra la issue automaticamente y el webhook `issues` sincroniza DevBoard.
- No se implementa parsing custom de commits en backend.

## Comandos usados para verificacion (resumen)
Levantar API:
```bash
dotnet run --project backend/src/DevBoard.Api
```

Verificar health:
```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:5067/api/health" | ConvertTo-Json -Depth 6
```

Levantar ngrok:
```bash
ngrok http 5067
```

Crear project:
```powershell
$projectBody = @{
  name = "Sandbox"
  repoOwner = "TU_OWNER"
  repoName = "TU_REPO"
  gitHubToken = "TU_TOKEN"
} | ConvertTo-Json

$projectResp = Invoke-RestMethod -Method Post -Uri "http://localhost:5067/api/projects" -ContentType "application/json" -Body $projectBody
$projectId = $projectResp.data.id
```

Crear task:
```powershell
$taskBody = @{ title = "Prueba sync real" } | ConvertTo-Json
$taskResp = Invoke-RestMethod -Method Post -Uri "http://localhost:5067/api/projects/$projectId/tasks" -ContentType "application/json" -Body $taskBody
$taskId = $taskResp.data.id
$issueNumber = $taskResp.data.gitHubIssueNumber
```

Consultar task:
```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:5067/api/tasks/$taskId" | ConvertTo-Json -Depth 6
```

Validacion tecnica:
```bash
dotnet build "backend/DevBoard.slnx"
dotnet test "backend/DevBoard.slnx"
```

## Convenciones de implementacion
- Controllers delgados; logica en Application.
- Validacion de entrada con FluentValidation.
- Errores centralizados via middleware global.
- Sin `console.log` ni `alert` en frontend.
- Sin dependencias inversas entre capas.

## Estado actual
- Solucion y estructura base creadas en `backend/`.
- Proyectos por capa, DI base y endpoint `GET /api/health` operativos.
- Entidades de dominio `Project` y `Task` implementadas con invariantes.
- Enum `TaskStatus` y excepcion de dominio implementados.
- `ApplicationDbContext` y configuraciones EF Core listos.
- Migracion inicial de base de datos creada y aplicable via `dotnet ef database update`.
- Contratos de Application (`ApiResponse<T>`, DTOs, interfaces) implementados.
- Endpoints MVP base de Projects y Tasks implementados.
- Cifrado de token de GitHub implementado con Data Protection.
- FluentValidation configurado para requests de Projects/Tasks.
- Middleware global de excepciones implementado y controllers simplificados sin `try/catch` repetido.
- Integracion Octokit implementada para crear/cerrar issues.
- Politica de consistencia fuerte activa: si GitHub falla, se bloquea la operacion local.
- Webhook GitHub implementado con parsing via private DTOs y validacion de firma HMAC SHA-256.
- Idempotencia de webhooks implementada con `WebhookDeliveries` (delivery id unico).
- SignalR implementado (`/hubs/devboard`) y emision de evento `TaskUpdated` al procesar cambios externos.
- Regla definida: `issues.reopened` sincroniza la tarea local a `InProgress`.
- Suite de pruebas mockeadas de `TaskService` implementada para validar consistencia fuerte y compensaciones.
- Suite de pruebas HTTP de webhook implementada con `WebApplicationFactory` para validar firma, idempotencia y transiciones.
- Validacion real contra GitHub completada (close/reopen issue reflejado en estado local esperado).
- Comandos de verificacion documentados dentro de este archivo.
- Autenticacion JWT implementada con usuarios locales (`Users`) y sesiones refresh (`RefreshTokens`).
- Endpoints `Projects`, `Tasks` y `DevBoardHub` protegidos con autenticacion.
- `POST /api/webhooks/github` permanece anonimo y validado por firma de GitHub.
- Endpoint de consulta de detalles/comentarios de issue GitHub disponible para frontend.

## Roadmap corto
- Sprint 1: base arquitectura + dominio + EF Core + endpoints MVP.
- Sprint 2: Octokit + webhook con private DTOs + SignalR `TaskUpdated`.
- Sprint 3: hardening, pruebas de integracion completas, observabilidad.
