# Handoff — Frontend DailyTasks (Pendientes Diarios)

> Documento de contexto para un chat dedicado **solo al frontend del módulo DailyTasks**.
> Léelo completo antes de empezar. No modifiques el backend ni el frontend de Inventario.
>
> **Estado (2026-11-01):** la base del frontend ya existe en
> `C:\Users\raul_\OneDrive\Desktop\webCCH\DailyTasks_Camara_Comercio_Huancayo_Frontend`
> y compila con `npm run build`. Continúa sobre ella; **no la recrees desde cero**.
> El resumen de estado está en `HANDOFF_FRONTEND.md` y `README.md` del frontend.

## 1. Objetivo

Construir el frontend del módulo **DailyTasks / Daylist** de la Cámara de Comercio de Huancayo:
pendientes diarios con lista filtrable, tablero Kanban, calendario real, informes, historial
y actualización en tiempo real.

El backend ya está implementado y funcionando. El frontend debe consumir su API real,
respetar permisos y ofrecer una experiencia profesional, accesible y responsive.

## 2. Skills obligatorios

Antes de escribir código, carga y usa estos skills:

- `impeccable` — jerarquía, layout, tipografía, color, motion, UX copy, accesibilidad y pulido.
- `ui-ux-pro-max` — estilos, paletas, tipografías, patrones UX y guías por stack.
- `modern-web-guidance` — APIs y patrones actuales de HTML/CSS/JS (evita prácticas obsoletas).

También puedes apoyarte en `design-system` si necesitas formalizar tokens.

## 3. Rutas del proyecto

| Qué | Ruta |
|---|---|
| Backend (solo lectura) | `C:\Users\raul_\OneDrive\Desktop\webCCH\Web_Camara_Comercio_Huancayo_Operaciones_Backend` |
| Frontend (ya creado) | `C:\Users\raul_\OneDrive\Desktop\webCCH\DailyTasks_Camara_Comercio_Huancayo_Frontend` |
| Frontend Inventario (referencia visual/técnica) | `C:\Users\raul_\OneDrive\Desktop\webCCH\Inventario_Camara_Comercio_Huancayo_Frontend` |

- Backend local: `http://localhost:5095`
- API DailyTasks: `http://localhost:5095/api/dailytasks`
- Hub SignalR: `http://localhost:5095/hubs/dailytasks`
- Frontend dev: `http://localhost:5175` (el backend ya permite CORS en ese puerto)
- Backend separado por módulos: la capa API de DailyTasks vive en
  `DailyTasks/DailyTasks.API`; el host sigue siendo `Inventario/Inventario.API`
  en el puerto `5095`, por lo que las URLs del frontend no cambian.

> **Arquitectura obligatoria:** este frontend debe reutilizar la misma arquitectura,
> convenciones y sistema de diseño del frontend de Inventario. No es un proyecto con
> stack libre.

## 4. Arquitectura y stack (misma base que Inventario)

Este proyecto **no elige un stack libre**: debe replicar la arquitectura del frontend de
Inventario (`C:\Users\raul_\OneDrive\Desktop\webCCH\Inventario_Camara_Comercio_Huancayo_Frontend`).
La referencia es obligatoria para estructura, dependencias, tema, cliente HTTP, auth y layout.

### Base técnica (copiar/adaptar de Inventario)

- React 18.3 + React DOM 18.3.
- Vite 5 + TypeScript 5.5 estricto (`strict: true`, `noEmit: true`, `moduleResolution: "bundler"`).
- MUI 5 (`@mui/material`, `@mui/icons-material`) + Emotion (`@emotion/react`, `@emotion/styled`).
- `@fontsource-variable/manrope` para la tipografía institucional.
- `@tanstack/react-query` v5 para datos, caché, mutaciones y polling.
- `react-router-dom` v6 con rutas lazy (`React.lazy` + `Suspense`).
- Cliente HTTP propio en `src/api/client.ts` basado en `fetch`, con `ApiError`, timeout,
  deduplicación de GET y helper `qs`.
- `AuthContext` + `src/auth/session.ts` con `localStorage` (misma forma que Inventario,
  adaptada a los datos del usuario de DailyTasks).
- Tema MUI centralizado en `src/theme/theme.ts` y tokens en `src/theme/tokens.ts`.
- Componentes comunes en `src/components/common` y layout en `src/components/layout`.
- Iconos de `@mui/icons-material` (no usar lucide ni otra librería de iconos).
- Imports relativos, como en Inventario (no es necesario alias `@/`).
- Formularios controlados con MUI, siguiendo el patrón de Inventario. Si se decide usar
  `react-hook-form` + `zod`, debe ser consistente en toda la app y no duplicar validación.

### Dependencias adicionales permitidas

Solo las necesarias para las funciones nuevas:

- `recharts` para gráficos de informes (por defecto). Si se prefiere el ecosistema MUI,
  validar `@mui/x-charts` con MUI 5 antes de usarlo.
- `@microsoft/signalr` para tiempo real.
- `@dnd-kit/core` + `@dnd-kit/sortable` para el Kanban accesible.
- `date-fns` + `date-fns-tz` para fechas de Perú (`America/Lima`).

No agregues Tailwind, shadcn/ui, Radix, lucide-react, sonner ni otro sistema de diseño.

### Scripts esperados

Los mismos scripts de Inventario:

```json
{
  "dev": "vite",
  "dev:https": "vite --mode https",
  "build": "tsc --noEmit && vite build",
  "preview": "vite preview"
}
```

Configura en `vite.config.ts`:

