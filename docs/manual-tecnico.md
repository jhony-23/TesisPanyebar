# Manual técnico

## Sistema digital para la administración del servicio de agua potable de la Aldea Panyebar

### 1. Identificación del documento

Este documento corresponde a la documentación técnica de la versión preparada
para la Fase 6 del trabajo de graduación.

El baseline funcional certificado es:

`fe5dbf9` — `feat(qr): include current responsible obligations`

Este commit representa el cierre certificado de la Fase 5. La documentación de
la Fase 6 se desarrolla posteriormente y conserva ese commit como punto de
referencia, sin alterar la historia del repositorio.

### 2. Propósito del manual

El manual proporciona la información técnica necesaria para comprender la
arquitectura, la configuración, el despliegue, el mantenimiento y la
recuperación del sistema. Su contenido describe el estado implementado y las
configuraciones observables en el repositorio; no implica que exista un
despliegue definitivo en producción.

### 3. Alcance técnico del sistema

El sistema implementado es una aplicación web responsiva compuesta por un
frontend React y una API/backend ASP.NET Core, con persistencia relacional en
SQL Server. La primera versión requiere comunicación con el servidor para
operar; no implementa una aplicación móvil nativa ni funcionamiento offline.

El alcance funcional respaldado por el código incluye:

- autenticación y autorización administrativa mediante JWT y permisos;
- gestión administrativa de personas, sectores, suministros y responsables;
- identificación de suministros mediante NIS y código QR;
- cuotas y obligaciones;
- jornadas comunitarias, participantes, asistencia y obligaciones derivadas;
- pagos y aplicaciones de pagos;
- finanzas y egresos;
- dashboard y reportes;
- programación y procesos de abastecimiento;
- administración del Comité;
- auditoría de operaciones administrativas.

Estas capacidades se encuentran representadas por los servicios de aplicación,
los controladores, las entidades del dominio y los conjuntos de entidades del
contexto de persistencia. El detalle de cada flujo operativo debe consultarse
en el código y en sus contratos específicos.

### 4. Arquitectura general

#### Backend

La solución .NET se organiza en los siguientes proyectos:

- **Panyebar.Api**: aplicación web que configura el host ASP.NET Core, los
  controladores, Swagger/OpenAPI, CORS, autenticación JWT y autorización.
- **Panyebar.Application**: servicios y contratos de aplicación para los
  módulos funcionales del sistema.
- **Panyebar.Domain**: entidades y tipos propios del dominio del sistema.
- **Panyebar.Infrastructure**: persistencia con Entity Framework Core y SQL
  Server, registro de dependencias, seguridad y adaptadores de infraestructura.

La descripción anterior se limita a las responsabilidades observables en los
proyectos y en su configuración; no presupone patrones arquitectónicos que no
estén demostrados por el repositorio.

El contexto `PanyebarDbContext` expone entidades para personas, sectores,
suministros, relaciones de responsables, cuotas, obligaciones, jornadas,
participaciones, pagos, aplicaciones de pagos, egresos, usuarios y permisos,
administración del Comité, auditoría, abastecimiento, procesos de suministro y
solicitudes de nuevo servicio. Las configuraciones de persistencia se aplican
desde el ensamblado de infraestructura y la base de datos se configura con el
proveedor de SQL Server.

#### Frontend

El frontend se organiza mediante React y Vite. Sus directorios principales son:

- `app`: composición y configuración de la aplicación;
- `assets`: recursos estáticos;
- `components`: componentes reutilizables de interfaz;
- `config`: configuración del frontend, incluida la base de la API;
- `pages`: páginas de la aplicación;
- `services`: comunicación con los servicios backend;
- `utils`: utilidades compartidas.

El frontend se comunica con la API mediante solicitudes HTTP. Puede utilizar la
variable `VITE_API_BASE_URL`; cuando no se configura, la base utilizada es
`/api`. En desarrollo, Vite define un proxy de `/api` hacia
`http://localhost:5166`. Esta configuración corresponde al entorno local y no
determina necesariamente la configuración que se utilizará en producción.

### 5. Stack tecnológico

