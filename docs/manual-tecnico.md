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

### 11. Seguridad, autenticación y autorización

#### Autenticación

La API utiliza autenticación JWT Bearer. El flujo observable inicia en
`POST /api/auth/login`: se busca un usuario administrativo activo por su nombre
de usuario, se verifica la contraseña contra su hash y, si las credenciales son
válidas, se emite un token de acceso. La respuesta también obtiene los permisos
vigentes del usuario para que el frontend pueda representar las capacidades
disponibles.

El token emitido contiene la identidad administrativa en los claims de sujeto,
identificador y nombre de usuario. Su emisión utiliza la configuración de
`Jwt:Issuer`, `Jwt:Audience` y `Jwt:ExpirationMinutes`. La API valida el issuer,
la audience, la vigencia y la clave de firma del token; además, el
`ClockSkew` de la validación está configurado en cero. La clave `Jwt:Key` es un
secreto de configuración y no se incluye en este documento.

El endpoint `GET /api/auth/me` requiere autenticación y obtiene la identidad a
partir de los claims del token para devolver la información del usuario y sus
permisos actuales. La duración usada para emitir el token se configura mediante
`Jwt:ExpirationMinutes`.

#### Contraseñas

Las contraseñas se procesan mediante `PasswordHasher<UsuarioAdministrativo>` a
través de `PasswordHasherAdapter`. El sistema guarda el resultado en
`UsuarioAdministrativo.PasswordHash` y verifica la contraseña proporcionada
contra ese valor; la entidad no está diseñada para almacenar la contraseña en
texto legible. La implementación acepta también el resultado de verificación
que indica que el hash requiere un nuevo cálculo.

El código impone una longitud mínima de ocho caracteres al crear usuarios
administrativos o restablecer contraseñas. No se documenta aquí ningún valor de
contraseña ni ninguna credencial concreta.

#### Autorización basada en permisos

La autorización se implementa mediante políticas dinámicas. Las políticas con
el formato `Permission:<código>` son interpretadas por
`PermissionPolicyProvider`, que crea un requisito `PermissionRequirement` y
exige primero una identidad autenticada. `PermissionAuthorizationHandler`
obtiene el identificador administrativo desde los claims y consulta
`IUsuarioPermissionRepository`.

La consulta de permisos comprueba que el usuario esté activo, recorre sus
relaciones `UsuarioRol`, considera únicamente los roles activos, recorre sus
relaciones `RolPermiso` y finalmente exige que el permiso encontrado esté
activo y tenga el código solicitado. La autorización efectiva ocurre cuando
esa consulta confirma el permiso.

En los controladores se observan códigos con nomenclatura de módulo y acción,
por ejemplo `PERSONAS.VER`, `SUMINISTROS.GESTIONAR` y
`SEGURIDAD.AUDITORIA.VER`. Estos ejemplos proceden de los códigos declarados
por la aplicación y no constituyen una lista exhaustiva en este manual.

Autenticación, rol, permiso y autorización cumplen funciones distintas:

- **Autenticación:** determina si las credenciales permiten reconocer a un
  usuario administrativo y emitir un token.
- **Rol:** agrupa una asignación administrativa reutilizable.
- **Permiso:** representa una capacidad técnica identificada por un código.
- **Autorización efectiva:** decide si una solicitud autenticada puede ejecutar
  una operación, consultando el permiso requerido y el estado vigente del
  usuario, sus roles y sus permisos.

#### Usuarios, roles y permisos

`UsuarioAdministrativo` contiene la identidad administrativa, su hash de
contraseña y su estado. `UsuarioRol` relaciona usuarios con roles. `Rol`
contiene el nombre, descripción y estado de una agrupación de autorización.
`RolPermiso` relaciona cada rol con los permisos que puede conceder, mientras
que `Permiso` contiene el código, nombre, descripción y estado de la capacidad
técnica.

La asignación efectiva es, por tanto, una relación indirecta:
`UsuarioAdministrativo` → `UsuarioRol` → `Rol` → `RolPermiso` → `Permiso`.
El código no establece aquí que un rol específico sea obligatorio para
producción. Las cuentas usadas para QA o demostración no forman parte de una
definición de credenciales productivas.