- `server.host: true`
- `server.port: 5175`
- `server.strictPort: true`
- Proxy `/api` → `http://localhost:5095`
- Proxy `/hubs` → `http://localhost:5095` con `ws: true` (SignalR)

No agregues `test`/`lint` si no vas a configurar Vitest/ESLint; mantén la simplicidad de
Inventario.

## 5. Variables de entorno

Igual que Inventario, en desarrollo se usan rutas relativas y Vite hace proxy al backend
local. Archivo `.env.development`:

```env
VITE_API_URL=/api/dailytasks
VITE_HUB_URL=/hubs/dailytasks
```

Plantilla `.env.example`:

```env
# Desarrollo: Vite hace proxy a http://localhost:5095 (ver vite.config.ts)
# Producción: https://api.tu-dominio.com/api/dailytasks
VITE_API_URL=/api/dailytasks
VITE_HUB_URL=/hubs/dailytasks
```

En producción:

```env
VITE_API_URL=https://api.tu-dominio.com/api/dailytasks
VITE_HUB_URL=https://api.tu-dominio.com/hubs/dailytasks
```

El cliente HTTP debe replicar el comportamiento de Inventario: si `VITE_API_URL` apunta a
`localhost` pero la app se abrió desde otro equipo (celular), usa la ruta relativa
`/api/dailytasks` para que Vite haga el proxy. El hub debe usar `VITE_HUB_URL`; en
desarrollo, `/hubs/dailytasks` viaja por el proxy con `ws: true`.

## 6. Autenticación

- Login: `POST /api/dailytasks/auth/login`
- Body: `{ "username": "milagros", "password": "..." }`
- Response:
  ```json
  {
    "token": "jwt...",
    "user": {
      "id": 1,
      "username": "milagros",
      "fullName": "Milagros",
      "area": "GESTION_ASOCIADO",
      "areaName": "GESTIÓN AL ASOCIADO",
      "role": "USER"
    }
  }
  ```
- Todas las demás rutas requieren `Authorization: Bearer <token>`.
- `GET /api/dailytasks/auth/me` valida la sesión y devuelve el usuario.
- `POST /api/dailytasks/auth/change-password`
  - Body: `{ "currentPassword": "...", "newPassword": "..." }`
  - Respuesta: `204 No Content`.
- El token es propio de DailyTasks; no sirve para Inventario.
- Replica el patrón de Inventario:
  - `src/auth/session.ts` con `getSession`, `setSession`, `clearSession` y
    `localStorage` bajo la clave `dailytasks_session`.
  - `src/auth/AuthContext.tsx` con `session`, `login` y `logout`; en login/logout,
    `queryClient.clear()` para no reutilizar datos de otra sesión.
  - `src/auth/ProtectedRoute.tsx`: si no hay sesión, `<Navigate to="/login" replace />`.
- La sesión debe incluir los datos necesarios para permisos:

  ```ts
  export interface Session {
    token: string;
    id: number;
    username: string;
    name: string;       // fullName
    area: string;
    areaName: string;
    role: 'USER' | 'MANAGER';
  }
  ```

- `login()` debe guardar `token` + `user` del response en esa forma.
- En cada arranque, valida con `/auth/me`; si falla con `401`, limpia la sesión.
- El cliente HTTP (`src/api/client.ts`) debe adjuntar `Authorization: Bearer <token>`
  desde `getSession()`, igual que Inventario.
- `401` → cerrar sesión y redirigir a `/login`.
- `403` → mantener sesión y mostrar mensaje de permisos.
- `429` → mostrar mensaje de rate limit en login.
- Agrega helpers de permisos (por ejemplo `src/auth/permissions.ts` o un hook
  `usePermissions`):
  - `isManager = session?.role === 'MANAGER'`
  - `canEditTask(task) = isManager || task.assignedToId === session?.id`
  - `canAssign = isManager`
  - `canDelete = isManager`
  - `canRestore = isManager`
  - `canSeeGlobalReports = isManager`

### Usuarios de prueba

Los 11 usuarios existen en la base local. Las contraseñas están en el `.env` del backend
(no se incluyen aquí). Usuarios clave:

| Usuario | Rol | Área |
|---|---|---|
| `milagros` | USER | GESTION_ASOCIADO |
| `klaus` | MANAGER | GERENCIA_GENERAL |

El resto: `brizeth`, `arely`, `maria`, `maribel`, `johonny`, `patricia`, `ginger`,
`carmen`, `german` (todos USER, cada uno con su área).

## 7. Roles y permisos

El backend valida permisos; el frontend debe ocultar o deshabilitar acciones no permitidas.

| Acción | USER | MANAGER |
|---|---|---|
| Ver lista, tablero, calendario, resumen | Sí | Sí |
| Ver historial | Solo de sus pendientes | De todos |
| Crear pendiente | Sí (se asigna a sí mismo) | Sí (puede asignar a cualquiera) |
| Editar título/descripción/prioridad/fecha | Solo si `assignedToId === user.id` | Cualquiera |
| Cambiar estado | Solo si `assignedToId === user.id` | Cualquiera |
| Reasignar responsable | No | Sí |
| Eliminar (soft delete) | No | Sí |
| Restaurar | No | Sí |
| Ver eliminados (`includeDeleted=true`) | No | Sí |
| Informe global `/reports` | No | Sí |
| Informe propio `/reports/me` | Sí | Sí |
| Resumen general `/summary` | Sí | Sí |

Nota: los usuarios regulares pueden **ver** todos los pendientes, pero solo pueden
**modificar** los suyos y consultar el historial de sus propias tareas. Gerencia General
puede consultar el historial de cualquier pendiente.