Las versiones siguientes se obtienen de `global.json`, los archivos de proyecto
del backend, `frontend/package.json` y el proyecto de pruebas.

| Área     | Tecnología o paquete                   | Versión o configuración declarada                           |
| -------- | -------------------------------------- | ----------------------------------------------------------- |
| Backend  | .NET                                   | `net8.0`                                                    |
| Backend  | ASP.NET Core                           | Incluido mediante el SDK web y `Microsoft.AspNetCore.App`   |
| Backend  | Entity Framework Core                  | `8.0.0`                                                     |
| Backend  | SQL Server                             | Proveedor `Microsoft.EntityFrameworkCore.SqlServer` `8.0.0` |
| Backend  | JWT Bearer                             | `Microsoft.AspNetCore.Authentication.JwtBearer` `8.0.0`     |
| Backend  | Swashbuckle.AspNetCore                 | `6.6.2`                                                     |
| Frontend | React                                  | `^19.2.8`                                                   |
| Frontend | React DOM                              | `^19.2.8`                                                   |
| Frontend | React Router DOM                       | `^7.18.3`                                                   |
| Frontend | Tailwind CSS                           | `^4.3.3`                                                    |
| Frontend | Vite                                   | `^8.2.0`                                                    |
| Frontend | qrcode.react                           | `^4.2.0`                                                    |
| Frontend | Oxlint                                 | `^1.75.0`                                                   |
| Pruebas  | xUnit                                  | `2.5.3`                                                     |
| Pruebas  | Microsoft.NET.Test.Sdk                 | `17.8.0`                                                    |
| Pruebas  | Microsoft.EntityFrameworkCore.InMemory | `8.0.0`                                                     |
| Pruebas  | coverlet.collector                     | `6.0.0`                                                     |

El proyecto backend tiene como framework objetivo `net8.0`. Las versiones con
prefijo `^` corresponden a rangos declarados por el gestor de paquetes del
frontend, no a una afirmación de que el entorno local tenga una versión
distinta instalada.

El archivo `global.json` solicita el SDK `8.0.424` y establece
`rollForward: latestPatch`; por ello, un patch posterior compatible puede ser
aceptado por esa configuración. .NET 10 no forma parte del framework objetivo
del sistema.

### 6. Estructura general del repositorio

La estructura relevante del repositorio es:

```text
backend/
  Panyebar.sln
  src/
    Panyebar.Api/
    Panyebar.Application/
    Panyebar.Domain/
    Panyebar.Infrastructure/
  tests/
    Panyebar.Security.Tests/
frontend/
  src/
    app/
    assets/
    components/
    config/
    pages/
    services/
    utils/
docs/
global.json
```

En `backend/src` se encuentran los cuatro proyectos de la solución y en
`backend/tests` el proyecto de pruebas observado. `frontend/src` contiene el
código React, sus páginas, componentes, servicios y utilidades. Los directorios
generados o de salida `node_modules`, `bin`, `obj` y `dist` no son componentes
arquitectónicos del sistema y no forman parte de esta estructura técnica.

### 7. Entornos y configuración general

#### Entorno de desarrollo

El entorno local observado está compuesto por:

- frontend ejecutado con Vite;
- backend ejecutado con ASP.NET Core;
- SQL Server Express local como proveedor de persistencia;
- `PublicWeb:BaseUrl` configurado para el frontend local;
- proxy de Vite para enrutar `/api` hacia el backend local.

La configuración de desarrollo contiene parámetros locales para estos servicios.
Los secretos, claves JWT y cadenas de conexión sensibles no forman parte de
este manual.

#### Entorno de entrega

El entorno de entrega queda pendiente de definición y configuración durante la
Fase 6. En este bloque no se selecciona un proveedor, no se define una URL
pública, no se declara implementada la producción y no se afirma la existencia
de respaldos de producción.

Los requisitos RNF-10 y RNF-11 no se presentan como verificados en este
documento.

### 8. Configuración del backend

