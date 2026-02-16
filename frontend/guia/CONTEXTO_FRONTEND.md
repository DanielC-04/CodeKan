# DevBoard Frontend - Contexto Maestro

## Objetivo del frontend
Construir la SPA de DevBoard en Angular 21 para gestionar tablero Kanban sincronizado con backend .NET 9 y GitHub Issues en tiempo real.

## Stack y decisiones cerradas
- Angular 21 standalone.
- TypeScript estricto (`strict`).
- Tailwind CSS para estilos.
- NG-ZORRO (locale `es_ES`) como libreria de componentes UI.
- NgRx para estado global.
- Signals para estado local de componentes.
- Angular CDK para drag and drop y utilidades de accesibilidad.
- SignalR (`@microsoft/signalr`) para tiempo real.
- Auth con JWT (access token) + refresh token en cookie HttpOnly (backend).

## Estructura objetivo
- `src/app/core`: auth, api, interceptors, guards, signalr, ui, store base.
- `src/app/features/auth`: login/register.
- `src/app/features/kanban`: board, task-card, flujos de tareas.
- `src/app/shared`: componentes y utilidades compartidas.
- `src/app/layout`: shell principal de la app.

## Flujo funcional principal
1. Usuario inicia sesion en frontend.
2. Front obtiene access token y opera con endpoints protegidos.
3. Usuario crea y mueve tareas en Kanban.
4. Backend sincroniza con GitHub Issues.
5. Webhooks externos actualizan backend.
6. Front recibe `TaskUpdated` por SignalR y actualiza el store en caliente.

## Politica de autenticacion frontend
- Access token se usa en header `Authorization: Bearer`.
- Refresh token no se guarda en frontend; se maneja por cookie HttpOnly.
- Interceptor de auth refresca sesion automaticamente en `401`.
- Rutas privadas protegidas con guard.
- El formulario de registro no permite seleccionar rol; backend asigna `Member`.
- `Admin` es rol interno de operacion del sistema.

## Politica de persistencia local (local store)
Se permite guardar solo estado no sensible:
- preferencias UI,
- filtros/orden,
- proyecto activo,
- snapshot de estado Kanban para rehidratacion.

No guardar:
- refresh token,
- secretos,
- datos sensibles de seguridad.

## Roadmap de implementacion

### Fase 0 - Bootstrap
- Crear app Angular 21.
- Configurar Tailwind.
- Instalar NG-ZORRO.

### Fase 1 - Core y Auth
- ApiService, UiService, interceptores globales.
- AuthService + AuthInterceptor + AuthGuard.
- Pantallas login/register.

### Fase 2 - Estado y persistencia
- NgRx slices (`auth`, `kanban`, `ui`).
- Effects de carga y sincronizacion.
- Local store para estado no sensible.

### Fase 3 - Kanban
- Board y task-card.
- Drag and drop con CDK.
- Integracion con endpoints protegidos.

### Fase 4 - Tiempo real
- Conexion SignalR al hub backend.
- Manejo de evento `TaskUpdated` en store.

### Fase 5 - Calidad y cierre
- Accesibilidad WCAG AA.
- Lint, tests y build en verde.
- Validacion E2E funcional con backend real.

## Criterios de terminado frontend
- Auth estable con refresh por cookie.
- Kanban operativo y sincronizado con backend.
- Actualizacion en tiempo real funcionando.
- Estado rehidratable en recarga.
- UI responsive y accesible.

## Estado actual
- Tailwind CSS configurado y aplicado en estilos globales.
- Capa core inicial creada (`api`, `auth`, `interceptors`, `guard`, `signalr`, `ui`, `storage`).
- Ruteo base implementado con rutas de auth y shell protegido.
- Paginas base creadas: `login`, `register`, `kanban`.
- Auth frontend conectado al backend JWT con refresh por cookie HttpOnly.
- Persistencia local inicial implementada para sesion de frontend y estado no sensible.
- NG-ZORRO configurado en locale espanol para formularios, layout y mensajeria UI.
- Store NgRx Kanban activo con effects, cache local por proyecto y actualizacion optimista.
- Kanban conectado a SignalR para evento `TaskUpdated` y actualizacion en tiempo real.
- Detalle de issue GitHub integrado en UI (descripcion, labels, assignees, comentarios read-only).
- Panel de issue con tabs y cache por tarea para mejorar rendimiento al reabrir.
- Cobertura de pruebas frontend extendida para Kanban y tarjetas.