## 8. Enums y catálogos

```ts
export type DailyTaskStatus = 'Pending' | 'InProgress' | 'Done';
export type DailyTaskPriority = 'Low' | 'Medium' | 'High';
export type DailyTaskRole = 'USER' | 'MANAGER';
```

Etiquetas UI sugeridas:

| Enum | Español |
|---|---|
| `Pending` | Pendiente |
| `InProgress` | En progreso |
| `Done` | Completado |
| `Low` | Baja |
| `Medium` | Media |
| `High` | Alta |
| `USER` | Usuario |
| `MANAGER` | Gerencia General |

Áreas (`code` → nombre):

| Code | Nombre |
|---|---|
| `GESTION_ASOCIADO` | GESTIÓN AL ASOCIADO |
| `TESORERIA` | TESORERÍA |
| `PROTESTOS_MORAS` | PROTESTOS Y MORAS |
| `MARKETING` | MARKETING |
| `SECRETARIA` | SECRETARÍA |
| `LOGISTICA` | LOGÍSTICA |
| `CORTE_ARBITRAJE` | CORTE DE ARBITRAJE |
| `CONTABILIDAD` | CONTABILIDAD |
| `FIDELIZACION_1` | FIDELIZACIÓN 1 |
| `FIDELIZACION_2` | FIDELIZACIÓN 2 |
| `GERENCIA_GENERAL` | GERENCIA GENERAL |

Los enums se serializan como **strings** (no números).

## 9. Contrato de la API

Base: `VITE_API_URL` (`/api/dailytasks`).

### 9.1 Autenticación

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/auth/login` | Login, devuelve token + usuario |
| GET | `/auth/me` | Usuario autenticado |
| POST | `/auth/change-password` | Cambiar contraseña |

### 9.2 Tareas

| Método | Ruta | Descripción | Rol |
|---|---|---|---|
| GET | `/tasks` | Lista paginada con filtros | Todos |
| POST | `/tasks` | Crear pendiente | Todos |
| GET | `/tasks/{id}` | Detalle | Todos |
| PUT | `/tasks/{id}` | Editar título/descripción/prioridad/fecha | Dueño o MANAGER |
| PATCH | `/tasks/{id}/status` | Cambiar estado | Dueño o MANAGER |
| PATCH | `/tasks/{id}/assignee` | Reasignar | MANAGER |
| DELETE | `/tasks/{id}` | Soft delete | MANAGER |
| POST | `/tasks/{id}/restore` | Restaurar | MANAGER |
| GET | `/tasks/{id}/history` | Historial/auditoría | Todos |
| GET | `/tasks/board` | Tablero Kanban | Todos |
| GET | `/tasks/calendar` | Calendario | Todos |
| GET | `/tasks/changes` | Cambios para polling | Todos |

#### Query params de `/tasks`, `/tasks/board`, `/tasks/calendar`

| Param | Tipo | Notas |
|---|---|---|
| `status` | `Pending\|InProgress\|Done` | Opcional |
| `priority` | `Low\|Medium\|High` | Opcional |
| `assignedToId` | number | Opcional |
| `area` | string | Código de área; se normaliza a mayúsculas |
| `createdFrom` | `YYYY-MM-DD` | Opcional |
| `createdTo` | `YYYY-MM-DD` | Opcional |
| `dueFrom` | `YYYY-MM-DD` | Opcional |
| `dueTo` | `YYYY-MM-DD` | Opcional |
| `dueDate` | `YYYY-MM-DD` | Filtro exacto por vencimiento |
| `overdue` | boolean | Vencidos calculados |
| `q` | string | Busca en título y descripción |
| `updatedSince` | ISO datetime | Opcional |
| `page` | number | Default 1 |
| `pageSize` | number | Default 20, máximo 100 |
| `sortBy` | `dueDate\|priority\|status\|title` | Default `createdAt` |
| `sortDir` | `asc\|desc` | Default `asc` |
| `includeDeleted` | boolean | Solo MANAGER |

`/tasks/calendar` además acepta `from` y `to` (`YYYY-MM-DD`). Si no se envían, usa el mes actual.

`/tasks/changes` acepta `since` (ISO datetime). Si no se envía, usa las últimas 24 horas.
Devuelve tareas modificadas y `deletedTaskIds` para retirar tarjetas.

#### Bodies

Crear:

```json
{
  "title": "Actualizar padrón de asociados",
  "description": "Revisar altas y bajas del mes.",
  "priority": "High",
  "dueDate": "2026-11-05",
  "assignedToId": 2
}
```

- `title` obligatorio, máximo 200.
- `description` opcional, máximo 2000.
- `priority` opcional; default `Medium`.
- `dueDate` opcional; default hoy (Perú).
- `assignedToId` opcional. USER solo puede enviar su propio id o omitirlo. MANAGER puede asignar a cualquiera.
- El estado inicial siempre es `Pending`.

Editar:

```json
{
  "title": "Nuevo título",
  "description": "Nueva descripción",
  "priority": "Medium",
  "dueDate": "2026-11-06"
}
```

Cambiar estado:

```json
{ "status": "Done" }
```

Reasignar:

```json
{ "assignedToId": 3 }
```

### 9.3 Catálogos

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/users?area=...` | Usuarios activos; `area` opcional |
| GET | `/areas` | Áreas disponibles |

### 9.4 Informes

| Método | Ruta | Descripción | Rol |
|---|---|---|---|
| GET | `/summary` | Resumen general | Todos |
| GET | `/reports` | Informe global | MANAGER |
| GET | `/reports/me` | Informe propio | Todos |
| GET | `/audit` | Auditoría global de cambios | MANAGER |