#### Auditoría

Las operaciones administrativas que incorporan auditoría crean registros en la
entidad `Auditoria` con el usuario administrativo, la acción, la entidad y su
identificador, la fecha y, cuando corresponde, los valores anterior y nuevo.
El servicio de administración permite consultar estos registros con filtros por
fechas, usuario, acción y entidad, con un límite controlado de resultados.

El código muestra este mecanismo en operaciones administrativas y funcionales
concretas, como cambios de responsable, procesos de suministro, gestión de
usuarios, cuotas, pagos, finanzas y administración del Comité. No se afirma que
absolutamente todas las operaciones del sistema generen un registro de
auditoría.

#### Buenas prácticas operativas

Las siguientes son recomendaciones operativas y no afirmaciones de que todas
ellas estén verificadas o automatizadas por la implementación actual:

- mantener secretos y credenciales fuera del repositorio;
- utilizar credenciales individuales y evitar compartir cuentas administrativas;
- aplicar el principio de mínimo privilegio mediante permisos necesarios;
- cambiar o revocar accesos cuando corresponda;
- utilizar HTTPS en el entorno de entrega.

### 12. Identificación de suministros mediante NIS y código QR

#### NIS

El NIS pertenece al suministro y se almacena en la propiedad `Suministro.Nis`.
Al crear un suministro, `SuministroNisGenerator` solicita el siguiente valor
de la secuencia persistente `dbo.SuministroNisSequence`. Después,
`NisFormatter` convierte ese correlativo en el formato implementado
`PAN-######`; por ejemplo, el primer valor puede representarse
conceptualmente como `PAN-000001`.

La secuencia numérica y el formateo son responsabilidades separadas. El
modelo configura un índice único sobre `Nis`, por lo que no se admiten dos
suministros con el mismo identificador almacenado. El NIS es una identidad del
suministro, no de la persona responsable; el cambio de responsable no crea un
nuevo NIS. Las operaciones de cambio de responsable conservan el suministro y
no asignan nuevamente su NIS.

#### Token técnico y código QR

Cada suministro se asocia con `Suministro.CodigoQrToken`. El generador utiliza
un valor aleatorio criptográfico de 32 bytes y lo representa como Base64 URL-safe
sin el relleno final. El token se almacena asociado al suministro y tiene un
índice único.

Al solicitar el QR administrativo, el backend construye una URL absoluta con
`PublicWeb:BaseUrl`, la ruta `/suministro/qr/{token}` y el token escapado. En el
frontend, `qrcode.react` utiliza ese valor para el renderizado visual mediante
`QRCodeSVG`. Por tanto, el token técnico, la URL de consulta y la representación
visual son elementos relacionados pero distintos.

El token no contiene directamente datos personales ni financieros y no funciona
como contraseña. La consulta QR pública no sustituye la autenticación ni la
autorización administrativa: existe una ruta pública específica para la
consulta permitida y la consulta administrativa por token permanece protegida
por el permiso de suministros.

#### Consulta pública mediante QR

La ruta `GET /api/suministros/public/qr/{token}` permite consultar el resumen
público asociado con un token válido sin exigir autenticación. La respuesta
implementada contiene el NIS, el sector, el estado del suministro, la cantidad
de obligaciones pendientes y el total pendiente.

El cálculo considera únicamente obligaciones con estado `Pendiente`: incluye
las obligaciones propias del suministro y, cuando existe un responsable
vigente, las obligaciones personales pendientes de esa persona. Por ello, las
obligaciones pagadas o anuladas no se incluyen, ni se consideran las
obligaciones personales de responsables históricos. La respuesta no expone
directamente datos personales del responsable ni detalles individuales de las
obligaciones.

#### Cambio de responsable

El cambio de responsable modifica la relación entre persona y suministro, no
la identidad del suministro. Si existía una relación vigente, se registra su
fecha de finalización y estado finalizado; después se crea una nueva relación
vigente en `PersonaSuministro`. Un índice único filtrado garantiza como máximo
un responsable vigente por suministro, mientras que las relaciones anteriores
se conservan para el historial.

