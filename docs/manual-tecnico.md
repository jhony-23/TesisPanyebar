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