Query params de `/audit`:

| Param | Tipo | Notas |
|---|---|---|
| `from` | `YYYY-MM-DD` | Fecha inicial en Perú |
| `to` | `YYYY-MM-DD` | Fecha final en Perú |
| `userId` | number | Usuario que realizó el cambio |
| `taskId` | number | Tarea específica |
| `action` | string | `CREATED`, `UPDATED`, `STATUS_CHANGED`, `ASSIGNED`, `DELETED`, `RESTORED` |
| `field` | string | `Title`, `DueDate`, `AssignedTo`, `Status`, etc. |
| `q` | string | Busca en título, valor anterior o valor nuevo |
| `sensitiveOnly` | boolean | Solo `Title`, `DueDate` y `AssignedTo` |
| `page` / `pageSize` | number | Paginación; máximo 100 |

`/audit` usa la tabla existente `TaskHistory`; no requiere una tabla nueva.

Query params de `/reports` y `/reports/me`:

| Param | Tipo | Notas |
|---|---|---|
| `from` | `YYYY-MM-DD` | Default hoy - 29 días |
| `to` | `YYYY-MM-DD` | Default hoy |
| `granularity` | `day\|week\|month` | Default `day` |
| `userId` | number | Solo MANAGER en `/reports`; ignorado en `/reports/me` |
| `area` | string | Código de área |
| `status` | enum | Opcional |
| `priority` | enum | Opcional |

El rango máximo es 366 días.

## 10. DTOs (TypeScript)

```ts
export type DailyTaskStatus = 'Pending' | 'InProgress' | 'Done';
export type DailyTaskPriority = 'Low' | 'Medium' | 'High';
export type DailyTaskRole = 'USER' | 'MANAGER';

export interface DailyTaskUser {
  id: number;
  username: string;
  fullName: string;
  area: string;
  areaName: string;
  role: DailyTaskRole;
}

export interface DailyTaskArea {
  code: string;
  name: string;
}

export interface DailyTask {
  id: number;
  code: string;              // "DT-12"
  title: string;
  description: string | null;
  status: DailyTaskStatus;
  priority: DailyTaskPriority;
  area: string;
  areaName: string;
  assignedToId: number;
  assignedToName: string;
  createdById: number;
  createdByName: string;
  createdAt: string;         // ISO UTC
  createdOn: string;         // YYYY-MM-DD (Perú)
  dueDate: string;           // YYYY-MM-DD (Perú)
  updatedAt: string;         // ISO UTC
  completedAt: string | null;// ISO UTC
  completedOn: string | null;// YYYY-MM-DD (Perú)
  isOverdue: boolean;
  daysOverdue: number;
  isDeleted: boolean;
  deletedAt: string | null;
  deletedByName: string | null;
}

export interface DailyTaskCard {
  id: number;
  code: string;
  title: string;
  status: DailyTaskStatus;
  priority: DailyTaskPriority;
  area: string;
  areaName: string;
  assignedToId: number;
  assignedToName: string;
  dueDate: string;
  isOverdue: boolean;
  daysOverdue: number;
}

export interface DailyTaskHistoryEntry {
  id: number;
  changedAt: string;         // ISO UTC
  changedById: number;
  changedByName: string;
  changeGroupId: string;     // GUID
  action: string;            // CREATED, UPDATED, STATUS_CHANGED, ASSIGNED, DELETED, RESTORED
  field: string | null;      // Title, Description, Priority, DueDate, Status, AssignedTo
  oldValue: string | null;
  newValue: string | null;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DailyTaskBoard {
  pending: DailyTaskCard[];
  inProgress: DailyTaskCard[];
  done: DailyTaskCard[];
}

export interface DailyTaskCalendarDay {
  date: string;              // YYYY-MM-DD
  tasks: DailyTaskCard[];
}

export interface DailyTaskCalendar {
  from: string;
  to: string;
  days: DailyTaskCalendarDay[];
}

export interface DailyTaskChanges {
  tasks: DailyTask[];
  deletedTaskIds: number[];
}

export interface CountByStatus {
  status: DailyTaskStatus;
  count: number;
}

export interface CountByPriority {
  priority: DailyTaskPriority;
  count: number;
}

export interface CountByArea {
  area: string;
  areaName: string;
  count: number;
}

export interface DailyTaskSummary {
  total: number;
  pending: number;
  inProgress: number;
  done: number;
  overdue: number;
  dueToday: number;
  completionRate: number;    // porcentaje 0..100
  byStatus: CountByStatus[];
  byPriority: CountByPriority[];
  byArea: CountByArea[];
}

export interface DailyTaskReportMetrics {
  total: number;
  pending: number;
  inProgress: number;
  done: number;
  overdue: number;
  dueToday: number;
  completionRate: number;
  createdInPeriod: number;
  completedInPeriod: number;
}

export interface DailyTaskReportSeriesPoint {
  periodStart: string;       // YYYY-MM-DD
  created: number;
  completed: number;
}

export interface DailyTaskReportByUser {
  userId: number;
  fullName: string;
  area: string;
  areaName: string;
  total: number;
  pending: number;
  inProgress: number;
  done: number;
  overdue: number;
  completionRate: number;
  createdInPeriod: number;
  completedInPeriod: number;
}

export interface DailyTaskReportByArea {
  area: string;
  areaName: string;
  total: number;
  done: number;
  overdue: number;
  completionRate: number;
}

export interface DailyTaskReport {
  from: string;
  to: string;
  granularity: 'day' | 'week' | 'month';
  metrics: DailyTaskReportMetrics;
  series: DailyTaskReportSeriesPoint[];
  byUser: DailyTaskReportByUser[];
  byArea: DailyTaskReportByArea[];
  byStatus: CountByStatus[];
  byPriority: CountByPriority[];
}

export interface DailyTaskAuditEntry {
  id: number;
  changedAt: string;
  changedById: number;
  changedByName: string;
  changeGroupId: string;
  action: string;
  field: string | null;
  oldValue: string | null;
  newValue: string | null;
  taskId: number;
  taskCode: string;
  taskTitle: string;
  area: string;
  areaName: string;
  assignedToId: number;
  assignedToName: string;
}

export interface DailyTaskAuditQuery {
  from?: string;
  to?: string;
  userId?: number;
  taskId?: number;
  action?: string;
  field?: string;
  q?: string;
  sensitiveOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export interface LoginResponse {
  token: string;
  user: DailyTaskUser;
}
```