El suministro conserva su NIS y su `CodigoQrToken` durante este cambio. La
consulta pública utiliza el responsable vigente para incorporar sus obligaciones
personales pendientes, de modo que no arrastra las obligaciones personales de
responsables históricos. La operación también registra la acción, la entidad,
el usuario administrativo y los valores anterior y nuevo en `Auditoria`.

### 13. Cuotas y obligaciones

#### Cuotas

`Cuota` representa una definición administrativa de un cobro. Contiene nombre,
descripción opcional, monto, periodicidad, fecha inicial y final de vigencia y
estado de registro. Las periodicidades implementadas son `Anual` y `Mensual`.
El servicio valida el monto con precisión de dos decimales y la coherencia del
rango de vigencia.

Una cuota puede utilizarse para generar una obligación para un suministro. En
ese proceso se conserva la referencia a `Cuota`, se copia el concepto y monto
de la cuota y se establece un período compatible con su periodicidad: `yyyy`
para una cuota anual o `yyyy-MM` para una cuota mensual. El sistema impide
duplicar una obligación no anulada para la misma cuota, suministro y período.
Una modificación posterior de la cuota no reescribe el monto histórico de una
obligación ya generada. El código no establece un monto permanente como Q30.

La cuota es la configuración que puede reutilizarse para generar cobros; la
obligación es el registro económico concreto, con su propio monto, período,
fechas, origen y estado.

#### Obligaciones

`Obligacion` representa un importe exigible a un único titular. La persistencia
aplica una restricción XOR: cada obligación pertenece a un suministro o a una
persona, pero no a ambos ni a ninguno. Por ello, una obligación de cuota se
relaciona con un suministro y una obligación personal se relaciona directamente
con una persona.

El origen distingue `CuotaOrdinaria`, `Jornada` y `Administrativa`. El registro
conserva concepto, monto, período opcional, fecha de generación, vencimiento
opcional y estado. Los estados implementados son `Pendiente`, `Pagada` y
`Anulada`. La consulta proyecta además `EsMorosa` cuando una obligación
pendiente tiene vencimiento y la fecha actual ya lo superó.

La generación administrativa personal recibe varias personas activas, valida
que no existan identificadores repetidos y crea una obligación independiente
para cada persona seleccionada. Cada registro queda con `PersonaId`, sin
`SuministroId` ni `CuotaId`, y se audita individualmente; no se crea una
obligación compartida entre personas.

Las consultas de obligaciones exponen un listado ordenado por fecha de
generación e identificador, y una consulta individual. Las operaciones de
generación validan referencias, período, montos y duplicidad; la anulación
requiere motivo y solo cambia una obligación pendiente a `Anulada`, conservando
el registro y su auditoría. Una obligación pagada o anulada no puede anularse
nuevamente.

### 14. Jornadas comunitarias

`Jornada` representa una actividad comunitaria dirigida a personas. Contiene
nombre, descripción, fecha, horario opcional, ubicación, monto opcional de
incumplimiento y estado. Sus estados son `Planificada`, `Cerrada` y
`Cancelada`; solo una jornada planificada puede editarse, cancelarse, modificar
participantes o cerrarse.

`ParticipacionJornada` relaciona una jornada con una persona y registra el
resultado administrativo y una observación opcional. Los resultados observables
son `Pendiente`, `Participacion`, `Ausencia` y `AusenciaJustificada`. Una
restricción única evita más de una participación para la misma jornada y
persona. La jornada no se relaciona aquí con el suministro.

Para cerrar una jornada deben existir participantes y no puede quedar ninguno
con resultado `Pendiente`. La ausencia no genera automáticamente una deuda en
cualquier circunstancia. Al cerrar, solo las participaciones con resultado
`Ausencia` generan obligaciones cuando la jornada tiene configurado un
`MontoIncumplimiento` positivo. La participación y la ausencia justificada no
generan obligación, y una ausencia sin monto configurado tampoco la genera.