El backend utiliza el sistema de configuración de ASP.NET Core, que combina
los archivos de configuración y las variables del entorno según el entorno de
ejecución. La configuración efectiva debe proporcionar los valores requeridos
sin incorporar secretos al repositorio.

La entrada `ConnectionStrings:DefaultConnection` es obligatoria. El método
`AddInfrastructure` obtiene esta cadena mediante la configuración y, si no
existe o está vacía, detiene la inicialización mediante una excepción. Cuando
está disponible, se utiliza para configurar `PanyebarDbContext` con el
proveedor de SQL Server.

La sección `Jwt` contiene los parámetros de los tokens de acceso:

- `Jwt:Issuer`: emisor esperado del token;
- `Jwt:Audience`: audiencia esperada;
- `Jwt:ExpirationMinutes`: duración de los tokens emitidos, en minutos;
- `Jwt:Key`: clave requerida para firmar y validar los tokens.

La clave JWT no se documenta ni se almacena en este manual. Los secretos deben
suministrarse mediante configuración segura del entorno, un almacén de secretos
o el mecanismo equivalente disponible para la ejecución; no deben incorporarse
al repositorio.

`PublicWeb:BaseUrl` identifica el origen web permitido para el frontend. En el
entorno `Development`, `Program.cs` utiliza este valor para configurar el
origen permitido por la política CORS `FrontendDevelopment`. Esta estrategia
de CORS es observable para desarrollo y no implica que sea la misma que se
utilice en producción.

Los valores locales observables en los archivos de desarrollo y perfiles de
ejecución son el frontend en `http://localhost:5173`, el backend HTTP en
`http://localhost:5166`, el backend HTTPS en `https://localhost:7164` y una
base local de SQL Server Express denominada `PanyebarDb`. Estos valores no
representan una configuración de producción.

Swagger y Swagger UI se habilitan únicamente cuando
`app.Environment.IsDevelopment()` es verdadero. El pipeline también utiliza
`UseHttpsRedirection()` para redirigir las solicitudes HTTP según la
configuración de ejecución.

| Parámetro                             | Propósito                                                  | Requerido                                    | Tratamiento recomendado                                                                         |
| ------------------------------------- | ---------------------------------------------------------- | -------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `ConnectionStrings:DefaultConnection` | Cadena usada por el contexto para conectarse a SQL Server. | Sí                                           | Suministrarla mediante configuración segura del entorno; no registrar valores sensibles.        |
| `Jwt:Issuer`                          | Identificar el emisor válido de los tokens.                | Sí                                           | Mantenerla consistente entre emisión y validación.                                              |
| `Jwt:Audience`                        | Identificar la audiencia válida de los tokens.             | Sí                                           | Mantenerla consistente entre emisión y validación.                                              |
| `Jwt:ExpirationMinutes`               | Definir la duración de los tokens emitidos.                | Configurado actualmente en `60`              | Ajustarla según la política de seguridad del entorno.                                           |
| `Jwt:Key`                             | Firmar y validar tokens JWT.                               | Sí                                           | Protegerla como secreto externo al repositorio y no exponerla en registros.                     |
| `PublicWeb:BaseUrl`                   | Definir el origen permitido por CORS en `Development`.     | Sí en `Development`                          | Configurar el origen local o autorizado del entorno; revisar la estrategia para producción.     |
| `ASPNETCORE_ENVIRONMENT`              | Seleccionar el entorno de ejecución de ASP.NET Core.       | Para activar el comportamiento `Development` | Definirlo explícitamente según el entorno; no usar `Development` como configuración productiva. |

### 9. Base de datos y persistencia

La persistencia utiliza Entity Framework Core 8.0.0 con el proveedor de SQL
Server. El contexto principal es `PanyebarDbContext`, ubicado en
`backend/src/Panyebar.Infrastructure/Persistence/PanyebarDbContext.cs`.

Sus `DbSet` se agrupan funcionalmente de la siguiente manera:

- **Personas y suministros:** `Personas`, `Sectores`, `Suministros` y
  `PersonaSuministros`.
- **Cuotas y obligaciones:** `Cuotas`, `Obligaciones`.
- **Jornadas:** `Jornadas`, `ParticipacionesJornada` y
  `ObligacionesJornada`.