## 11. Tiempo real (SignalR)

- Instala `@microsoft/signalr`.
- Implementa un hook `useDailyTasksHub` (o `src/api/signalr.ts`) con el ciclo de vida
  atado a la sesión, siguiendo el patrón de hooks de Inventario.
- Resuelve la URL del hub así:

  ```ts
  const configured = import.meta.env.VITE_HUB_URL ?? '/hubs/dailytasks';
  const hubUrl = configured.startsWith('http')
    ? configured
    : `${window.location.origin}${configured}`;
  ```

- Conecta enviando el token como `access_token` en query string:

  ```ts
  new HubConnectionBuilder()
    .withUrl(`${hubUrl}?access_token=${token}`)
    .withAutomaticReconnect()
    .build();
  ```

- En desarrollo, `/hubs` debe estar proxificado por Vite con `ws: true` hacia
  `http://localhost:5095`; así funciona sin CORS y sin exponer el backend.
- El backend acepta `access_token` en query string **solo** para `/hubs/dailytasks`.
- Eventos emitidos:

| Evento | Payload |
|---|---|
| `taskCreated` | `DailyTask` |
| `taskUpdated` | `DailyTask` |
| `taskStatusChanged` | `DailyTask` |
| `taskAssigned` | `DailyTask` |
| `taskDeleted` | `{ taskId: number }` |
| `taskRestored` | `DailyTask` |

Al recibir un evento:

- Invalida queries de TanStack Query usando las claves centralizadas `qk` de
  `src/api/queryClient.ts`: `tasks`, `board`, `calendar`, `summary`, `reports`,
  `history` del id afectado.
- O actualiza la caché de forma optimista con el payload (upsert por `id`).
- En `taskDeleted`, retira el id de listas, tablero y calendario.
- Evita duplicados por `id`.
- En `onreconnected`, refetch de las queries visibles.
- Si SignalR no conecta, activa polling de respaldo.

### Polling de respaldo

- Cada 20–30 s: `GET /tasks/changes?since=<ultimoSyncISO>`.
- Usa el timestamp UTC del último poll exitoso menos 30 s de margen.
- Aplica `tasks` como upsert y elimina `deletedTaskIds`.
- Detén el polling cuando SignalR esté conectado.

## 12. Pantallas y UX

### 12.1 Login

- Branding institucional de la Cámara de Comercio de Huancayo.
- Campos usuario y contraseña, mostrar/ocultar, estado de carga, error claro.
- Manejo de `401` y `429`.
- Enlace discreto a soporte o ayuda si aplica.

### 12.2 Shell de la aplicación

- Sidebar en desktop, drawer o bottom nav en móvil.
- Navegación: Inicio/Resumen, Lista, Tablero, Calendario, Informes.
- Topbar: búsqueda global (si aplica), indicador de conexión en tiempo real,
  menú de usuario con nombre, área, rol, cambiar contraseña y cerrar sesión.
- Mostrar claramente cuando el usuario es Gerencia General.

### 12.3 Inicio / Resumen

- Tarjetas KPI: total, pendientes, en progreso, completados, vencidos, para hoy.
- Porcentaje de cumplimiento con visual claro.
- Gráficos: por estado, por prioridad, por área.
- “Mi día”: pendientes del usuario para hoy y vencidos.
- Próximos vencimientos.
- Acciones rápidas: crear pendiente, ver tablero, ver calendario.

### 12.4 Lista de pendientes

- Filtros: estado, prioridad, área, responsable, rango de fechas, vencidos, búsqueda.
- Ordenamiento y paginación.
- Tabla en desktop; tarjetas en móvil.
- Columnas: código, título, estado, prioridad, área, responsable, vencimiento,
  vencido, acciones.
- Acciones: ver/editar, cambiar estado, reasignar (MANAGER), historial,
  eliminar/restaurar (MANAGER).
- Botón “Nuevo pendiente”.
- Vista de eliminados para MANAGER (`includeDeleted=true`).

### 12.5 Tablero Kanban

- 3 columnas: Pendiente, En progreso, Completado.
- Contador por columna.
- Tarjeta: código, título, prioridad, área, responsable, vencimiento,
  badge de vencido (`isOverdue`, `daysOverdue`).
- Drag-and-drop entre columnas con `@dnd-kit`, solo si el usuario puede cambiar el estado.
- Actualización optimista + rollback si falla.
- Filtros compartidos con la lista.
- En móvil: columnas con scroll horizontal o tabs por estado.

### 12.6 Calendario