La obligación derivada se asigna a la persona de la participación, tiene origen
`Jornada`, queda pendiente, no tiene suministro, cuota, período ni vencimiento,
y utiliza como concepto la jornada correspondiente. `ObligacionJornada`
relaciona esa obligación con la participación que la originó. Sus índices
únicos garantizan como máximo una obligación por participación y una
participación por obligación; antes del cierre el servicio también evita volver
a enlazar participaciones que ya tengan obligación.

### 15. Pagos y comprobantes

#### Registro y aplicación de pagos

Un `Pago` registra una transacción con monto, fecha, concepto, usuario
administrativo y estado. Sus obligaciones se relacionan mediante
`AplicacionPago`, que conecta cada pago con cada `Obligacion` aplicada:

```text
Pago -> AplicacionPago -> Obligacion
```

El registro de pago exige que las obligaciones existan, que sus identificadores
no estén repetidos, que todas estén en estado `Pendiente` y que pertenezcan al
mismo titular. El titular puede ser una persona o un suministro, pero no se
mezclan obligaciones de distintos titulares. El monto recibido debe coincidir
exactamente con la suma de los montos de las obligaciones seleccionadas.

En consecuencia, no se admiten pagos parciales, sobrepagos ni saldos a favor.
Un mismo pago sí puede aplicar varias obligaciones completas del mismo titular.
En una operación válida se crea el pago, se crea una aplicación por obligación
y cada obligación cambia a `Pagada` dentro de la operación transaccional.
La combinación `PagoId` y `ObligacionId` es única para evitar duplicar una
misma aplicación dentro del mismo pago.

#### Comprobantes

El servicio proporciona los datos del comprobante mediante
`GetComprobanteAsync`; no crea un archivo PDF ni persiste un documento separado.
El número se deriva del identificador del pago con el formato `PAG-######`.
El `ComprobantePagoDto` contiene, de forma general, número, identificador,
fecha, concepto, total, estado, usuario administrativo, titular y la lista de
obligaciones aplicadas con su origen, concepto, período y monto.

La API expone estos datos mediante el endpoint de comprobante y el frontend los
utiliza para la representación digital del comprobante. La existencia de este
DTO no implica un formato de archivo adicional.

#### Anulación y conservación histórica

La anulación solo procede para un pago registrado cuyas aplicaciones existan y
cuyas obligaciones continúen pagadas. Cambia el estado del pago a `Anulado`,
devuelve las obligaciones asociadas a `Pendiente` y conserva las filas de
`Pago` y `AplicacionPago`; por tanto, la relación histórica permanece. La
misma obligación puede pagarse nuevamente después de anular el pago anterior,
sin eliminar la aplicación histórica.

La operación registra una auditoría con el estado y las obligaciones afectadas.
Un pago ya anulado no puede anularse otra vez. Las reglas de validación también
impiden registrar pagos sobre obligaciones inexistentes, no pendientes o con
montos inconsistentes.

### 16. Gestión financiera

La gestión financiera combina pagos registrados y egresos administrativos. No
existe una entidad persistente independiente llamada `Ingreso`: los ingresos
financieros se derivan de los registros de `Pago` cuyo estado es `Registrado`.
Los pagos anulados quedan fuera de los ingresos, movimientos y totales.

`Egreso` es la entidad persistente para gastos administrativos del Comité. Guarda
concepto, monto, fecha civil, usuario administrativo y estado. Los estados son
`Registrado` y `Anulado`; el monto debe ser positivo y el concepto no puede
superar la longitud configurada. Un egreso registrado puede editarse o anularse.
La anulación conserva la fila, cambia su estado y excluye el egreso de los
movimientos y totales; un egreso anulado no puede editarse ni anularse de nuevo.

El servicio financiero proporciona consultas de ingresos, egresos, movimientos
y resumen, con filtros de fecha y, cuando corresponde, estado o tipo de
movimiento. El resumen calcula:

```text
balance = pagos registrados - egresos registrados
```

Los pagos se filtran usando el día operativo de Guatemala y los egresos usan su
fecha civil. Los movimientos distinguen los tipos `Ingreso` y `Egreso`, y el
resumen devuelve ingresos totales, egresos totales y balance.