- **Pagos:** `Pagos` y `AplicacionesPago`.
- **Finanzas:** `Egresos`.
- **Seguridad:** `UsuariosAdministrativos`, `Roles`, `Permisos`,
  `UsuarioRoles` y `RolPermisos`.
- **Administración del Comité:** `AdministracionesComite`, `Cargos` e
  `IntegrantesAdministracion`.
- **Auditoría:** `Auditorias`.
- **Abastecimiento y procesos:** `ProgramacionesAbastecimiento`,
  `ProcesosSuministro` y `SolicitudesNuevoServicio`.

En `OnModelCreating`, el contexto define la secuencia `SuministroNisSequence`
en el esquema `dbo`, con valor inicial `1` e incremento de `1`. La secuencia
aporta el correlativo numérico; no constituye por sí sola el formato completo
del NIS. La composición del identificador se realiza en `SuministroNisGenerator`
y `NisFormatter`, donde el valor se presenta con el formato `PAN-######`
dentro del rango implementado.

El mismo método aplica las configuraciones del ensamblado de Infrastructure
mediante `ApplyConfigurationsFromAssembly` y ejecuta `ConfigureUtcDateTimes()`.
Las clases de configuración se encuentran en
`backend/src/Panyebar.Infrastructure/Persistence/Configurations/` y definen
el mapeo de entidades, relaciones, restricciones e índices de persistencia sin
concentrar esas reglas en el contexto.

### 10. Migraciones de Entity Framework Core

Las migraciones de Entity Framework Core se encuentran en
`backend/src/Panyebar.Infrastructure/Persistence/Migrations/`. Las migraciones
actuales, ordenadas cronológicamente por su identificador, son:

| Migración                                           | Finalidad inferible por el nombre                  |
| --------------------------------------------------- | -------------------------------------------------- |
| `20260827025307_InitialCreate`                      | Creación inicial del modelo de datos.              |
| `20260903230736_ConfigureSectorAdministration`      | Configuración de la administración de sectores.    |
| `20260904000930_ConfigurePersonAdministration`      | Configuración de la administración de personas.    |
| `20260905014010_ConfigureSupplyIdentity`            | Configuración de la identidad de los suministros.  |
| `20260905015808_ConfigureSupplyAdministration`      | Configuración de la administración de suministros. |
| `20260908015939_ConfigureSupplyProcesses`           | Configuración de los procesos de suministro.       |
| `20260908153046_ConfigureFeesAndObligations`        | Configuración de cuotas y obligaciones.            |
| `20260911003707_ConfigureCommunityWorkDays`         | Configuración de jornadas de trabajo comunitario.  |
| `20260912004909_ConfigurePayments`                  | Configuración de pagos.                            |
| `20260917033536_ConfigureFinancialManagement`       | Configuración de la gestión financiera.            |
| `20260917044659_AllowHistoricalPaymentApplications` | Habilitación de aplicaciones históricas de pagos.  |
| `20260929025905_AddSectorToPersonas`                | Incorporación del sector a las personas.           |

`Program.cs` no evidencia llamadas a `Database.Migrate()`,
`Database.MigrateAsync()` ni `EnsureCreated()`. Por tanto, las migraciones no
se presentan como aplicadas automáticamente al iniciar la API. Su aplicación
debe formar parte de un procedimiento controlado de preparación o actualización
de la base de datos; el procedimiento productivo definitivo queda pendiente de
definición y no se establece en este bloque.

No existe evidencia de un seeding general mediante `HasData` en el código
productivo actual. Algunas migraciones históricas sí contienen operaciones SQL
idempotentes para incorporar permisos u otros elementos específicos y una de
ellas contempla compatibilidad con el usuario existente `demo.admin`. Este
usuario se documenta únicamente como referencia de QA o demostración y no como
cuenta administrativa definitiva de producción.

La base de desarrollo no debe asumirse como una copia completa del entorno
final. Los datos ficticios de prueba y cualquier cuenta usada para demostración
no deben presentarse como datos productivos.