- Calendario mensual real con navegación anterior/siguiente/hoy.
- Celdas con indicadores de tareas por día (puntos, badges o mini-lista).
- Clic en un día:
  - abre un panel lateral con las tareas de ese día;
  - acción “Ver en tablero” que filtra el tablero por `dueDate`.
- Usa `GET /tasks/calendar?from=...&to=...` y los filtros activos.
- Manejo de días fuera del mes, feriados si se desea, y responsive.

### 12.7 Informes

- Filtros: rango de fechas, granularidad, área, estado, prioridad, usuario (MANAGER).
- KPIs: total, creados en el periodo, completados en el periodo, vencidos,
  para hoy, porcentaje de cumplimiento.
- Gráfico de línea/área: creados vs. completados por periodo.
- Barras/donuts: por estado, prioridad y área.
- Tabla por usuario (MANAGER) con métricas y porcentaje.
- Para USER: usar `/reports/me`; no mostrar filtro de usuario ni informe global.
- Exportar a CSV/PDF es opcional; si se implementa, que sea claro y accesible.

### 12.8 Historial / auditoría

- Drawer o modal por tarea.
- Timeline agrupado por `changeGroupId`.
- Cada entrada: usuario, fecha/hora (Perú), acción, campo, valor anterior → nuevo.
- Traducir acciones y campos al español.
- Mostrar “— ” para valores nulos.
- Orden descendente por fecha.

### 12.9 Crear / editar pendiente

- Modal o drawer.
- Campos: título, descripción, prioridad, fecha de vencimiento.
- MANAGER: selector de responsable.
- USER: responsable fijo (él mismo), sin selector.
- Validación: título obligatorio (máx. 200), descripción (máx. 2000).
- Fecha por defecto: hoy en Perú.
- Estado inicial: Pendiente (no editable al crear).
- Confirmación y toast al guardar.

### 12.10 Cambiar contraseña

- Modal con contraseña actual, nueva y confirmación.
- Validación mínima 8 caracteres.
- Manejo de error y éxito.

### 12.11 Estados vacíos, carga y error

- Skeletons en listas, tablero, calendario e informes.
- Empty states con ilustración/icono y CTA.
- Errores con mensaje claro y acción de reintentar.
- Toasts para éxito/error.
- `aria-live` para anuncios.

## 13. Dirección visual (misma identidad que Inventario)

- **Base visual obligatoria:** reutiliza `src/theme/tokens.ts` y `src/theme/theme.ts` de
  Inventario. No inventes una paleta nueva.
- **Colores institucionales:**
  - `brand.indigo = #1D1F6E`
  - `brand.navy = #131347`
  - `brand.navyDeep = #0C0C2E`
  - `brand.gold = #BA9907`
  - `brand.goldLight = #D8B84A`
  - `brand.goldSoft = #F5EDCF`
  - Neutros y semánticos: `colors.bg`, `colors.surface`, `colors.border`,
    `colors.text`, `colors.textSecondary`, `colors.success`, `colors.warning`,
    `colors.error`, `colors.info` y sus variantes `*Soft`.
- **Tipografía:** Manrope Variable vía `@fontsource-variable/manrope`, con la escala
  tipográfica del tema de Inventario (`h1`–`h6`, `body1`, `body2`, `caption`, `overline`).
- **Layout:** replica `AppLayout`, `AppSidebar`, `AppTopBar`, `MobileNav`, `MobileMenu`
  y `Brand`. Sidebar azul institucional, acento dorado, contenido máximo ~1240 px.
- **Componentes:** usa MUI (`Card`, `Paper`, `Chip`, `Table`, `Dialog`, `Drawer`,
  `TextField`, `Select`, `Tabs`, `Alert`, `Snackbar`, `Skeleton`, etc.) y los componentes
  comunes de Inventario (`PageHeader`, `StatCard`, `EmptyState`, `PageLoader`,
  `SectionHeading`, `ProgressRing`, `StatusChips`).
- **Estados:** define chips/colores para estado y prioridad usando el tema:
  - Pendiente: neutro/azul suave.
  - En progreso: azul/info.
  - Completado: verde/success.
  - Vencido: rojo/error.
  - Prioridad alta: ámbar/warning; media: info; baja: neutro.
- **Personalidad:** institucional, confiable, moderna y humana; no un dashboard genérico.
- **Densidad:** cómoda, con jerarquía clara y tablas legibles.
- **Motion:** transiciones sutiles, respetar `prefers-reduced-motion` (el tema de
  Inventario ya lo contempla).
- **Accesibilidad:** WCAG AA, foco visible, navegación por teclado, labels, contraste,
  tamaños táctiles mínimos 44 px.
- **Responsive:** 360 px, 768 px, 1024 px, 1280 px+.
- **Modo oscuro:** no es prioridad; si se implementa, debe ser completo y con contraste.

Usa `impeccable` para revisar jerarquía, espaciado, microcopy y estados vacíos **dentro**
de la identidad de Inventario. Usa `ui-ux-pro-max` para patrones y refinamiento, no para
cambiar el sistema de diseño. Usa `modern-web-guidance` para APIs actuales compatibles con
React/MUI (container queries, `:has()`, view transitions, etc.), sin reemplazar los
componentes MUI.

## 14. Fechas y zona horaria

- `createdOn`, `dueDate`, `completedOn` son fechas de Perú (`YYYY-MM-DD`).
  Muéstralas como `dd/MM/yyyy` sin convertir a UTC.
- `createdAt`, `updatedAt`, `completedAt`, `changedAt` son UTC.
  Formatea con `America/Lima` (UTC-5).