El registro, edición y anulación de egresos generan auditoría con el usuario,
la acción, la entidad y los valores anterior y nuevo cuando corresponde. Los
pagos registrados y anulados también conservan la trazabilidad mediante las
auditorías del servicio de pagos.

Los controladores financieros se protegen con los permisos declarados para
finanzas, entre ellos `FINANZAS.VER` y `FINANZAS.GESTIONAR`; estos permisos
controlan el acceso a consultas y operaciones administrativas respectivamente.

### 17. Dashboard y reportes

#### Dashboard

El dashboard se expone mediante `GET /api/dashboard/resumen` y requiere el
permiso `DASHBOARD.VER`. Recibe año y mes, valida que formen un período mensual
representable y devuelve un resumen para ese mes administrativo. El cálculo
incluye ingresos totales derivados de pagos registrados, egresos totales de
egresos registrados, balance, cantidad de pagos registrados, cantidad de
obligaciones pendientes y monto de esas obligaciones pendientes.

Los pagos se filtran por su fecha UTC convertida desde el inicio del día
operativo de Guatemala. Los egresos se consultan con sus fechas civiles. Las
obligaciones pertenecen al mes por `FechaGeneracion`, no por su fecha de
vencimiento ni por el texto de su período. El balance del resumen es la
diferencia entre ingresos y egresos del intervalo mensual.

El frontend utiliza además la consulta de recaudación por sector para mostrar
la distribución territorial de pagos. Esta consulta agrupa aplicaciones de
pagos registrados cuyas obligaciones pertenecen a suministros, y suma el monto
histórico de esas obligaciones por sector. Las aplicaciones históricas de pagos
anulados se conservan, pero no se consideran recaudación.

#### Reportes

Los reportes se exponen bajo `GET /api/reportes` y requieren `REPORTES.VER`.
Las consultas implementadas son:

- **Pagos:** lista pagos dentro de un rango de fechas, incluyendo su estado,
  monto, concepto, fecha y usuario administrativo. El reporte conserva tanto
  registros como anulaciones; solo los registrados representan ingreso.
- **Recaudación por sector:** agrupa por sector las aplicaciones de pagos
  registrados asociadas con obligaciones de suministros y permite rango de
  fechas.
- **Obligaciones pendientes:** lista obligaciones pendientes, su titular según
  corresponda, origen, concepto, monto, período, fechas y condición de mora.
- **Participación en jornadas:** consulta participaciones de personas,
  resultado, observación, jornada, estado y fecha de la jornada, con rango de
  fechas civiles.

Los rangos de fechas se validan para impedir que la fecha inicial sea posterior
a la final. La implementación no evidencia exportación a Excel o PDF, envío por
correo, gráficas adicionales ni analítica predictiva.

### 18. Programación del abastecimiento

`ProgramacionAbastecimiento` registra la planificación administrativa del
abastecimiento para un `Sector`. Contiene fecha, hora inicial, hora final,
estado y observación opcional. Los estados implementados son `Programado`,
`Completado` y `Cancelado`.

El servicio permite consultar una programación individual o listados filtrados
por fecha inicial, fecha final y sector. Los resultados se ordenan por fecha,
hora y nombre del sector. Para crear o actualizar una programación se valida
que el sector exista y esté activo, que el intervalo horario sea válido y que la
observación no supere la longitud permitida. No se admite solapamiento de
intervalos programados para el mismo sector y fecha.

También existe una operación de creación recurrente. La recurrencia puede ser
semanal, mensual o anual, y se define mediante una cantidad de ocurrencias o
una fecha final, pero no ambas. El servicio limita la generación a entre 2 y 52
programaciones y verifica los solapamientos antes de persistir el conjunto.

Una programación en estado `Programado` puede actualizarse, completarse o
cancelarse. Las transiciones no ejecutan acciones sobre válvulas, sensores,
telemetría, redes hidráulicas ni otros dispositivos: el módulo registra y
consulta planificación administrativa por sector. Las operaciones de gestión
registran auditoría y requieren `ABASTECIMIENTO.GESTIONAR`; las consultas
requieren `ABASTECIMIENTO.VER`.