- `isOverdue` y `daysOverdue` vienen calculados del backend; úsalos.
- La fecha por defecto al crear es hoy en Perú:
  ```ts
  formatInTimeZone(new Date(), 'America/Lima', 'yyyy-MM-dd')
  ```
- No cambies el estado de una tarea vencida automáticamente.

## 15. Manejo de errores HTTP

El backend devuelve `{ "error": "mensaje" }` para errores de negocio.

| Código | Significado | Acción UI |
|---|---|---|
| 400 | Validación / regla de negocio | Mostrar mensaje inline o toast |
| 401 | Sesión inválida/expirada | Limpiar sesión y redirigir a login |
| 403 | Sin permisos | Mantener sesión, toast de permisos |
| 404 | No encontrado | Mensaje y volver |
| 409 | Conflicto de concurrencia | “Recarga e intenta de nuevo” |
| 429 | Rate limit | Mensaje y esperar |
| 500 | Error interno | Mensaje genérico + reintentar |

Centraliza el cliente HTTP, tipa los errores y evita duplicar lógica.

## 16. Estructura (misma organización que Inventario)

```text
src/
  api/
    client.ts        fetch + ApiError + timeout + dedupe + qs (adaptar de Inventario)
    queryClient.ts   QueryClient + claves qk
    auth.ts          login, me, change-password
    tasks.ts         tareas, tablero, calendario, cambios, historial
    reports.ts       summary, reports, reports/me
    catalog.ts       users, areas
    types.ts         DTOs y enums de DailyTasks
    index.ts         re-exportaciones
  auth/
    AuthContext.tsx
    ProtectedRoute.tsx
    session.ts
    permissions.ts   helpers de rol/permisos (opcional)
  components/
    common/          PageHeader, StatCard, EmptyState, PageLoader,
                     SectionHeading, ProgressRing, StatusChips,
                     ConfirmDialog, ...
    layout/          AppLayout, AppSidebar, AppTopBar, MobileNav,
                     MobileMenu, Brand, navItems
  features/
    auth/            LoginPage, ChangePasswordDialog
    dailytasks/
      inicio/        InicioPage
      lista/         ListaPage, TaskFormDialog, TaskDetailDrawer,
                     TaskHistoryDrawer, TaskFilters
      tablero/       TableroPage, KanbanColumn, KanbanCard
      calendario/    CalendarioPage, MonthGrid, DayPanel
      informes/      InformesPage, ReportFilters, ReportCharts
  hooks/
    useDebounced.ts
    useDailyTasksHub.ts
    usePermissions.ts
  routes/
    AppRoutes.tsx
  theme/
    theme.ts
    tokens.ts
  utils/
    format.ts
    labels.ts
    user.ts
```

- Usa **imports relativos**, como Inventario (no alias `@/`).
- Copia de Inventario:
  - `public/brand/logo-cch.png`
  - `public/brand/logo-cch-mark.png`
  - `public/icon-192.png`, `public/icon-512.png`, `public/apple-touch-icon.png`
  - `public/manifest.webmanifest` (actualiza nombre/short_name a DailyTasks)
- Replica el patrón de `index.html` con `lang="es"`, `theme-color="#1D1F6E"`,
  `viewport-fit=cover` y título `DailyTasks CCH`.
- Mantén `tsconfig.json` y `vite.config.ts` alineados con Inventario, cambiando solo
  el puerto (5175), el proxy `/api` y el proxy `/hubs` con `ws: true`.

## 17. Claves de TanStack Query (patrón `qk` de Inventario)

Centraliza las claves en `src/api/queryClient.ts`, igual que Inventario:

```ts
export const qk = {
  me: ['auth', 'me'] as const,
  tasks: (filters: Record<string, unknown> = {}) => ['tasks', filters] as const,
  task: (id: number | string) => ['task', String(id)] as const,
  board: (filters: Record<string, unknown> = {}) => ['board', filters] as const,
  calendar: (from: string, to: string, filters: Record<string, unknown> = {}) =>
    ['calendar', from, to, filters] as const,
  summary: ['summary'] as const,
  reportGlobal: (filters: Record<string, unknown> = {}) =>
    ['report', 'global', filters] as const,
  reportMe: (filters: Record<string, unknown> = {}) =>
    ['report', 'me', filters] as const,
  history: (id: number | string) => ['history', String(id)] as const,
  users: (area?: string) => ['users', area ?? ''] as const,
  areas: ['areas'] as const,
};
```

Usa los mismos defaults de `QueryClient` que Inventario:
`staleTime: 60_000`, `gcTime: 10 * 60_000`, `refetchOnWindowFocus: false`,
`refetchOnReconnect: true`, `retry: 1`, mutaciones con `retry: 0`.

Invalida/actualiza las claves afectadas después de cada mutación. Para drag-and-drop y
cambios de estado, usa actualización optimista con rollback.

## 18. Criterios de aceptación

- Login, logout, persistencia de sesión y `/auth/me` funcionando.
- Lista con todos los filtros, ordenamiento, paginación y acciones por rol.
- Crear, editar y cambiar estado; MANAGER puede asignar, eliminar y restaurar.
- Kanban de 3 columnas con drag-and-drop y guards de permisos.
- Calendario mensual con navegación y clic que filtra el tablero por fecha.
- Informes con KPIs, series y gráficos; global para MANAGER, propio para USER.
- Historial con usuario, fecha/hora, acción, campo y valores anterior/nuevo.
- SignalR actualiza sin recargar; polling de respaldo si falla.
- Responsive y accesible (teclado, foco, contraste, labels).
- `npm run build` pasa sin errores de TypeScript.
- Sin errores en consola.
- README con setup, scripts, variables de entorno y usuarios de prueba.
- No se modifica el backend ni el frontend de Inventario.

## 19. Cómo levantar el backend

```powershell
cd "C:\Users\raul_\OneDrive\Desktop\webCCH\Web_Camara_Comercio_Huancayo_Operaciones_Backend"
dotnet run --project Inventario\Inventario.API
```

- API: `http://localhost:5095`
- Swagger/OpenAPI: si está habilitado, revisar `Program.cs`.
- El `.env` del backend ya tiene la conexión PostgreSQL, claves JWT y los 11 usuarios.
- Las contraseñas de los usuarios están en `.env` (no se incluyen en este documento).

## 20. Orden de implementación recomendado

> **Estado:** los pasos 1–4 ya están implementados y los pasos 5–9 tienen una
> primera versión funcional. La siguiente etapa debe continuar con drag-and-drop,
> acciones de reasignar/eliminar, historial y pulido, sin recrear el proyecto.

1. Copiar la base de Inventario: `package.json`, `vite.config.ts`, `tsconfig.json`,
   `index.html`, `public/` (brand, iconos, manifest), `src/main.tsx`, `src/App.tsx`.
2. Copiar/adaptar `src/theme/tokens.ts` y `src/theme/theme.ts` (misma identidad).
3. Copiar/adaptar `src/api/client.ts`, `src/api/queryClient.ts`, `src/auth/*`,
   `src/components/layout/*` y `src/components/common/*`.
4. Auth: login, sesión, `/auth/me`, ProtectedRoute, cambio de contraseña.
5. Lista: filtros, paginación, crear/editar, cambio de estado, historial.
6. Kanban con `@dnd-kit` y permisos.
7. Calendario mensual y filtro por día.
8. Informes y gráficos con `recharts`.
9. SignalR (`@microsoft/signalr`) + polling de respaldo.
10. Responsive, accesibilidad, estados vacíos, pulido con `impeccable`.
11. `npm run build` y README.

## 21. Reglas para el agente de frontend

- **No modifiques el backend.**
- **No modifiques el frontend de Inventario.** Solo cópialo/consúltalo.
- **Sigue la arquitectura de Inventario:** React 18 + Vite 5 + TypeScript estricto +
  MUI 5 + Emotion + TanStack Query + React Router 6, con la misma estructura de carpetas,
  cliente HTTP, `AuthContext`/`session`, tema/tokens, layout y componentes comunes.
- **No introduzcas Tailwind, shadcn/ui, Radix, lucide-react, sonner ni otro sistema de
  diseño.** Usa MUI y `@mui/icons-material`.
- Usa imports relativos como Inventario (no alias `@/`).
- No inventes endpoints, campos ni enums; usa este documento.
- No incluyas contraseñas ni secretos en el código.
- No uses datos mock si la API real está disponible.
- Mantén el español peruano en la UI.
- Si algo del contrato no está claro, documenta la suposición y continúa.
- Al terminar una etapa, ejecuta `npm run build` y corrige errores.
- Documenta decisiones relevantes en el README del frontend.

## 22. Estado actual de la implementación (2026-11-01)

### Implementado y compilando

- Proyecto Vite + React 18 + TypeScript + MUI 5 + Emotion + Manrope.
- Tema MUI y tokens copiados/adaptados de Inventario.
- Cliente HTTP (`fetch`, `ApiError`, timeout, dedupe, `qs`) con `GET/POST/PUT/PATCH/DELETE`.
- `AuthContext`, `session.ts`, `ProtectedRoute` y helpers de permisos.
- Login, sesión persistente, `/auth/me`, logout y cambio de contraseña.
- Layout responsive: sidebar, topbar, navegación móvil y menú de cuenta.
- Resumen con KPIs y porcentaje de cumplimiento (`/summary`).
- Lista con búsqueda, filtros por estado/prioridad/área/responsable, vencidos,
  orden, paginación y vista de tarjetas en móvil.
- Creación y edición de pendientes con validación local.
- Tablero Kanban de 3 columnas (`/tasks/board`), sin drag-and-drop todavía.
- Calendario mensual real (`/tasks/calendar`) con panel lateral por día y salto al
  tablero filtrado por `dueDate`.
- Informes con KPIs, series creados/completados, gráficos por estado/prioridad y
  tablas por usuario/área (`/reports` y `/reports/me`).
- Papelera con restauración para MANAGER.
- SignalR (`useDailyTasksHub`) con invalidación de caché y polling de respaldo.
- `npm run build` ejecutado correctamente.

### Pendiente para la siguiente etapa

- Drag-and-drop del Kanban con `@dnd-kit` y cambio de estado optimista.
- Acciones de reasignar/eliminar desde lista y tablero, con guards de permisos.
- Historial/auditoría por tarea (drawer con timeline por `changeGroupId`).
- Filtros de fecha y responsable en tablero/calendario.
- Pulido visual y accesibilidad con `impeccable`, `ui-ux-pro-max` y
  `modern-web-guidance`.
- Pruebas de flujos críticos y documentación de usuario.

### Archivos de referencia en el frontend

- `README.md` — setup, scripts, estado y pendientes.
- `HANDOFF_FRONTEND.md` — resumen corto para continuar.
- `src/api/types.ts` — DTOs y enums.
- `src/api/tasks.ts` — endpoints de tareas, tablero, calendario y cambios.
- `src/features/dailytasks/` — vistas del módulo.
- `src/hooks/useDailyTasksHub.ts` — SignalR y polling.