### 19. Procesos administrativos del suministro

#### Solicitud de nuevo servicio

`SolicitudNuevoServicio` representa una solicitud administrativa presentada por
una persona para un sector, con dirección de referencia, fecha de solicitud,
observación opcional y estado. Sus estados son `Pendiente`, `Aprobada` y
`Rechazada`. La creación valida que la persona y el sector estén activos y que
la persona no tenga otra solicitud pendiente de nuevo servicio.

Las solicitudes se consultan priorizando las pendientes. Resolver una solicitud
solo es posible mientras permanece pendiente y requiere una observación de
resolución válida cuando se proporciona. Al aprobarla, el servicio crea el
suministro activo, genera su NIS y token QR, lo relaciona con el sector y crea
la relación vigente entre el suministro y la persona solicitante. Al rechazarla
no se crea un suministro. Por tanto, la solicitud no representa por sí sola un
suministro creado automáticamente antes de su aprobación.

La creación y resolución de solicitudes se auditan con el usuario
administrativo que ejecuta la operación. La consulta requiere
`SUMINISTROS.VER` y la creación, aprobación o rechazo requiere
`SUMINISTROS.GESTIONAR`.

#### Cancelación, reconexión e historial del suministro

Los procesos operativos administrativos del suministro se representan mediante
`ProcesoSuministro`, relacionado con un `Suministro` y con el usuario
administrativo que ejecutó la acción. El tipo de proceso puede ser
`Cancelacion` o `Reconexion`; cada registro conserva estado anterior, estado
nuevo, fecha, motivo obligatorio y observación opcional.

La cancelación solo se aplica a un suministro `Activo` y lo cambia a
`Cancelado`. La reconexión solo se aplica a un suministro `Cancelado` y lo
devuelve a `Activo`. El motivo tiene longitud validada y la operación registra
el proceso histórico junto con una auditoría. El suministro no se elimina, por
lo que conserva su registro, NIS, token QR y el historial consultable de
procesos.

Las consultas administrativas de procesos requieren `SUMINISTROS.VER`, y las
transiciones de cancelación y reconexión requieren `SUMINISTROS.GESTIONAR`.
Estas operaciones representan cambios administrativos de estado y no deben
interpretarse como eliminación física del suministro ni como automatización
del servicio hidráulico.

### 20. Administración del Comité

`AdministracionComite` representa un período de administración con nombre,
fecha de inicio, fecha de finalización opcional y estado de registro. Una
administración se considera activa cuando su estado es `Activo` y no tiene
fecha de finalización.

El servicio permite crear una administración, consultar el listado histórico,
obtener una administración y finalizarla. La creación valida nombre, fecha y
usuario actor, y no permite otra administración activa al mismo tiempo. La
finalización exige una fecha no anterior al inicio, registra `FechaFin` y cambia
el estado a `Inactivo`. Las administraciones anteriores se conservan y se
devuelven en las consultas históricas.

#### Integrantes y cargos

`IntegranteAdministracion` relaciona una administración con una `Persona` y un
`Cargo`. `Cargo` es un catálogo con nombre, descripción y estado, y el servicio
expone los cargos activos para asignación. La asignación de un integrante exige
que la administración esté activa y que la persona y el cargo estén activos.

La asignación evita conservar simultáneamente conflictos de la misma persona o
del mismo cargo dentro de una administración: cuando corresponde, reemplaza
las asignaciones relacionadas y registra la nueva combinación persona-cargo.
Los integrantes se presentan con el nombre de la persona y el nombre del
cargo. El modelo no relaciona `IntegranteAdministracion` con
`UsuarioAdministrativo`; ser integrante del Comité no equivale a poseer una
cuenta de acceso al sistema.

La creación de administraciones, la asignación de integrantes y la finalización
requieren `ADMINISTRACION.GESTIONAR`; las consultas y el catálogo de cargos
requieren `ADMINISTRACION.VER`. Estas operaciones registran auditoría con el
usuario actor, la acción, la entidad y los valores anteriores y nuevos cuando
corresponde.
