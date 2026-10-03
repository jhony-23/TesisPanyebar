# Manual de Usuario

## Sistema digital para la administración del servicio de agua potable de la Aldea Panyebar

**Universidad del Valle de Guatemala**

**Facultad de Ingeniería**

**Documento complementario del trabajo de graduación:** Desarrollo de un sistema digital para la administración y control de pagos de servicio de agua potable mediante identificación única con código QR y plataforma web móvil para la cobranza domiciliaria en la Aldea Panyebar, San Juan La Laguna, Sololá.

**Versión:** 1.0
**Año:** 2026

**Autor:** Juan Pablo Ixcamparic Escún

**Lugar:** Guatemala

Las figuras completas se encuentran en la versión formal del Manual de Usuario. Este archivo conserva sus referencias textuales y no incorpora imágenes binarias.

## Control del documento

| Documento | Manual de Usuario                                                   |
| --------- | ------------------------------------------------------------------- |
| Sistema   | Sistema digital para la administración del servicio de agua potable |
| Comunidad | Aldea Panyebar                                                      |
| Versión   | 1.0                                                                 |
| Año       | 2026                                                                |
| Autor     | Juan Pablo Ixcamparic Escún                                         |

Historial de versiones

| Versión | Año  | Descripción                                                                                                       |
| ------- | ---- | ----------------------------------------------------------------------------------------------------------------- |
| 1.0     | 2026 | Versión inicial del manual, elaborada para la entrega del sistema al Comité de Agua Potable de la Aldea Panyebar. |

Este documento constituye el manual de usuario del sistema y puede utilizarse de forma independiente. También puede incorporarse como anexo del trabajo de graduación que documenta el desarrollo del sistema, con su propia portada y numeración.

> **INFORMACIÓN — DATOS DE DEMOSTRACIÓN**
> Los nombres, identificadores, usuarios, suministros, montos y demás datos mostrados en las figuras corresponden a información ficticia utilizada durante las pruebas y la documentación del sistema.

ÍNDICE

## LISTA DE FIGURAS

**Figura 1. Botón Cerrar sesión en el encabezado**

**Figura 2. Distribución general de la interfaz en el módulo Personas**

**Figura 3. Panel de inicio con los módulos disponibles para el usuario autenticado**

**Figura 4. Listado de personas registradas**

**Figura 5. Formulario para registrar una persona**

**Figura 6. Módulo Sectores: listado y formulario de creación**

**Figura 7. Listado de suministros registrados**

**Figura 8. Formulario para registrar un suministro**

**Figura 9. Confirmación del registro del suministro PAN-000001**

**Figura 10. Código QR del suministro PAN-000001**

**Figura 11. Suministro con responsable asignado**

**Figura 12. Historial de responsables del suministro**

**Figura 13. Formulario para registrar una solicitud de nuevo servicio**

**Figura 14. Solicitud registrada en estado Pendiente**

**Figura 15. Ventana para aprobar una solicitud**

**Figura 16. Solicitud aprobada con el suministro PAN-000002 creado**

**Figura 17. Listado de cuotas registradas**

**Figura 18. Formulario para crear una cuota**

**Figura 19. Listado de obligaciones registradas**

**Figura 20. Panel para generar una obligación desde una cuota**

**Figura 21. Panel para generar obligaciones personales**

**Figura 22. Formulario para crear una jornada comunitaria**

**Figura 23. Jornada registrada en estado Planificada**

**Figura 24. Panel de gestión de una jornada**

**Figura 25. Registro de participación y ausencia**

**Figura 26. Jornada cerrada con la obligación generada por ausencia**

**Figura 27. Obligación generada por ausencia a una jornada**

**Figura 28. Panel para registrar un pago de varias obligaciones**

**Figura 29. Ventana de confirmación del pago**

**Figura 30. Pago registrado con su número de comprobante**

**Figura 31. Detalle de un pago registrado**

**Figura 32. Detalle del pago de una obligación de suministro**

**Figura 33. Obligaciones canceladas después del pago**

**Figura 34. Comprobante de pago en pantalla**

**Figura 35. Comprobante de pago impreso desde el navegador**

**Figura 36. Pantalla principal del módulo Finanzas**

**Figura 37. Panel para registrar un egreso**

**Figura 38. Egresos registrados**

**Figura 39. Período administrativo y totales del dashboard**

**Figura 40. Recaudación por sector y alcance del resumen**

**Figura 41. Reporte de pagos**

**Figura 42. Reporte de obligaciones pendientes**

**Figura 43. Reporte de participación en jornadas**

**Figura 44. Calendario de abastecimiento en vista Mes**

**Figura 45. Ventana para crear una programación de abastecimiento**

**Figura 46. Detalle de una programación de abastecimiento**

**Figura 47. Confirmación para marcar una programación como completada**

**Figura 48. Motivos de cancelación de una programación**

**Figura 49. Programación de abastecimiento en vista Agenda**

**Figura 50. Módulo Administración y sus pestañas**

**Figura 51. Ventana para crear un período administrativo**

**Figura 52. Administración vigente con los cargos del Comité**

**Figura 53. Ventana para asignar un integrante del Comité**

**Figura 54. Integrantes asignados a la administración vigente**

**Figura 55. Ventana para cambiar el integrante de un cargo**

**Figura 56. Cargo sin personas disponibles para asignar**

**Figura 57. Ventana para finalizar un período administrativo**

**Figura 58. Sección de administraciones anteriores**

**Figura 59. Cuentas administrativas con sus roles**

**Figura 60. Ventana para crear un usuario administrativo**

**Figura 61. Usuario administrativo creado sin roles asignados**

**Figura 62. Administración del estado y los roles de una cuenta**

**Figura 63. Cuenta desactivada**

**Figura 64. Cuenta inactiva con la opción Activar usuario**

**Figura 65. Ventana para restablecer la contraseña de un usuario**

**Figura 66. Pestaña Roles y permisos con el rol Operador de cobros**

**Figura 67. Ventana para crear un rol administrativo**

**Figura 68. Permisos seleccionados para el rol Operador de cobros**

**Figura 69. Guardado de los permisos del rol**

**Figura 70. Comparación del menú lateral: a) demo.admin; b) operador.demo**

**Figura 71. Módulo Pagos consultado por operador.demo**

**Figura 72. Historial de auditoría**

**Figura 73. Detalle de un registro de auditoría**

**Figura 74. Mensaje de confirmación**

**Figura 75. Mensaje de advertencia en una ventana de confirmación**

**Figura 76. Mensaje de error**

## LISTA DE CUADROS

**Cuadro 1. Procesos administrativos que permite realizar el sistema**

**Cuadro 2. Símbolos utilizados en el manual**

**Cuadro 3. Convenciones de formato**

**Cuadro 4. Requisitos para utilizar el sistema**

**Cuadro 5. Elementos de la interfaz general**

**Cuadro 6. Elementos comunes de la interfaz**

**Cuadro 7. Diferencias entre persona y suministro**

**Cuadro 8. Características del NIS**

**Cuadro 9. Comparación entre NIS y código QR**

**Cuadro 10. Estados de una solicitud**

**Cuadro 11. Diferencia entre cuota y obligación**

**Cuadro 12. Tipos de obligaciones según su origen**

**Cuadro 13. Estados de una jornada**

**Cuadro 14. Información del comprobante de pago**

**Cuadro 15. Diferencias entre pago, ingreso y egreso**

**Cuadro 16. Indicadores del dashboard**

**Cuadro 17. Tipos de reporte disponibles**

**Cuadro 18. Estados de una programación de abastecimiento**

**Cuadro 19. Modelo de acceso del sistema**

**Cuadro 20. Grupos de permisos del sistema**

**Cuadro 21. Resultado de la demostración de restricción de acceso**

**Cuadro 22. Columnas del historial de auditoría**

**Cuadro 23. Información del detalle de auditoría**

**Cuadro 24. Tipos de mensajes**

**Cuadro 25. Mensajes frecuentes del sistema**

**Cuadro 26. Solución de problemas frecuentes**

# I. INTRODUCCIÓN

El Sistema digital para la administración del servicio de agua potable es una plataforma web desarrollada para apoyar al Comité de Agua Potable de la Aldea Panyebar, San Juan La Laguna, Sololá, en el registro, la consulta y el control de la información administrativa del servicio. El sistema reúne en un solo lugar información que antes se conservaba principalmente en registros manuales: personas, suministros, obligaciones económicas, pagos, jornadas comunitarias, movimientos financieros y programación del abastecimiento.

## Propósito del sistema

El sistema centraliza la información administrativa, conserva el historial de las operaciones y facilita la consulta de los datos que el Comité necesita para su gestión. Cada suministro se identifica mediante un Número de Identificación de Suministro (NIS) y un código QR, dos mecanismos que permiten localizar el mismo registro de forma rápida e inequívoca.

La plataforma se utiliza mediante un navegador web y se adapta a computadoras, tabletas y teléfonos. Las funciones disponibles para cada persona dependen de los roles y permisos asignados a su cuenta administrativa.

## Destinatarios del manual

El manual está dirigido a los integrantes del Comité de Agua Potable y a las personas autorizadas para operar el sistema, por ejemplo, encargados de registro, de cobros, de finanzas o de la administración de accesos. No requiere conocimientos técnicos de informática: basta con saber utilizar un navegador web.

## Procesos que permite realizar

**Cuadro 1. Procesos administrativos que permite realizar el sistema**

| Área                       | Procesos principales                                                                              |
| -------------------------- | ------------------------------------------------------------------------------------------------- |
| Registro comunitario       | Registro y actualización de personas; catálogo de sectores.                                       |
| Suministros                | Registro de suministros, NIS, código QR, responsables e historial; solicitudes de nuevo servicio. |
| Cobros                     | Cuotas, obligaciones, pagos y comprobantes.                                                       |
| Participación comunitaria  | Jornadas comunitarias, asistencia, ausencias y obligaciones derivadas.                            |
| Finanzas e información     | Ingresos, egresos, balance, dashboard mensual y reportes.                                         |
| Servicio                   | Programación administrativa del abastecimiento por sector.                                        |
| Administración y seguridad | Períodos e integrantes del Comité, usuarios, roles, permisos y auditoría.                         |

_Fuente: elaboración propia._

## Organización del manual

Los capítulos II a V presentan las convenciones, los requisitos, el acceso y la interfaz general. Los capítulos VI a XXI describen, módulo por módulo, los procedimientos de operación en el mismo orden en que aparecen en el menú principal. Los capítulos XXII a XXVI explican el módulo Administración: Comité, usuarios, roles y permisos, restricción de acceso y auditoría. Finalmente, los capítulos XXVII a XXX reúnen los mensajes del sistema, las buenas prácticas, la solución de problemas y el glosario.

## Datos utilizados en las ilustraciones

Las figuras corresponden a capturas de pantalla reales del sistema en funcionamiento. Para proteger la privacidad de la comunidad, se elaboraron con información ficticia, como los sectores

«Sector Demostración Norte» y «Sector Demostración Sur», las personas de ejemplo y las cuentas demo.admin y operador.demo.

> **INFORMACIÓN**
> Los nombres, identificadores, usuarios, suministros, montos y demás datos mostrados en las figuras corresponden a información ficticia utilizada durante las pruebas y la documentación del sistema.

# II. CONVENCIONES DEL MANUAL

Para facilitar la lectura, el manual utiliza un conjunto reducido de símbolos, formatos y estructuras que se repiten en todos los capítulos.

## Símbolos

**Cuadro 2. Símbolos utilizados en el manual**

| Símbolo | Nombre                | Significado                                                                         |
| ------- | --------------------- | ----------------------------------------------------------------------------------- |
|         | Información           | Explicación complementaria para comprender una función o un concepto.               |
|         | Resultado esperado    | Lo que debe aparecer después de completar correctamente un procedimiento.           |
|         | Precaución            | Acción que modifica información importante o que no puede revertirse con facilidad. |
|         | Recomendación         | Buena práctica para utilizar correctamente el sistema.                              |
|         | Seguridad             | Información sobre contraseñas, permisos, roles o información restringida.           |
| →       | Ruta o siguiente paso | Secuencia de opciones que deben seleccionarse para llegar a una pantalla.           |

_Fuente: elaboración propia._

## Formato de los textos

**Cuadro 3. Convenciones de formato**

| Convención       | Ejemplo                         | Uso                                                              |
| ---------------- | ------------------------------- | ---------------------------------------------------------------- |
| Ruta             | Menú principal → Personas       | Indica cómo llegar a una pantalla desde el menú lateral.         |
| Negrita          | Seleccione Guardar responsable. | Botones, opciones del menú, pestañas y enlaces de la interfaz.   |
| Cursiva          | Complete el campo Nombres.      | Nombres de campos, listas, casillas y secciones de una pantalla. |
| Comillas latinas | «Cuota creada correctamente.»   | Textos y mensajes que muestra el sistema.                        |

_Fuente: elaboración propia._

## Estructura de los procedimientos

Cada procedimiento importante se presenta con la misma estructura: título, ruta de acceso, condiciones previas cuando corresponden (Antes de comenzar), pasos numerados y resultado

esperado. Las figuras muestran la pantalla real del sistema y se ubican junto al procedimiento que ilustran.

Los nombres de botones, campos y mensajes se transcriben tal como aparecen en la interfaz. Cuando una pantalla es muy extensa, la figura presenta un acercamiento a la parte que interesa para que el texto sea legible.

# III. REQUISITOS PARA UTILIZAR EL SISTEMA

El sistema es una aplicación web: no requiere instalar programas en el equipo del usuario.

Antes de utilizarlo, verifique que cuenta con los elementos del Cuadro 4.

**Cuadro 4. Requisitos para utilizar el sistema**

| Requisito     | Descripción                                                                                                                                                  |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Dispositivo   | Computadora, tableta o teléfono inteligente.                                                                                                                 |
| Navegador web | Navegador actualizado que permita abrir la dirección web del sistema.                                                                                        |
| Conexión      | Conexión a la red o a Internet que permita comunicarse con el servidor donde se instale el sistema, según el entorno de implementación que defina el Comité. |
| Credenciales  | Nombre de usuario y contraseña de una cuenta administrativa activa.                                                                                          |
| Permisos      | Uno o más roles asignados a la cuenta, con los permisos necesarios para las funciones que la persona debe realizar.                                          |

_Fuente: elaboración propia._

> **INFORMACIÓN**
> El sistema necesita comunicarse con el servidor para consultar y registrar información. Esta versión no funciona sin conexión ni requiere una aplicación móvil: en teléfonos y tabletas se utiliza desde el navegador.

> **INFORMACIÓN — FUNCIONES DISPONIBLES**
> Las opciones del menú y las acciones visibles pueden variar entre usuarios. Cada persona observa únicamente los módulos y las operaciones que autorizan los permisos de su cuenta (capítulo XXV).

La dirección web del sistema la proporciona la persona encargada de la administración técnica del sistema dentro del Comité, de acuerdo con el entorno en el que se instale.

# IV. ACCESO AL SISTEMA

El acceso a las funciones administrativas requiere autenticación: el sistema solicita un nombre de usuario y una contraseña antes de mostrar cualquier información administrativa.

## Iniciar sesión

**Ruta: Navegador web → Dirección del sistema → Pantalla de inicio de sesión**

### Antes de comenzar

Verifique que dispone de su nombre de usuario y de su contraseña, y que su cuenta se encuentra activa.

### Pasos

1. Abra el navegador web en su computadora, tableta o teléfono.
2. Ingrese la dirección web del sistema proporcionada por el Comité.
3. Identifique la pantalla de inicio de sesión, que solicita las credenciales de la cuenta administrativa.
4. Ingrese su nombre de usuario.
5. Ingrese su contraseña. Respete las mayúsculas y minúsculas.
6. Seleccione Iniciar sesión.

> **RESULTADO ESPERADO**
> El sistema muestra el panel de inicio con el saludo de bienvenida, y el nombre del usuario autenticado aparece en el encabezado y en la tarjeta Sesión activa del menú lateral (Figura 3).

> **SEGURIDAD**
> Las credenciales son personales. No comparta su contraseña ni permita que otra persona utilice su cuenta: las operaciones administrativas quedan registradas a nombre del usuario que inició sesión.

## Si no es posible ingresar

Si el nombre de usuario o la contraseña no son válidos, el sistema no permite el ingreso. En ese caso:

1. Verifique que escribió correctamente el nombre de usuario, incluidos los puntos, por ejemplo operador.demo.
2. Revise que la tecla de mayúsculas no esté activada y vuelva a escribir la contraseña.
3. Si el problema continúa, solicite a un usuario autorizado que revise el estado de su cuenta o que restablezca su contraseña (capítulo XXIII).

> **INFORMACIÓN**
> Una cuenta en estado Inactivo no puede iniciar sesión. Solo un usuario autorizado puede activarla nuevamente desde Administración → Usuarios.

## Cerrar sesión

**Ruta: Encabezado → Cerrar sesión**

1. Termine o confirme cualquier operación que esté en proceso.
2. Seleccione Cerrar sesión, en el extremo derecho del encabezado (Figura 1).

**Figura 1. Botón Cerrar sesión en el encabezado**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> La sesión finaliza. Para volver a utilizar el sistema será necesario ingresar nuevamente las credenciales.

> **SEGURIDAD**
> Al terminar de utilizar el sistema, especialmente en computadoras compartidas, seleccione Cerrar sesión. Así evita que otra persona utilice su cuenta desde el mismo equipo.

# V. INTERFAZ GENERAL

Todas las pantallas comparten la misma organización: un menú lateral para navegar entre módulos, un encabezado con la información de la sesión y un área de trabajo donde se presentan los listados, formularios e indicadores de cada módulo. La Figura 2 identifica estas áreas en el módulo Personas y el Cuadro 5 explica cada una.

**Figura 2. Distribución general de la interfaz en el módulo Personas**

_Fuente: elaboración propia._

**Cuadro 5. Elementos de la interfaz general**

| N.º | Elemento            | Descripción                                                                                                   |
| --- | ------------------- | ------------------------------------------------------------------------------------------------------------- |
| 1   | Encabezado          | Muestra «Área administrativa» y «Gestión del servicio comunitario»; permanece visible en todas las pantallas. |
| 2   | Menú lateral        | Contiene el Menú principal con los módulos disponibles para el usuario.                                       |
| 3   | Módulo seleccionado | La opción resaltada indica el módulo que se está utilizando.                                                  |

| N.º | Elemento              | Descripción                                                                                                |
| --- | --------------------- | ---------------------------------------------------------------------------------------------------------- |
| 4   | Usuario autenticado   | Nombre de la cuenta que inició sesión.                                                                     |
| 5   | Cerrar sesión         | Finaliza la sesión de trabajo.                                                                             |
| 6   | Encabezado del módulo | Presenta el área, el nombre del módulo y una breve descripción de su propósito.                            |
| 7   | Botón principal       | Inicia la operación más frecuente del módulo, por ejemplo Nueva persona.                                   |
| 8   | Búsqueda              | Permite localizar registros al escribir un nombre o un dato de referencia.                                 |
| 9   | Listado               | Presenta los registros en forma de tabla, con sus columnas y acciones.                                     |
| 10  | Estado                | Etiqueta de color que indica la condición del registro, por ejemplo Activo.                                |
| 1   | Formulario            | Panel para crear información; algunos módulos lo muestran siempre y otros lo abren con el botón principal. |
| 12  | Tarjeta de sesión     | Muestra el nombre del usuario y la indicación Sesión activa.                                               |

_Fuente: elaboración propia._

## Menú lateral y navegación

El menú lateral presenta los módulos en este orden: Inicio, Personas, Sectores, Suministros, Solicitudes, Cuotas, Obligaciones, Jornadas, Pagos, Finanzas, Dashboard, Reportes, Abastecimiento y Administración. Para abrir un módulo, seleccione su nombre; la opción se resalta y el área de trabajo muestra el contenido correspondiente.

> **INFORMACIÓN**
> El menú se construye según los permisos de la cuenta. Dos usuarios pueden observar opciones diferentes: un usuario con todos los permisos ve los catorce módulos, mientras que un usuario con un rol limitado ve únicamente los módulos autorizados (capítulo XXV).

Cuando la ventana del navegador es pequeña, el menú muestra una barra de desplazamiento vertical para llegar a las últimas opciones.

## Elementos comunes de las pantallas

**Cuadro 6. Elementos comunes de la interfaz**

| Elemento                              | Uso                                                                                                        |
| ------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Botón principal del módulo            | Abre una operación, por ejemplo Nueva cuota, Registrar pago o Nuevo egreso.                                |
| Botones de los formularios y ventanas | Ejecutan la operación, por ejemplo Crear persona o Aprobar solicitud.                                      |
| Volver, Cancelar y Cerrar             | Cierran un formulario o una ventana sin completar la operación.                                            |
| Campos obligatorios y opcionales      | Los campos marcados con asterisco (\*) son obligatorios; los que indican (opcional) pueden dejarse vacíos. |
| Contadores de caracteres              | Indican la longitud máxima del texto, por ejemplo 0/150.                                                   |

| Elemento                           | Uso                                                                                   |
| ---------------------------------- | ------------------------------------------------------------------------------------- |
| Listas desplegables                | Permiten elegir un valor de un conjunto definido, como un sector o una persona.       |
| Búsquedas y filtros                | Reducen el listado a los registros que cumplen el criterio escrito o seleccionado.    |
| Etiquetas de estado                | Indican la condición de un registro: Activo, Pendiente, Pagada, Cerrada, entre otras. |
| Ventanas de confirmación           | Solicitan revisar la información antes de ejecutar operaciones importantes.           |
| Barra de desplazamiento horizontal | Aparece en tablas anchas; permite ver las columnas y acciones de la derecha.          |
| Mensajes                           | Informan el resultado de una operación (capítulo XXVII).                              |

_Fuente: elaboración propia._

> **RECOMENDACIÓN**
> Antes de seleccionar un botón que guarda información, revise los datos ingresados. Los mensajes de confirmación indican que la operación se completó.

## Uso desde teléfonos y tabletas

La misma aplicación web se utiliza desde teléfonos y tabletas mediante el navegador. En pantallas pequeñas, la interfaz reorganiza sus elementos verticalmente y el menú lateral se presenta como un menú desplegable que se abre desde un control de navegación. Las reglas y operaciones son las mismas que en una computadora; solo cambia la organización visual.

> **INFORMACIÓN**
> Las figuras de este manual se obtuvieron en una computadora. En un teléfono, los mismos elementos aparecen reorganizados, pero conservan sus nombres.

# VI. PANEL DE INICIO

Después de iniciar sesión, el sistema muestra el panel de inicio. Esta pantalla da la bienvenida al usuario autenticado y reúne accesos directos a los módulos habilitados para su cuenta.

**Ruta: Menú principal → Inicio**

**Figura 3. Panel de inicio con los módulos disponibles para el usuario autenticado**

_Fuente: elaboración propia._

Como se observa en la Figura 3, el panel contiene los siguientes elementos:

1. El encabezado Inicio, con el saludo «Bienvenido, operador.demo» y la descripción del propósito del sistema.
2. El recuadro Panel administrativo, que recuerda que los módulos disponibles dependen de los permisos de la cuenta.
3. La sección Módulos disponibles, con una tarjeta por cada módulo habilitado y el enlace Abrir módulo.

## Abrir un módulo desde el panel

**Ruta: Menú principal → Inicio → Abrir módulo**

1. Seleccione Inicio en el menú lateral.
2. En la sección Módulos disponibles, localice la tarjeta del módulo que desea utilizar.
3. Seleccione Abrir módulo.

> **RESULTADO ESPERADO**
> El sistema abre el módulo seleccionado y lo resalta en el menú lateral.

## Módulos según los permisos

La Figura 3 corresponde a la cuenta de demostración operador.demo, que tiene el rol Operador de cobros. Por esa razón, el panel y el menú muestran únicamente Personas, Suministros, Solicitudes, Obligaciones y Pagos. Un usuario con más permisos observa más tarjetas y más opciones en el menú.

> **INFORMACIÓN**
> La ausencia de un módulo no indica una falla del sistema: significa que la cuenta no tiene permisos para utilizarlo. Si necesita un módulo que no aparece, solicite la revisión de sus roles a un usuario autorizado.

# VII. PERSONAS

El módulo Personas conserva el registro comunitario de las personas relacionadas con el servicio. Una persona puede ser responsable de un suministro, solicitante de un nuevo servicio, participante en jornadas comunitarias, titular de obligaciones personales o integrante del Comité.

> **INFORMACIÓN — PERSONA Y SUMINISTRO**
> Una persona puede existir en el sistema aunque no posea un suministro de agua; por ejemplo, las jornadas comunitarias se aplican a personas y no a suministros. Por esa razón, registrar una persona no crea automáticamente un suministro (capítulo IX).

## Consultar personas

**Ruta: Menú principal → Personas**

1. Seleccione Personas en el menú lateral.
2. Revise el panel Personas registradas, que indica la cantidad de personas y presenta el listado (Figura 4).

**Figura 4. Listado de personas registradas**

_Fuente: elaboración propia._

El listado presenta las columnas Nombre completo (con el sector debajo del nombre, cuando existe), Identificación, Teléfono, Estado y Acciones. En Acciones se encuentran los botones Editar e Inactivar.

## Buscar una persona

**Ruta: Menú principal → Personas → Buscar por nombre**

1. Ubique el campo Buscar por nombre..., en la parte superior del listado.
2. Escriba el nombre o el apellido de la persona, completo o parcial.
3. Revise el listado, que muestra las coincidencias.

> **RECOMENDACIÓN**
> Para volver a ver todas las personas, borre el texto del campo de búsqueda. Busque siempre a la persona antes de registrarla, para evitar registros duplicados.

## Registrar una persona

**Ruta: Menú principal → Personas → Crear persona**

### Antes de comenzar

Verifique que la persona no esté registrada; utilice la búsqueda por nombre. Tenga a mano sus nombres y apellidos y, si se dispone de ellos, su sector, identificación, teléfono y dirección de referencia.

### Pasos

1. Seleccione Personas en el menú lateral.
2. Ubique el formulario Crear persona, a la derecha del listado. También puede seleccionar Nueva persona.
3. Complete los campos Nombres y Apellidos.
4. En Sector, seleccione el sector de la persona o conserve la opción «Sin sector asignado».
5. Complete, si corresponde, los campos opcionales Identificación, Teléfono y Dirección de referencia.
6. Revise la información ingresada.
7. Seleccione Crear persona.

| **Figura 5. Formulario para registrar una persona** Fuente: elaboración propia. | Campos del formulario Nombres y Apellidos: datos de identificación de la persona, hasta 150 caracteres cada uno. Sector (opcional): sector administrativo de la persona; puede quedar «Sin sector asignado». Identificación (opcional): número de documento, hasta 50 caracteres. Teléfono (opcional): número de contacto, hasta 30 caracteres. Dirección de referencia (opcional): ubicación descrita con las referencias de la comunidad, hasta 500 caracteres. El formulario recuerda: «La identificación y el teléfono son opcionales.» |
| --------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

> **RESULTADO ESPERADO**
> La persona aparece en Personas registradas con el estado Activo y queda disponible para las operaciones autorizadas.

> **INFORMACIÓN**
> El registro de una persona no implica la creación de un suministro. Para asociarla con un servicio, asígnela como responsable de un suministro (capítulo XI) o registre una solicitud de nuevo servicio (capítulo XII).

## Editar la información de una persona

**Ruta: Menú principal → Personas → Editar**

1. Busque a la persona en el listado.
2. Seleccione Editar en su fila.
3. Modifique los datos necesarios en el formulario que presenta el sistema.
4. Revise los cambios y confírmelos con el botón de guardado del formulario.

> **RESULTADO ESPERADO**
> El listado muestra la información actualizada de la persona.

## Estado de una persona

La columna Estado indica la condición de la persona; los registros nuevos inician con el estado

Activo. El botón Inactivar cambia a la persona a una condición inactiva sin eliminar su registro.

**Ruta: Menú principal → Personas → Inactivar**

1. Busque a la persona en el listado.
2. Seleccione Inactivar en su fila.
3. Confirme la operación si el sistema lo solicita.

> **RESULTADO ESPERADO**
> La columna Estado refleja la nueva condición de la persona.

> **INFORMACIÓN**
> Las listas de selección de otros módulos trabajan con personas activas: la generación de obligaciones personales muestra la lista Personas activas, y la asignación de integrantes del Comité informa cuando «No hay personas activas disponibles con ese criterio.»

> **PRECAUCIÓN**
> Antes de inactivar a una persona, verifique que no deba participar en procesos vigentes, como jornadas comunitarias, obligaciones personales o cargos del Comité.

## Sector asociado a una persona

El campo Sector permite asociar administrativamente a una persona con un sector, aunque no posea un suministro. Cuando existe, el sector aparece debajo del nombre en el listado, por ejemplo

«Sector: Sector Demostración Norte».

> **INFORMACIÓN**
> El sector de una persona y el sector de un suministro son datos independientes. Asignar un sector a una persona no modifica el sector de los suministros de los que sea responsable.

# VIII. SECTORES

Los sectores organizan territorialmente el servicio de agua potable. Se utilizan para clasificar los suministros, asociar administrativamente a las personas y programar el abastecimiento. Durante el diagnóstico del proyecto, la comunidad identificó cinco sectores: Sector 1, Sector 2, Sector 3, Chuacanac y Panacal. Las figuras utilizan sectores ficticios de demostración.

## Consultar sectores

**Ruta: Menú principal → Sectores**

**Figura 6. Módulo Sectores: listado y formulario de creación**

_Fuente: elaboración propia._

La pantalla Sectores (Figura 6) presenta el panel Sectores registrados, con las columnas

Nombre, Descripción, Estado y Acciones (Editar e Inactivar), y el formulario Crear sector.

## Crear un sector

**Ruta: Menú principal → Sectores → Crear sector**

1. Seleccione Sectores en el menú lateral.
2. En el panel Crear sector, ingrese el Nombre del sector, hasta 100 caracteres. También puede seleccionar Nuevo sector.
3. Si lo desea, escriba una Descripción (opcional, hasta 500 caracteres).
4. Seleccione Crear sector.

> **RESULTADO ESPERADO**
> El sector aparece en Sectores registrados con el estado Activo y queda disponible en las listas de sectores de los demás módulos.

## Editar un sector

**Ruta: Menú principal → Sectores → Editar**

1. Localice el sector en el listado.
2. Seleccione Editar.
3. Modifique el nombre o la descripción y confirme los cambios con el botón de guardado del formulario.

> **RESULTADO ESPERADO**
> El listado muestra los datos actualizados del sector.

## Inactivar un sector

**Ruta: Menú principal → Sectores → Inactivar**

1. Localice el sector en el listado.
2. Seleccione Inactivar.
3. Confirme la operación si el sistema lo solicita.

> **PRECAUCIÓN**
> Inactive un sector solo cuando deje de formar parte de la organización territorial del servicio. Antes de hacerlo, revise los suministros, las personas y la programación de abastecimiento asociados.

## Relación con otros módulos

1. Suministros: cada suministro se registra en un sector.
2. Personas: una persona puede tener un sector asociado (opcional).
3. Solicitudes: cada solicitud de nuevo servicio indica el sector del futuro suministro.
4. Abastecimiento: la programación se define por sector.
5. Dashboard: la recaudación por sector agrupa los pagos de obligaciones de suministros.

# IX. SUMINISTROS

Un suministro representa un servicio de agua potable registrado en el sistema. Cada suministro tiene su propia identidad administrativa, formada por un NIS y un código QR, y se relaciona con un sector, una dirección de referencia, un responsable vigente, un historial de responsables y un estado.

**Cuadro 7. Diferencias entre persona y suministro**

| Concepto       | Persona                                         | Suministro                                                   |
| -------------- | ----------------------------------------------- | ------------------------------------------------------------ |
| Representa     | Al individuo registrado en la comunidad.        | Al servicio de agua potable registrado.                      |
| Identificación | Nombres, apellidos e identificación (opcional). | NIS (por ejemplo, PAN-000001) y código QR.                   |
| Sector         | Opcional.                                       | Obligatorio al registrarlo.                                  |
| Obligaciones   | Obligaciones personales y de jornadas.          | Obligaciones de las cuotas del servicio.                     |
| Relación       | Puede ser responsable de suministros.           | Tiene un responsable vigente y un historial de responsables. |

_Fuente: elaboración propia._

## Consultar suministros

**Ruta: Menú principal → Suministros**

**Figura 7. Listado de suministros registrados**

_Fuente: elaboración propia._

El panel Suministros registrados (Figura 7) presenta las columnas NIS, Sector, Dirección, Responsable actual y Estado. Las acciones de cada fila incluyen Ver QR, Editar y Responsables; más a la derecha se encuentran Asignar responsable y Cancelar.

> **RECOMENDACIÓN**
> Si no observa todas las acciones de una fila, utilice la barra de desplazamiento horizontal ubicada debajo del listado.

## Buscar un suministro por NIS

**Ruta: Menú principal → Suministros → Buscar por NIS**

1. En el panel Buscar por NIS, escriba el número completo, por ejemplo «PAN-000002».
2. Seleccione Buscar.
3. Revise el resultado en el listado.
4. Para volver a ver todos los suministros, seleccione Limpiar.

> **RESULTADO ESPERADO**
> El listado muestra el suministro que corresponde al NIS ingresado.

> **INFORMACIÓN**
> El NIS tiene el formato PAN-000001: el prefijo PAN, un guion y seis dígitos (capítulo X).

## Registrar un suministro

**Ruta: Menú principal → Suministros → Crear suministro**

### Antes de comenzar

Verifique que el sector exista y se encuentre activo. Cuando el registro proviene del trámite de una persona, también puede utilizar una solicitud de nuevo servicio (capítulo XII).

### Pasos

1. Seleccione Suministros en el menú lateral.
2. Ubique el formulario Crear suministro. También puede seleccionar Nuevo suministro.
3. En Sector, seleccione el sector al que pertenece el suministro.
4. En Dirección de referencia, describa la ubicación del suministro según las referencias de la comunidad.
5. Seleccione Crear suministro.

| **Figura 8. Formulario para registrar un suministro** Fuente: elaboración propia. | Campos del formulario Sector: sector territorial del suministro; es obligatorio. Dirección de referencia: descripción de la ubicación del servicio, hasta 500 caracteres. El NIS no se escribe: «El NIS se genera automáticamente y no puede editarse.» |
| ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

**Figura 9. Confirmación del registro del suministro PAN-000001**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Suministro creado con NIS PAN-000001.» (con el número asignado) y el suministro aparece en el listado con el responsable «Sin responsable» y el estado Activo (Figura 9).

> **INFORMACIÓN**
> El suministro se crea sin responsable. Para asociarlo con una persona, utilice Asignar responsable (capítulo XI).

## Editar un suministro

**Ruta: Menú principal → Suministros → Editar**

1. Localice el suministro en el listado.
2. Seleccione Editar.
3. En el panel Editar suministro, observe que el NIS se presenta como dato de solo lectura.
4. Modifique los datos administrativos permitidos, como el Sector, y confirme los cambios con el botón de guardado del formulario.

> **RESULTADO ESPERADO**
> El listado muestra los datos actualizados; el NIS y el código QR del suministro no cambian.

## Estado y cancelación administrativa

La columna Estado indica la condición del suministro; los suministros nuevos inician con el estado Activo. La acción Cancelar del listado corresponde a la cancelación administrativa del suministro, operación que conserva su historial.

> **PRECAUCIÓN**
> La cancelación administrativa cambia la condición del servicio. Utilícela solo cuando el Comité lo haya decidido y verifique antes el NIS del suministro seleccionado.

# X. NIS Y CÓDIGO QR

El sistema utiliza dos mecanismos para identificar un suministro: el Número de Identificación de Suministro (NIS), pensado para la identificación manual, y el código QR, pensado para la lectura con la cámara de un dispositivo. Ambos permiten recuperar el mismo registro de suministro.

## Número de Identificación de Suministro (NIS)

El NIS identifica permanentemente a cada suministro. Su formato es PAN-000001: el prefijo PAN, un guion y seis dígitos consecutivos. Los suministros de las figuras utilizan, por ejemplo, PAN-000001 y PAN-000002.

**Cuadro 8. Características del NIS**

| Característica           | Descripción                                                                          |
| ------------------------ | ------------------------------------------------------------------------------------ |
| Automático               | El sistema lo genera al crear el suministro; no se escribe ni se edita.              |
| Único                    | Dos suministros nunca comparten el mismo NIS.                                        |
| Pertenece al suministro  | Identifica el servicio, no a la persona responsable.                                 |
| Permanente               | No cambia cuando cambia el responsable ni cuando se editan los datos del suministro. |
| No reutilizable          | No se asigna a otro suministro.                                                      |
| Independiente del sector | El sector no forma parte del NIS.                                                    |

_Fuente: elaboración propia._

> **INFORMACIÓN**
> Un nuevo NIS se genera únicamente cuando se registra un nuevo suministro, ya sea desde Suministros o al aprobar una solicitud de nuevo servicio.

## Código QR del suministro

Cada suministro tiene un código QR asociado. El código contiene un identificador técnico aleatorio y único que permite al sistema localizar el suministro. En particular, el código QR:

1. identifica técnicamente un suministro;
2. no contiene directamente datos personales ni financieros;
3. no constituye una contraseña ni permite iniciar sesión;
4. permite recuperar el mismo suministro que el NIS.

## Ver el código QR de un suministro

**Ruta: Menú principal → Suministros → Ver QR**

1. Seleccione Suministros en el menú lateral.
2. Localice el suministro en el listado o mediante la búsqueda por NIS.
3. Seleccione Ver QR en la fila del suministro.
4. Revise la ventana Código QR del suministro, que muestra el NIS y el código (Figura 10).
5. Seleccione Cerrar para volver al listado.

**Figura 10. Código QR del suministro PAN-000001**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> La ventana muestra el código QR y la leyenda «Este código identifica técnicamente el suministro. No contiene información personal ni financiera.»

## Consultar un suministro mediante el código QR

El código QR contiene un enlace hacia la consulta del suministro dentro de la plataforma, acompañado del identificador técnico. Al leerlo con la cámara o con una aplicación lectora de códigos QR, el dispositivo abre en el navegador la consulta del suministro correspondiente.

La consulta mediante QR presenta las obligaciones pendientes propias del suministro y las obligaciones personales pendientes de su responsable vigente. No incluye obligaciones pagadas o anuladas ni obligaciones de responsables anteriores.

> **PRECAUCIÓN**
> El enlace del código QR incorpora la dirección web del sistema desde el cual se generó. Utilice y reproduzca únicamente los códigos generados desde la instalación definitiva del sistema; los códigos generados en un entorno de pruebas no funcionarán en otra instalación.

> **SEGURIDAD**
> Leer un código QR no autoriza a realizar operaciones. El registro de pagos y las demás operaciones administrativas siguen sujetos a la autenticación y a los permisos de la cuenta.

## Comparación entre NIS y código QR

**Cuadro 9. Comparación entre NIS y código QR**

| Aspecto                        | NIS                                    | Código QR                                      |
| ------------------------------ | -------------------------------------- | ---------------------------------------------- |
| Forma de uso                   | Se escribe o se lee visualmente.       | Se lee con la cámara de un dispositivo.        |
| Contenido                      | Prefijo PAN, guion y seis dígitos.     | Enlace con un identificador técnico aleatorio. |
| Dónde se consulta              | Columna NIS y búsqueda Buscar por NIS. | Botón Ver QR del suministro.                   |
| Resultado                      | Localiza el suministro.                | Localiza el mismo suministro.                  |
| Datos personales o financieros | No contiene.                           | No contiene.                                   |
| Cambio de responsable          | Se conserva.                           | Se conserva.                                   |

_Fuente: elaboración propia._

# XI. RESPONSABLE DEL SUMINISTRO

El responsable es la persona asociada con un suministro en un momento determinado. El sistema conserva el historial de responsables, de manera que un cambio no elimina la relación anterior ni modifica la identidad del suministro.

> **INFORMACIÓN**
> Cambiar el responsable no crea un nuevo suministro, ni un nuevo NIS, ni un nuevo código QR. Esos elementos se crean únicamente al registrar un nuevo suministro.

## Asignar el responsable de un suministro

**Ruta: Menú principal → Suministros → Asignar responsable**

### Antes de comenzar

Verifique que la persona esté registrada y activa en el módulo Personas.

### Pasos

1. Seleccione Suministros en el menú lateral.
2. Localice el suministro. Si es necesario, desplace horizontalmente el listado para ver todas las acciones.
3. Seleccione Asignar responsable.
4. En el panel Asignar responsable, que advierte «El NIS y la identidad del suministro se conservarán.», seleccione a la persona en la lista Selecciona una persona.
5. Seleccione Guardar responsable. Para salir sin cambios, seleccione Cancelar.

**Figura 11. Suministro con responsable asignado**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Responsable actualizado correctamente.» y la columna Responsable actual presenta el nombre de la persona (Figura 11).

## Cambiar el responsable

El cambio se realiza con la misma opción de asignación. Al guardar una persona distinta, el sistema finaliza la relación vigente, registra la nueva relación y conserva la anterior en el historial.

**Ruta: Menú principal → Suministros → Asignar responsable**

1. Localice el suministro y seleccione Asignar responsable.
2. Seleccione a la nueva persona responsable.
3. Seleccione Guardar responsable.
4. Seleccione Responsables para verificar el historial actualizado.

> **RESULTADO ESPERADO**
> La nueva persona aparece como responsable vigente; el NIS y el código QR del suministro no cambian.

> **PRECAUCIÓN**
> Verifique el NIS del suministro antes de guardar. Un cambio aplicado al suministro equivocado altera su información administrativa.

## Consultar el historial de responsables

**Ruta: Menú principal → Suministros → Responsables**

1. Localice el suministro en el listado.
2. Seleccione Responsables.
3. Revise la ventana Historial de responsables (Figura 12).
4. Seleccione Cerrar.

**Figura 12. Historial de responsables del suministro**

_Fuente: elaboración propia._

Cada registro del historial muestra el nombre de la persona y las fechas de Inicio y Fin. La relación actual se identifica con la etiqueta Vigente y la indicación «Fin: Actual»; las relaciones anteriores conservan su fecha de finalización.

# XII. SOLICITUDES DE NUEVO SERVICIO

El módulo Solicitudes administra las solicitudes para registrar nuevos suministros de agua. Una solicitud indica la persona solicitante, el sector y la dirección de referencia del futuro servicio. Al aprobarla, el sistema crea el suministro con su propio NIS y código QR.

**Cuadro 10. Estados de una solicitud**

| Estado    | Significado                                                                                           |
| --------- | ----------------------------------------------------------------------------------------------------- |
| Pendiente | La solicitud fue registrada y espera resolución. Muestra las acciones Aprobar y Rechazar.             |
| Aprobada  | La solicitud fue aprobada y el sistema creó el suministro, cuyo NIS aparece en la columna Suministro. |

_Fuente: elaboración propia._

## Registrar una solicitud

**Ruta: Menú principal → Solicitudes → Nueva solicitud**

### Antes de comenzar

Verifique que la persona solicitante esté registrada y activa, y que el sector exista.

### Pasos

1. Seleccione Solicitudes en el menú lateral.
2. Seleccione Nueva solicitud. El panel Nueva solicitud indica que «La solicitud comenzará en estado Pendiente.»
3. En Persona solicitante, seleccione a la persona.
4. En Sector, seleccione el sector del futuro suministro.
5. En Dirección de referencia, describa la ubicación del servicio (hasta 500 caracteres).
6. Si es necesario, escriba una Observación (opcional, hasta 1000 caracteres).
7. Seleccione Crear solicitud. Para salir sin registrar, seleccione Volver o Cerrar.

**Figura 13. Formulario para registrar una solicitud de nuevo servicio**

_Fuente: elaboración propia._

Los campos marcados con asterisco (\*) en la Figura 13 son obligatorios.

**Figura 14. Solicitud registrada en estado Pendiente**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Solicitud creada correctamente.» y la solicitud aparece con el estado Pendiente, todavía sin suministro asociado (Figura 14).

## Aprobar una solicitud

**Ruta: Menú principal → Solicitudes → Aprobar**

1. Localice la solicitud con estado Pendiente.
2. Seleccione Aprobar.
3. En la ventana Aprobar solicitud, revise el solicitante, el sector y la dirección (Figura 15).
4. Lea la advertencia «Al aprobar se creará un nuevo suministro con NIS y código QR propios.»
5. Si lo desea, escriba una Observación (opcional).
6. Seleccione Aprobar solicitud. Para salir sin aprobar, seleccione Volver.

**Figura 15. Ventana para aprobar una solicitud**

_Fuente: elaboración propia._

> **PRECAUCIÓN**
> La aprobación crea un suministro con identidad permanente. Revise cuidadosamente la información antes de aprobar.

**Figura 16. Solicitud aprobada con el suministro PAN-000002 creado**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Solicitud aprobada. Se creó el suministro PAN-000002.». La solicitud cambia a Aprobada, la columna Suministro presenta el NIS creado y la columna Acciones indica la fecha y el usuario que la resolvió (Figura 16).

> **INFORMACIÓN**
> El nuevo suministro aparece en el módulo Suministros con la persona solicitante como responsable actual.

## Rechazar una solicitud

**Ruta: Menú principal → Solicitudes → Rechazar**

1. Localice la solicitud con estado Pendiente.
2. Seleccione Rechazar.
3. Confirme la operación en la ventana que presenta el sistema.

> **INFORMACIÓN**
> Rechazar una solicitud no crea ningún suministro.

## Consultar las solicitudes

El panel Solicitudes registradas muestra las columnas Solicitante, Sector, Dirección, Fecha, Estado, Suministro y Acciones. Las solicitudes resueltas ya no presentan botones; en su lugar indican cuándo y por quién fueron resueltas, por ejemplo «Resuelta el 01/10/2026, 22:25 por demo.admin».

# XIII. CUOTAS

Una cuota es la configuración administrativa de un cobro: define su nombre, monto, periodicidad y vigencia. A partir de una cuota, el sistema genera obligaciones para los suministros. Durante el diagnóstico, el Comité indicó que la cuota ordinaria vigente era de Q30 anuales; el sistema permite configurar este valor sin modificar el programa.

**Cuadro 11. Diferencia entre cuota y obligación**

| Concepto            | Cuota                                     | Obligación                                                      |
| ------------------- | ----------------------------------------- | --------------------------------------------------------------- |
| Qué es              | Configuración administrativa de un cobro. | Compromiso económico generado para un titular.                  |
| Ejemplo             | Cuota demostrativa 2026: Q 30.00, Anual.  | Cuota 2026 del suministro PAN-000001 por Q 30.00.               |
| Dónde se administra | Módulo Cuotas.                            | Módulo Obligaciones.                                            |
| Monto               | Valor configurado vigente.                | Monto copiado al generarla; se conserva aunque la cuota cambie. |
| ¿Se paga?           | No.                                       | Sí, completa, en el módulo Pagos.                               |

_Fuente: elaboración propia._

## Consultar cuotas

**Ruta: Menú principal → Cuotas**

**Figura 17. Listado de cuotas registradas**

_Fuente: elaboración propia._

El panel Cuotas registradas (Figura 17) muestra las columnas Cuota (nombre y descripción), Monto, Periodicidad, Vigencia, Estado y Acciones. El campo Buscar cuota... permite localizar una cuota por su nombre.

## Crear una cuota

**Ruta: Menú principal → Cuotas → Crear cuota**

1. Seleccione Cuotas en el menú lateral.
2. Ubique el formulario Crear cuota. También puede seleccionar Nueva cuota.
3. Ingrese el Nombre de la cuota (hasta 100 caracteres) y, si lo desea, una Descripción.
4. Ingrese el Monto (Q), por ejemplo 30.00.
5. Seleccione la Periodicidad, por ejemplo Anual.
6. Indique la fecha de Inicio de vigencia y, si corresponde, la de Fin de vigencia.
7. Revise los datos y seleccione Crear cuota.

| **Figura 18. Formulario para crear una cuota** Fuente: elaboración propia. | Campos del formulario Nombre: identifica la cuota en las listas. Monto (Q): valor en quetzales que se copiará a cada obligación generada. Periodicidad: frecuencia del cobro, por ejemplo Anual. Inicio de vigencia: fecha desde la cual se aplica la cuota. Fin de vigencia (opcional): si se deja vacío, la vigencia aparece como «Sin fecha de fin». |
| ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |

> **RESULTADO ESPERADO**
> El sistema muestra «Cuota creada correctamente.» y la cuota aparece en el listado con el estado Activa.

> **INFORMACIÓN**
> «El monto se copiará y conservará en cada obligación cuando sea generada.» Por lo tanto, un cambio posterior en la cuota no modifica las obligaciones ya generadas.

## Editar o inactivar una cuota

La columna Acciones contiene los botones Editar e Inactivar. Utilice Editar para corregir los datos de la cuota e Inactivar cuando deje de aplicarse. Si la tabla no muestra completos estos botones, utilice la barra de desplazamiento horizontal.

> **PRECAUCIÓN**
> Antes de modificar o inactivar una cuota, confirme la decisión administrativa del Comité. Las obligaciones ya generadas conservan el monto con el que fueron creadas.

# XIV. OBLIGACIONES

Una obligación es un compromiso económico registrado a nombre de un titular. El titular puede ser un suministro, como en la cuota anual del servicio, o una persona, como en un aporte comunitario o en una ausencia a una jornada. Cada obligación se paga completa o permanece pendiente.

**Cuadro 12. Tipos de obligaciones según su origen**

| Origen                    | Titular    | Cómo se genera                                              | Ejemplo                                                        |
| ------------------------- | ---------- | ----------------------------------------------------------- | -------------------------------------------------------------- |
| Cuota                     | Suministro | Con Generar obligación, desde una cuota.                    | Cuota demostrativa 2026 de PAN-000001, Q 30.00.                |
| Administrativa (personal) | Persona    | En Generar obligación personal.                             | Aporte comunitario demostrativo, Q 20.00.                      |
| Ausencia a jornada        | Persona    | Al cerrar una jornada con monto por ausencia (capítulo XV). | Ausencia a jornada: Jornada comunitaria demostrativa, Q 25.00. |

_Fuente: elaboración propia._

## Consultar obligaciones

**Ruta: Menú principal → Obligaciones**

**Figura 19. Listado de obligaciones registradas**

_Fuente: elaboración propia._

El panel Obligaciones registradas (Figura 19) contiene las siguientes columnas:

1. Concepto: nombre de la obligación y, debajo, su origen, por ejemplo «Cuota: …» o
   «Administrativa».

1. Titular: NIS y sector del suministro, o la referencia de la persona.
1. Período: período al que corresponde la obligación, por ejemplo 2026.
1. Monto: valor de la obligación.
1. Fechas: fecha de generación y fecha de vencimiento.
1. Situación: Pendiente o Pagada.
1. Acciones: Anular en las obligaciones pendientes; las pagadas indican «Sin acciones disponibles».
   Para localizar una obligación, escriba un concepto o un dato del titular en Buscar obligación.... La indicación «La morosidad mostrada proviene del servidor» recuerda que la situación de cada obligación corresponde a la información registrada en el sistema.

## Generar una obligación desde una cuota

**Ruta: Menú principal → Obligaciones → Generar obligación**

### Antes de comenzar

Verifique que la cuota esté activa y que el suministro esté registrado.

### Pasos

1. Seleccione Obligaciones en el menú lateral.
2. Seleccione Generar obligación. El sistema abre el panel Generar desde cuota.
3. En Cuota, seleccione la cuota; la lista muestra su nombre, monto y periodicidad.
4. En Suministro, seleccione el suministro por su NIS y sector.
5. En Período, escriba el año con cuatro dígitos («Formato anual: YYYY»), por ejemplo 2026.
6. Si corresponde, indique la Fecha de vencimiento (opcional).
7. Revise el recuadro Referencia de la cuota.
8. Seleccione Generar obligación. Para salir sin generar, seleccione Volver o Cerrar.

**Figura 20. Panel para generar una obligación desde una cuota**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Obligación generada correctamente. El monto quedó registrado con el valor actual de la cuota.» y la obligación aparece como Pendiente en el listado.

> **INFORMACIÓN**
> La obligación se asigna al suministro y conserva el monto vigente de la cuota al momento de generarla (Figura 20).

## Generar obligaciones personales

Este procedimiento registra una obligación independiente para cada persona seleccionada. Se utiliza para cobros administrativos que corresponden a personas, por ejemplo aportes comunitarios.

**Ruta: Menú principal → Obligaciones → Generar obligación personal**

1. Seleccione Obligaciones en el menú lateral.
2. En el panel Generar obligación personal, ingrese el Concepto.
3. Ingrese el Monto por persona.
4. Si corresponde, indique el Periodo y la Fecha de vencimiento (opcionales).
5. En Personas activas, marque las casillas de las personas. Utilice Buscar persona... para localizarlas, Seleccionar todas para marcarlas todas o Limpiar selección para desmarcarlas.
6. Verifique el contador de personas seleccionadas.
7. Seleccione Generar obligación personal.

**Figura 21. Panel para generar obligaciones personales**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Se generaron 1 obligaciones correctamente.», con la cantidad de obligaciones creadas, y cada obligación aparece como Pendiente (Figura 19).

> **INFORMACIÓN**
> Cuando se seleccionan varias personas, el sistema crea una obligación independiente para cada una; no se crea una obligación compartida.

## Anular una obligación

El botón Anular aparece en las obligaciones pendientes. La anulación es una operación administrativa sensible: utilícela únicamente para corregir obligaciones generadas por error y de acuerdo con las decisiones del Comité.

> **PRECAUCIÓN**
> Revise el concepto, el titular y el monto antes de anular una obligación. Las obligaciones pagadas no muestran acciones disponibles.

# XV. JORNADAS COMUNITARIAS

Las jornadas comunitarias son actividades de trabajo para el mantenimiento del servicio. El módulo permite programarlas, registrar a sus participantes, controlar la asistencia y cerrar la actividad para que el sistema genere las obligaciones que correspondan.

> **INFORMACIÓN**
> Las jornadas se aplican a personas, no a suministros. Una persona puede participar en jornadas aunque no posea un suministro de agua.

**Cuadro 13. Estados de una jornada**

| Estado      | Significado                                                                                                                 |
| ----------- | --------------------------------------------------------------------------------------------------------------------------- |
| Planificada | La jornada está registrada; se pueden agregar participantes y registrar su asistencia.                                      |
| Cerrada     | La actividad finalizó: la participación queda como información de consulta y el sistema generó las obligaciones aplicables. |

_Fuente: elaboración propia._

## Crear una jornada

**Ruta: Menú principal → Jornadas → Crear jornada**

1. Seleccione Jornadas en el menú lateral.
2. Ubique el formulario Crear jornada. También puede seleccionar Nueva jornada.
3. Ingrese el Nombre de la jornada y, si lo desea, una Descripción.
4. Seleccione la Fecha.
5. Indique la hora de Inicio y de Fin y la Ubicación (opcionales).
6. Si las ausencias deben generar un cobro, ingrese el Monto por ausencia (Q).
7. Seleccione Crear jornada.

| **Figura 22. Formulario para crear una jornada comunitaria** Fuente: elaboración propia. | Campos del formulario Nombre y Fecha: identifican la actividad. Inicio y Fin (opcionales): horario previsto. Ubicación (opcional): lugar de la actividad. Monto por ausencia (Q) (opcional): valor de la obligación por ausencia. El formulario advierte: «El monto es opcional. Si no se configura, las ausencias no generarán obligación económica.» |
| ------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |

**Figura 23. Jornada registrada en estado Planificada**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Jornada creada correctamente.» y la jornada aparece en Jornadas registradas con el estado Planificada (Figura 23).

## Gestionar participantes

**Ruta: Menú principal → Jornadas → Gestionar**

1. En Jornadas registradas, localice la jornada y seleccione Gestionar.
2. En la sección Agregar participantes, busque a las personas con Buscar persona....
3. Marque a las personas que participarán. Puede utilizar Seleccionar todas o Limpiar selección.
4. Seleccione Agregar seleccionadas.

**Figura 24. Panel de gestión de una jornada**

_Fuente: elaboración propia._

El panel de gestión (Figura 24) muestra el nombre y el estado de la jornada, su fecha, horario y ubicación, y los indicadores Participantes, Pendientes (participantes sin resultado registrado) y Monto por ausencia. Cuando todas las personas activas ya fueron agregadas, el sistema indica «No hay personas activas disponibles para agregar.»

## Registrar asistencia o ausencia

**Ruta: Menú principal → Jornadas → Gestionar → Participación**

1. En la sección Participación, localice a la persona.
2. En la lista de resultado, seleccione Participó o Ausencia.
3. Si lo desea, escriba una observación en el campo de texto de la fila.
4. Seleccione Guardar en la fila de la persona.
5. Repita los pasos para cada participante.

**Figura 25. Registro de participación y ausencia**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Participación actualizada correctamente.» y la etiqueta de cada persona refleja el resultado registrado (Figura 25).

> **RECOMENDACIÓN**
> Para quitar a una persona agregada por error, seleccione Retirar en su fila antes de cerrar la jornada.

## Cerrar la jornada

**Ruta: Menú principal → Jornadas → Gestionar → Cerrar jornada**

### Antes de comenzar

Verifique que todos los participantes tengan un resultado registrado y que el Monto por ausencia sea el correcto.

### Pasos

1. Abra el panel de la jornada con Gestionar.
2. Revise la participación de cada persona.
3. Seleccione Cerrar jornada.

**Figura 26. Jornada cerrada con la obligación generada por ausencia**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Jornada cerrada correctamente. Las obligaciones aplicables fueron generadas por el servidor.». La jornada cambia a Cerrada y la persona ausente muestra una etiqueta con la obligación generada, por ejemplo «Obligación #3» (Figura 26).

> **PRECAUCIÓN — AUSENCIAS Y OBLIGACIONES**
> Una ausencia no necesariamente debe generar automáticamente una obligación económica; esta se genera cuando las condiciones configuradas determinan que corresponde. Por ejemplo, si la jornada no tiene Monto por ausencia, las ausencias quedan registradas pero no generan cobro.

> **PRECAUCIÓN**
> Después del cierre, la participación se presenta solo para consulta. Revise todos los resultados antes de cerrar la jornada.

## Obligaciones generadas por ausencia

Las obligaciones generadas al cerrar una jornada aparecen en el módulo Obligaciones con el concepto «Ausencia a jornada: …», a nombre de la persona y con el monto por ausencia

configurado (Figura 27). Una misma persona puede acumular varias obligaciones de jornadas pendientes; cada una se conserva como un registro independiente.

**Figura 27. Obligación generada por ausencia a una jornada**

_Fuente: elaboración propia._

## Otras acciones de una jornada

La columna Acciones de Jornadas registradas incluye también Editar, para corregir los datos de una jornada. El panel de gestión presenta además Cancelar jornada, destinado a las actividades que no se realizarán.

> **PRECAUCIÓN**
> Utilice Cancelar jornada solo cuando la actividad no vaya a realizarse. Para finalizar una jornada realizada, utilice Cerrar jornada.

# XVI. PAGOS

El módulo Pagos registra la cancelación de obligaciones. Cada pago corresponde a un solo titular —un suministro o una persona— y puede cancelar una o varias obligaciones completas de ese titular. Al registrarlo, el sistema genera un comprobante y el ingreso financiero correspondiente.

## Reglas del registro de pagos

> **PRECAUCIÓN — REGLAS DE COBRO**
> No existen pagos parciales, sobrepagos ni saldos a favor. Cada obligación seleccionada se cancela completa o permanece pendiente. Un pago puede cancelar varias obligaciones completas del mismo titular únicamente cuando el monto coincide exactamente con la suma de las obligaciones seleccionadas. El sistema calcula automáticamente el Total exacto: «No se admiten pagos parciales ni montos distintos al total seleccionado.»

## Consultar pagos

**Ruta: Menú principal → Pagos**

El panel Pagos registrados muestra las columnas Comprobante, Fecha, Titular (con su tipo: persona o suministro), Concepto, Monto y Acciones (Ver detalle y Anular). El campo Buscar pago... permite localizar un pago (Figura 30).

## Registrar un pago

**Ruta: Menú principal → Pagos → Registrar pago**

### Antes de comenzar

Verifique que el titular tenga obligaciones pendientes y confirme con la persona que realiza el pago cuáles obligaciones desea cancelar.

### Pasos

1. Seleccione Pagos en el menú lateral.
2. Seleccione Registrar pago. El sistema abre el panel Registrar pago.
3. En Titular, seleccione la persona o el suministro. La lista indica el tipo de titular, por ejemplo «Obligación personal».
4. Revise el recuadro Titular seleccionado.
5. En Obligaciones pendientes, marque las obligaciones que se cancelarán. Solo puede seleccionar obligaciones completas de ese titular; Quitar selección desmarca todas.
6. En Concepto, seleccione el concepto del pago, por ejemplo «Pago de varias obligaciones».
7. Revise el resumen: cantidad de obligaciones seleccionadas y Total exacto.
8. Seleccione Revisar pago.

**Figura 28. Panel para registrar un pago de varias obligaciones**

_Fuente: elaboración propia._

En el ejemplo de la Figura 28, la persona tiene dos obligaciones pendientes, de Q 20.00 y Q 25.00; al seleccionar ambas, el Total exacto es Q 45.00.

## Confirmar el pago

**Ruta: Menú principal → Pagos → Registrar pago → Revisar pago**

1. En la ventana Confirmar pago, verifique el nombre del titular (Figura 29).
2. Revise cada obligación y el Total.
3. Seleccione Registrar pago. Para corregir la selección, seleccione Volver.

**Figura 29. Ventana de confirmación del pago**

_Fuente: elaboración propia._

> **INFORMACIÓN**
> «Al confirmar, las obligaciones seleccionadas quedarán pagadas y el sistema generará un comprobante.»

**Figura 30. Pago registrado con su número de comprobante**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Pago registrado correctamente. Comprobante PAG-000001.», con el número de comprobante generado, y el pago aparece en Pagos registrados (Figura 30).

## Consultar el detalle de un pago

**Ruta: Menú principal → Pagos → Ver detalle**

1. Localice el pago en Pagos registrados.
2. Seleccione Ver detalle.

**Figura 31. Detalle de un pago registrado**

_Fuente: elaboración propia._

El detalle (Figura 31) presenta el número de comprobante con su estado (Registrado), el concepto, la fecha de registro y el Total pagado, además de Titular, Tipo, NIS («No aplica» en los pagos de personas), Registrado por y la cantidad de Obligaciones. La sección Obligaciones canceladas enumera cada obligación con su monto. Debajo del detalle se presenta el comprobante (capítulo XVII).

## Pago de obligaciones de un suministro

Cuando el titular es un suministro, el procedimiento es el mismo: seleccione el suministro en Titular y marque sus obligaciones pendientes. En el detalle, el Tipo indica «Suministro» y el NIS muestra el número del suministro (Figura 32).

**Figura 32. Detalle del pago de una obligación de suministro**

_Fuente: elaboración propia._

## Efecto del pago en las obligaciones

Después del registro, las obligaciones canceladas cambian su Situación a Pagada y ya no muestran acciones disponibles (Figura 33). Las obligaciones no seleccionadas permanecen Pendiente.

**Figura 33. Obligaciones canceladas después del pago**

_Fuente: elaboración propia._

> **INFORMACIÓN**
> Cuando no hay obligaciones por cobrar, el sistema informa «No existen obligaciones pendientes disponibles para registrar un pago.» y el botón Registrar pago no está disponible.

## Anular un pago

La columna Acciones de Pagos registrados presenta el botón Anular a los usuarios autorizados. La anulación cambia la condición administrativa del pago sin eliminar su registro: los pagos anulados se excluyen de los totales del dashboard y se distinguen en el reporte de pagos.

> **PRECAUCIÓN**
> La anulación de un pago modifica información financiera. Utilícela solo para corregir errores comprobados y con autorización del Comité, y verifique el número de comprobante antes de confirmar. La operación queda registrada en la auditoría.

# XVII. COMPROBANTES DE PAGO

Cada pago registrado genera un comprobante. El comprobante se identifica con un número correlativo con el formato PAG-000001 y resume la información del pago.

## Contenido del comprobante

**Figura 34. Comprobante de pago en pantalla**

_Fuente: elaboración propia._

**Cuadro 14. Información del comprobante de pago**

| Sección                 | Contenido                                                                                       |
| ----------------------- | ----------------------------------------------------------------------------------------------- |
| Encabezado              | Comité de Agua Potable, Aldea Panyebar, San Juan La Laguna, Sololá, y el número de comprobante. |
| Titular                 | Nombre del titular y su tipo (persona o suministro), con la etiqueta de estado.                 |
| Datos del pago          | Fecha, Registrado por, Concepto y NIS (o «No aplica»).                                          |
| Obligaciones canceladas | Concepto, Período y Monto de cada obligación.                                                   |
| Cierre                  | Estado y Total pagado.                                                                          |

| Sección | Contenido                                                                                    |
| ------- | -------------------------------------------------------------------------------------------- |
| Pie     | «Comprobante generado por el sistema administrativo del Comité de Agua Potable de Panyebar.» |

_Fuente: elaboración propia._

## Consultar un comprobante

**Ruta: Menú principal → Pagos → Ver detalle**

1. Seleccione Pagos en el menú lateral.
2. Localice el pago por su número de comprobante o por el titular con Buscar pago....
3. Seleccione Ver detalle.
4. Desplácese hacia abajo hasta el comprobante (Figura 34).

## Imprimir un comprobante

**Ruta: Menú principal → Pagos → Ver detalle → Imprimir comprobante**

1. Abra el detalle del pago.
2. Seleccione Imprimir comprobante, al final del comprobante.
3. En la ventana de impresión del navegador, elija la impresora o la opción de guardar como PDF.
4. Confirme la impresión.

**Figura 35. Comprobante de pago impreso desde el navegador**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> Se obtiene un comprobante impreso o en formato PDF con la misma información que se muestra en pantalla (Figura 35).

> **RECOMENDACIÓN**
> Entregue el comprobante a la persona que realizó el pago y conserve una copia cuando el Comité lo requiera.

# XVIII. FINANZAS

El módulo Finanzas presenta los ingresos, los egresos y el balance administrativo del Comité de Agua Potable. Los ingresos se originan automáticamente en los pagos registrados; los egresos se registran en este módulo.

**Cuadro 15. Diferencias entre pago, ingreso y egreso**

| Concepto | Qué representa                                                         | Cómo se registra                       |
| -------- | ---------------------------------------------------------------------- | -------------------------------------- |
| Pago     | Cancelación de obligaciones completas de un titular.                   | En Pagos, con Registrar pago.          |
| Ingreso  | Movimiento financiero de entrada derivado de un pago registrado.       | Automáticamente, al registrar el pago. |
| Egreso   | Gasto administrativo del Comité, por ejemplo una compra de materiales. | En Finanzas, con Nuevo egreso.         |
| Balance  | Ingresos menos egresos.                                                | Cálculo automático del sistema.        |

_Fuente: elaboración propia._

## Consultar la información financiera

**Ruta: Menú principal → Finanzas**

**Figura 36. Pantalla principal del módulo Finanzas**

_Fuente: elaboración propia._

La pantalla (Figura 36) presenta tres indicadores: Ingresos (pagos registrados), Egresos (gastos vigentes) y Balance (ingresos menos egresos). La sección Movimientos financieros lista cada movimiento con Fecha, Tipo (Ingreso o Egreso), Concepto con su referencia, Estado y Monto.

> **INFORMACIÓN**
> «Los ingresos provienen exclusivamente de pagos registrados; los egresos anulados no participan.»

## Filtrar movimientos

**Ruta: Menú principal → Finanzas → Filtros financieros**

1. En Filtros financieros, indique la Fecha desde y la Fecha hasta.
2. Si lo desea, seleccione el Tipo de movimiento; la opción «Todos» incluye ingresos y egresos.
3. Seleccione Aplicar filtros.
4. Para quitar los filtros, seleccione Limpiar.

> **RESULTADO ESPERADO**
> Los indicadores y los movimientos corresponden al rango de fechas consultado.

## Registrar un egreso

**Ruta: Menú principal → Finanzas → Nuevo egreso**

1. Seleccione Finanzas en el menú lateral.
2. Seleccione Nuevo egreso. El sistema abre el panel Registrar egreso.
3. En Concepto, seleccione el concepto del gasto, por ejemplo «Compra de materiales».
4. Ingrese el Monto (Q).
5. Indique la Fecha del gasto.
6. Seleccione Registrar egreso. Para salir sin registrar, seleccione Volver o Cerrar.

**Figura 37. Panel para registrar un egreso**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Egreso registrado correctamente.». El egreso aparece en Movimientos financieros y en Egresos registrados (Figura 38), y el balance se actualiza: en el ejemplo, Q45.00 de ingresos menos Q 15.00 de egresos dan un balance de Q 30.00.

**Figura 38. Egresos registrados**

_Fuente: elaboración propia._

> **INFORMACIÓN**
> El panel indica: «Registra un gasto administrativo. La fecha se conserva como fecha civil.», es decir, como la fecha de calendario del gasto.

## Corregir o anular un egreso

En Egresos registrados, cada egreso presenta las acciones Editar y Anular. «Los registros anulados permanecen visibles como parte del historial administrativo», pero no participan en los totales.

> **PRECAUCIÓN**
> Anule un egreso solo para corregir un registro equivocado. Verifique la fecha, el concepto y el monto antes de confirmar.

# XIX. DASHBOARD

El Dashboard presenta los principales totales administrativos de un mes específico. Ofrece una visión rápida del período; para consultas detalladas, utilice los módulos Finanzas y Reportes.

## Seleccionar el período

**Ruta: Menú principal → Dashboard**

1. Seleccione Dashboard en el menú lateral.
2. En Período administrativo, escriba el Año y seleccione el Mes.
3. Seleccione Consultar. Para volver al mes en curso, seleccione Mes actual.

**Figura 39. Período administrativo y totales del dashboard**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra el mes consultado, por ejemplo «Octubre 2026», y los Totales del período (Figura 39).

> **INFORMACIÓN**
> «Los valores mostrados corresponden exclusivamente al mes seleccionado.»

## Indicadores del período

**Cuadro 16. Indicadores del dashboard**

| Indicador               | Descripción en pantalla                   | Interpretación                                                       |
| ----------------------- | ----------------------------------------- | -------------------------------------------------------------------- |
| Ingresos                | Pagos registrados durante el mes.         | Dinero recibido mediante pagos en el mes.                            |
| Egresos                 | Gastos vigentes durante el mes.           | Gastos registrados y no anulados del mes.                            |
| Balance                 | Ingresos menos egresos.                   | Resultado del mes; un valor negativo indica más gastos que ingresos. |
| Pagos                   | Pagos vigentes registrados.               | Cantidad de pagos no anulados del mes.                               |
| Obligaciones pendientes | Generadas en el mes y aún pendientes.     | Obligaciones del mes que todavía no se pagan.                        |
| Monto pendiente         | Monto de obligaciones pendientes del mes. | Suma de esas obligaciones pendientes.                                |

_Fuente: elaboración propia._

## Recaudación por sector

**Figura 40. Recaudación por sector y alcance del resumen**

_Fuente: elaboración propia._

La sección Recaudación por sector (Figura 40) muestra los pagos aplicados a obligaciones de suministros durante el período, agrupados por el sector del suministro: la Recaudación atribuible a suministros, la cantidad de Sectores con recaudación y una gráfica de barras por sector.

> **INFORMACIÓN**
> La recaudación por sector excluye los pagos anulados y las obligaciones personales sin suministro; por eso su total puede ser menor que los ingresos del mes. En el ejemplo, los ingresos suman Q 75.00, pero solo Q 30.00 corresponden a obligaciones de suministros.

## Alcance del resumen

El recuadro Alcance del resumen aclara que las obligaciones del dashboard son únicamente las generadas durante el período seleccionado que continúan pendientes. Para consultar todas las obligaciones pendientes, sin importar el mes en que se generaron, utilice el reporte de obligaciones pendientes (capítulo XX).

# XX. REPORTES

El módulo Reportes presenta información estructurada de pagos, obligaciones pendientes y participación en jornadas. Los reportes se consultan en pantalla a partir de los registros del sistema y no modifican la información de origen.

**Cuadro 17. Tipos de reporte disponibles**

| Reporte                   | Contenido                                                                        | Columnas                                                 | Filtros          |
| ------------------------- | -------------------------------------------------------------------------------- | -------------------------------------------------------- | ---------------- |
| Pagos                     | Historial de pagos registrados y anulados dentro del rango consultado.           | Fecha, Concepto, Estado, Monto.                          | Rango de fechas. |
| Obligaciones pendientes   | Obligaciones que actualmente permanecen pendientes de cancelación.               | Titular, Concepto, Origen, Generación, Monto, Situación. | —                |
| Participación en jornadas | Resultados registrados para las personas participantes en jornadas comunitarias. | Jornada, Fecha, Persona, Resultado, Estado.              | Rango de fechas. |

_Fuente: elaboración propia._

## Generar un reporte

**Ruta: Menú principal → Reportes**

1. Seleccione Reportes en el menú lateral.
2. En Tipo de reporte, seleccione Pagos, Obligaciones pendientes o Participación en jornadas.
3. Cuando el reporte presente Filtros, indique la Fecha desde y la Fecha hasta.
4. Seleccione Aplicar filtros. Para quitar el rango, seleccione Limpiar.
5. Revise los resultados y la cantidad de registros indicada en el reporte.

**Figura 41. Reporte de pagos**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema presenta el reporte seleccionado con los registros que cumplen los criterios (Figura 41).

## Reporte de obligaciones pendientes

**Figura 42. Reporte de obligaciones pendientes**

_Fuente: elaboración propia._

Este reporte (Figura 42) reúne las obligaciones que permanecen pendientes, con su titular, concepto, origen (por ejemplo, «Cuota ordinaria»), fecha de generación, monto y situación. Es la consulta adecuada para conocer los saldos pendientes de períodos anteriores.

## Reporte de participación en jornadas

**Figura 43. Reporte de participación en jornadas**

_Fuente: elaboración propia._

Este reporte (Figura 43) muestra, para cada jornada, la fecha, la persona, el resultado registrado (participación o ausencia) y el estado de la jornada.

> **RECOMENDACIÓN**
> Utilice el rango de fechas para preparar la información de una reunión del Comité, por ejemplo, los pagos o la participación de un mes.

# XXI. ABASTECIMIENTO

El módulo Abastecimiento organiza los días y horarios de distribución de agua potable por sector mediante un calendario administrativo.

> **INFORMACIÓN**
> La programación de abastecimiento es administrativa. El sistema no controla válvulas, bombas ni infraestructura hidráulica: registra y comunica la planificación del servicio.

## Vista Mes

**Ruta: Menú principal → Abastecimiento → Mes**

**Figura 44. Calendario de abastecimiento en vista Mes**

_Fuente: elaboración propia._

La vista Mes (Figura 44) contiene los siguientes elementos:

1. Los indicadores Programados (pendientes de realizar), Completados (realizados este mes) y Cancelados (cancelados este mes).
2. Los controles de navegación ‹ (mes anterior), Hoy y › (mes siguiente), junto al nombre del mes.
3. La lista de sectores, con la opción «Todos los sectores».
4. El selector de vista Mes / Agenda.
5. El calendario, en el que cada programación se identifica por su hora y su sector.
6. La leyenda de colores: Programado (azul), Completado (verde) y Cancelado (gris).

## Navegar entre períodos y filtrar por sector

**Ruta: Menú principal → Abastecimiento**

1. Seleccione ‹ o › para cambiar de mes.
2. Seleccione Hoy para regresar al mes actual.
3. En la lista de sectores, seleccione un sector para ver solo su programación, o «Todos los sectores» para ver la programación completa.

## Agregar una programación

**Ruta: Menú principal → Abastecimiento → (día del calendario) → Nueva programación**

1. En la vista Mes, seleccione el día en el calendario («Selecciona un día para crear una programación»). En la vista Agenda, seleccione + Agregar junto a la fecha.
2. En la ventana Nueva programación, seleccione el Sector.
3. Verifique la Fecha e indique el intervalo horario en Desde y Hasta.
4. Si lo desea, escriba una Observación.
5. Si la programación se repite, elija la frecuencia en Repetir, por ejemplo «Cada semana»; indique en Finaliza el criterio de finalización, por ejemplo «Después de varias repeticiones», y escriba el Número de programaciones.
6. Seleccione Crear programaciones. Para salir sin guardar, seleccione Cancelar.

**Figura 45. Ventana para crear una programación de abastecimiento**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema confirma la operación, por ejemplo «4 programaciones creadas correctamente.», y el calendario muestra las programaciones en estado Programado (Figura 44).

> **INFORMACIÓN**
> «Se crearán programaciones independientes. Editar una posteriormente no modificará las demás.»

## Consultar el detalle de una programación

**Ruta: Menú principal → Abastecimiento → (programación)**

1. Seleccione la programación en el calendario.
2. Revise la ventana de detalle: sector, fecha, horario, Estado y Observación (Figura 46).

**Figura 46. Detalle de una programación de abastecimiento**

_Fuente: elaboración propia._

La ventana de detalle presenta las acciones Editar, Completar y Cancelar programación.

## Editar una programación

**Ruta: Menú principal → Abastecimiento → (programación) → Editar**

1. Abra el detalle de la programación.
2. Seleccione Editar.
3. Modifique la información necesaria y confirme los cambios.

> **INFORMACIÓN**
> El sistema rechaza la modificación cuando la programación ya finalizó o cuando el nuevo horario se solapa con otra programación activa del mismo sector. En ese caso muestra un mensaje de error en color rojo (Figura 76).

## Marcar una programación como completada

**Ruta: Menú principal → Abastecimiento → (programación) → Completar**

1. Abra el detalle de la programación.
2. Seleccione Completar.
3. En la ventana Marcar como completada, verifique el sector, la fecha y el horario.
4. Seleccione Marcar como completada. Para salir sin cambios, seleccione Volver.

**Figura 47. Confirmación para marcar una programación como completada**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Programación marcada como completada.»; la programación cambia al estado Completado y se presenta en color verde.

## Cancelar una programación

**Ruta: Menú principal → Abastecimiento → (programación) → Cancelar programación**

1. Abra el detalle de la programación.
2. Seleccione Cancelar programación.
3. En Observación de cancelación, seleccione el motivo: Reprogramación del abastecimiento, Mantenimiento de la red, Falta de disponibilidad de agua, Emergencia operativa, Condiciones climáticas u Otro.
4. Confirme la cancelación.

**Figura 48. Motivos de cancelación de una programación**

_Fuente: elaboración propia._

> **PRECAUCIÓN**
> «La programación quedará registrada históricamente como cancelada.» Verifique la fecha y el sector antes de confirmar.

> **RESULTADO ESPERADO**
> El sistema muestra «Programación cancelada.»; la programación cambia al estado Cancelado y conserva el motivo seleccionado.

## Vista Agenda

**Ruta: Menú principal → Abastecimiento → Agenda**

**Figura 49. Programación de abastecimiento en vista Agenda**

_Fuente: elaboración propia._

La vista Agenda (Figura 49) presenta las programaciones del mes en forma de lista, agrupadas por fecha. Cada elemento muestra el sector, el horario, la observación o el motivo de cancelación y la etiqueta de estado. El enlace + Agregar de cada fecha permite crear una programación para ese día.

**Cuadro 18. Estados de una programación de abastecimiento**

| Estado     | Color | Significado                                            |
| ---------- | ----- | ------------------------------------------------------ |
| Programado | Azul  | Programación pendiente de realizar.                    |
| Completado | Verde | Programación registrada como realizada.                |
| Cancelado  | Gris  | Programación cancelada; conserva el motivo registrado. |

_Fuente: elaboración propia._

# XXII. ADMINISTRACIÓN DEL COMITÉ

El módulo Administración reúne las funciones de control institucional y seguridad. Se organiza en cuatro pestañas: Comité, Usuarios, Roles y permisos y Auditoría (Figura 50). Este capítulo describe la pestaña Comité; los capítulos XXIII a XXVI explican las demás.

**Ruta: Menú principal → Administración**

**Figura 50. Módulo Administración y sus pestañas**

_Fuente: elaboración propia._

> **SEGURIDAD**
> El acceso a cada pestaña y a sus operaciones depende de los permisos de la cuenta. Las funciones de este módulo deben asignarse únicamente a las personas responsables de la administración del sistema.

## Períodos e integrantes del Comité

La pestaña Comité administra los períodos de la administración comunitaria y los integrantes que ocupan cada cargo. Los cargos disponibles son Presidente, Secretario, Tesorero, Vocal I y Vocal II. Cuando se produce un cambio de directiva, el sistema conserva las administraciones anteriores como historial.

> **INFORMACIÓN**
> Los cargos del Comité son independientes de los roles del sistema. Ocupar un cargo no concede permisos: los permisos se asignan mediante roles en la pestaña Roles y permisos (capítulo XXIV).

## Crear un período administrativo

**Ruta: Administración → Comité → Crear administración**

1. Seleccione Administración y la pestaña Comité.
2. Cuando no existe una administración vigente, el sistema indica «Registra el primer período administrativo para comenzar a organizar los cargos del comité.» Seleccione Crear administración.
3. En la ventana Nueva administración del comité, escriba el Nombre del período, por ejemplo
   «Comité 2026-2027».

4. Indique la Fecha de inicio.
5. Seleccione Crear administración. Para salir sin crear, seleccione Volver.

**Figura 51. Ventana para crear un período administrativo**

_Fuente: elaboración propia._

> **INFORMACIÓN**
> «Este período quedará como la administración vigente hasta que sea finalizado.»

**Figura 52. Administración vigente con los cargos del Comité**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> La administración aparece como Administración vigente, con la etiqueta Vigente, la fecha de inicio y la indicación «Actual». Los cinco cargos se muestran como «Sin asignar» (Figura 52).

## Asignar una persona a un cargo

**Ruta: Administración → Comité → Asignar persona**

1. En Integrantes, localice la tarjeta del cargo con la indicación «Sin asignar».
2. Seleccione Asignar persona.
3. En la ventana Asignar integrante, escriba el nombre o el apellido en Buscar persona para filtrar la lista.
4. En Persona, seleccione a la persona que ocupará el cargo.
5. Seleccione Asignar persona. Para salir sin cambios, seleccione Volver.

**Figura 53. Ventana para asignar un integrante del Comité**

_Fuente: elaboración propia._

**Figura 54. Integrantes asignados a la administración vigente**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «La integración del comité fue actualizada.» y la tarjeta del cargo presenta el nombre de la persona, la indicación «Integrante asignado» y la opción Cambiar persona (Figura 54).

## Cambiar la persona de un cargo

**Ruta: Administración → Comité → Cambiar persona**

1. En la tarjeta del cargo, seleccione Cambiar persona.
2. En la ventana Cambiar integrante, revise la persona actual («Actualmente: …»).
3. Seleccione a la nueva persona en Persona.
4. Confirme el cambio con el botón de la ventana.

**Figura 55. Ventana para cambiar el integrante de un cargo**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> La tarjeta del cargo muestra a la nueva persona asignada.

## Personas disponibles para un cargo

La lista Persona muestra únicamente personas activas que no ocupan otro cargo en la administración vigente; por ejemplo, quien ya es Secretario no aparece en la lista del cargo de Tesorero. Cuando ninguna persona cumple esas condiciones, el sistema indica «No hay personas activas disponibles con ese criterio.» (Figura 56).

**Figura 56. Cargo sin personas disponibles para asignar**

_Fuente: elaboración propia._

> **RECOMENDACIÓN**
> Si una persona no aparece en la lista, verifique en Personas que su estado sea Activo y revise si ocupa otro cargo en la administración vigente.

## Finalizar un período

**Ruta: Administración → Comité → Finalizar período**

### Antes de comenzar

Confirme con el Comité la fecha del cambio de directiva y verifique que los integrantes registrados sean correctos: después de finalizar el período no podrán modificarse.

### Pasos

1. En la tarjeta Administración vigente, seleccione Finalizar período.
2. En la ventana Finalizar período administrativo, lea la advertencia.
3. Indique la Fecha de finalización.
4. Seleccione Finalizar período. Para salir sin finalizar, seleccione Volver.

**Figura 57. Ventana para finalizar un período administrativo**

_Fuente: elaboración propia._

> **PRECAUCIÓN — FINALIZACIÓN DEL PERÍODO**
> Al finalizar una administración, esta pasa al historial y sus integrantes quedan conservados. El período ya no debe modificarse como administración vigente: «Después de finalizar este período ya no podrás modificar sus integrantes.»

> **RESULTADO ESPERADO**
> La administración pasa a Administraciones anteriores con sus integrantes, y el Comité puede registrar el nuevo período administrativo.

## Administraciones anteriores

**Figura 58. Sección de administraciones anteriores**

_Fuente: elaboración propia._

La sección Administraciones anteriores permite consultar la composición histórica del Comité sin alterar sus registros (Figura 58). Mientras no exista un período finalizado, muestra «Todavía no existen períodos administrativos finalizados.»

# XXIII. USUARIOS ADMINISTRATIVOS

La pestaña Usuarios administra las cuentas con acceso administrativo al sistema: controla quién puede iniciar sesión y qué roles tiene asignados.

## Consultar usuarios

**Ruta: Administración → Usuarios**

**Figura 59. Cuentas administrativas con sus roles**

_Fuente: elaboración propia._

La pantalla (Figura 59) muestra los indicadores Usuarios, Activos e Inactivos, y el panel Cuentas administrativas. Cada cuenta presenta su nombre de usuario, la indicación «Usuario administrativo», su estado, sus roles y las acciones Administrar y Restablecer contraseña.

1. Para localizar una cuenta, escriba un nombre de usuario o un rol en Buscar.
2. Para filtrar por condición, seleccione un valor en Estado o conserve «Todos».

## Crear un usuario

**Ruta: Administración → Usuarios → Nuevo usuario**

1. Seleccione Nuevo usuario.
2. En la ventana Nuevo usuario administrativo, ingrese el Nombre de usuario, por ejemplo
   «operador. admin».

3. Ingrese la Contraseña.
4. Escriba nuevamente la contraseña en Confirmar contraseña.
5. Seleccione Crear usuario. Para salir sin crear, seleccione Volver.

**Figura 60. Ventana para crear un usuario administrativo**

_Fuente: elaboración propia._

> **INFORMACIÓN**
> «La cuenta se creará activa. Después podrás asignarle uno o más roles.» Mientras no tenga roles, la cuenta no tiene permisos sobre los módulos del sistema.

**Figura 61. Usuario administrativo creado sin roles asignados**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «El usuario administrativo fue creado.» y la cuenta aparece con el estado Activo y la indicación «Sin roles asignados» (Figura 61).

> **SEGURIDAD**
> Utilice una cuenta individual para cada persona. No cree cuentas compartidas: la auditoría registra las operaciones a nombre de la cuenta que las realiza.

## Asignar roles a un usuario

**Ruta: Administración → Usuarios → Administrar**

1. Localice la cuenta y seleccione Administrar.
2. En la ventana de la cuenta, revise el Estado de la cuenta.
3. En Roles, marque todos los roles que correspondan a las responsabilidades de la persona; el contador indica cuántos están seleccionados.
4. Seleccione Guardar roles.

**Figura 62. Administración del estado y los roles de una cuenta**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Los roles de operador.demo fueron actualizados.» (con el nombre de la cuenta) y el listado presenta el rol junto a la cuenta (Figura 59).

> **INFORMACIÓN**
> Un usuario puede tener uno o más roles; sus permisos resultan de los roles asignados (capítulo XXIV).

## Desactivar un usuario

**Ruta: Administración → Usuarios → Administrar → Desactivar usuario**

1. Seleccione Administrar en la cuenta.
2. En la sección Acceso a la cuenta, seleccione Desactivar usuario.
3. Confirme la operación si el sistema lo solicita.

**Figura 63. Cuenta desactivada**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «La cuenta operador.demo fue desactivada.»; la cuenta presenta el estado Inactivo y el indicador Inactivos aumenta (Figura 63).

> **INFORMACIÓN**
> «El backend protege automáticamente las cuentas y roles necesarios para conservar un administrador funcional.» Es decir, el sistema impide los cambios que lo dejarían sin una cuenta capaz de administrarlo.

> **SEGURIDAD**
> Desactive las cuentas de las personas que dejan de colaborar con el Comité. Una cuenta inactiva no puede iniciar sesión.

## Activar un usuario

**Ruta: Administración → Usuarios → Administrar → Activar usuario**

1. Seleccione Administrar en la cuenta inactiva.
2. Verifique el estado Inactivo («El usuario se encuentra inactivo.»).
3. Seleccione Activar usuario.

**Figura 64. Cuenta inactiva con la opción Activar usuario**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «La cuenta operador.demo fue activada.» y la cuenta vuelve al estado Activo.

## Restablecer la contraseña de un usuario

**Ruta: Administración → Usuarios → Restablecer contraseña**

1. Localice la cuenta y seleccione Restablecer contraseña.
2. En la ventana Restablecer contraseña, ingrese la Nueva contraseña.
3. Escriba la misma contraseña en Confirmar nueva contraseña.
4. Seleccione Restablecer contraseña. Para salir sin cambios, seleccione Volver.

**Figura 65. Ventana para restablecer la contraseña de un usuario**

_Fuente: elaboración propia._

> **PRECAUCIÓN**
> «La contraseña anterior dejará de ser válida inmediatamente.» A partir de ese momento, la persona debe ingresar con la nueva contraseña.

> **SEGURIDAD**
> Las contraseñas temporales deben comunicarse únicamente a la persona autorizada. El sistema lo recuerda: «Utiliza una contraseña temporal segura y entrégala únicamente a la persona autorizada.»

# XXIV. ROLES Y PERMISOS

El sistema controla el acceso mediante roles y permisos. Un permiso autoriza una operación concreta, por ejemplo consultar pagos o registrar pagos. Un rol agrupa varios permisos que corresponden a una responsabilidad. Los usuarios reciben roles, y sus permisos resultan de los roles asignados (Cuadro 19).

**Cuadro 19. Modelo de acceso del sistema**

| Usuario administrativo Cuenta con la que una persona inicia sesión. Puede tener uno o más roles. | →   | Rol Conjunto de permisos con nombre y descripción, por ejemplo Operador de cobros. | →   | Permisos Autorizaciones que habilitan módulos y operaciones, por ejemplo Ver pagos. |
| ------------------------------------------------------------------------------------------------ | --- | ---------------------------------------------------------------------------------- | --- | ----------------------------------------------------------------------------------- |

_Fuente: elaboración propia._

## Consultar roles

**Ruta: Administración → Roles y permisos**

**Figura 66. Pestaña Roles y permisos con el rol Operador de cobros**

_Fuente: elaboración propia._

La pestaña Roles y permisos (Figura 66) se divide en dos paneles. El panel Roles muestra la cantidad de roles registrados, el botón + para crear un rol, el campo Buscar rol y la lista de roles. Al seleccionar un rol, el panel derecho presenta su nombre, su estado, su descripción, el campo Buscar permiso, la cantidad de permisos asignados y los grupos de permisos.

## Crear un rol

**Ruta: Administración → Roles y permisos → +**

1. En el panel Roles, seleccione +.
2. En la ventana Nuevo rol administrativo, ingrese el Nombre del rol, por ejemplo «Encargado de cobros».
3. Escriba la Descripción de las responsabilidades del rol.
4. Seleccione Crear rol. Para salir sin crear, seleccione Volver.

**Figura 67. Ventana para crear un rol administrativo**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El rol aparece en la lista de roles. Como indica la ventana, «Después de crear el rol podrás seleccionar los permisos que le corresponden.»

## Asignar permisos a un rol

**Ruta: Administración → Roles y permisos → (rol) → Guardar permisos**

1. Seleccione el rol en el panel Roles.
2. Localice los permisos por grupo o escriba un término en Buscar permiso, por ejemplo
   «pagos».

3. Marque las casillas de los permisos que el rol debe tener. Para marcar todos los permisos de un grupo, seleccione Seleccionar grupo; para desmarcarlos, seleccione Quitar grupo.
4. Verifique el contador de permisos asignados.
5. Desplácese al final de la lista y seleccione Guardar permisos.

**Figura 68. Permisos seleccionados para el rol Operador de cobros**

_Fuente: elaboración propia._

**Figura 69. Guardado de los permisos del rol**

_Fuente: elaboración propia._

> **RESULTADO ESPERADO**
> El sistema muestra «Los permisos de Operador de cobros fueron actualizados.», con el nombre del rol modificado.

> **INFORMACIÓN**
> Cada permiso muestra su nombre, una descripción y un código técnico, por ejemplo CUOTAS. GESTIONAR. «Los cambios críticos son validados nuevamente por el servidor para evitar dejar el sistema sin un administrador funcional.»

> **RECOMENDACIÓN**
> Asigne a cada rol únicamente los permisos necesarios para sus responsabilidades. Distinga los permisos de consulta («Ver …») de los permisos de gestión («Gestionar …»).

## Grupos de permisos disponibles

**Cuadro 20. Grupos de permisos del sistema**

| Grupo                      | Permiso                  | Qué permite                                                                 |
| -------------------------- | ------------------------ | --------------------------------------------------------------------------- |
| Abastecimiento             | Gestionar abastecimiento | Crear, editar y cambiar el estado de programaciones de abastecimiento.      |
|                            | Ver abastecimiento       | Consultar la programación administrativa del abastecimiento.                |
| Administración comunitaria | Gestionar administración | Gestionar la administración comunitaria.                                    |
|                            | Ver administración       | Consultar la administración comunitaria.                                    |
| Cuotas                     | Gestionar cuotas         | Crear, modificar y administrar las cuotas.                                  |
|                            | Ver cuotas               | Consultar las cuotas administrativas.                                       |
| Dashboard                  | Ver dashboard            | Consultar el resumen administrativo mensual.                                |
| Finanzas                   | Gestionar finanzas       | Registrar, editar y anular egresos.                                         |
|                            | Ver finanzas             | Consultar ingresos, egresos, movimientos, totales y balance.                |
| Jornadas                   | Gestionar jornadas       | Crear, modificar y administrar jornadas comunitarias y sus participaciones. |
|                            | Ver jornadas             | Consultar las jornadas comunitarias y sus participaciones.                  |
| Obligaciones               | Gestionar obligaciones   | Generar, administrar y anular obligaciones.                                 |
|                            | Ver obligaciones         | Consultar las obligaciones registradas.                                     |
| Pagos                      | Gestionar pagos          | Registrar pagos administrativos.                                            |
|                            | Ver pagos                | Consultar pagos y comprobantes.                                             |
| Personas                   | Gestionar personas       | Crear, actualizar y cambiar el estado de personas.                          |
|                            | Ver personas             | Consultar personas registradas.                                             |
| Reportes                   | Ver reportes             | Consultar reportes administrativos.                                         |

| Grupo              | Permiso                            | Qué permite                                              |
| ------------------ | ---------------------------------- | -------------------------------------------------------- |
| Sectores           | Gestionar sectores                 | Crear, actualizar y cambiar el estado de sectores.       |
|                    | Ver sectores                       | Consultar el catálogo administrativo de sectores.        |
| Seguridad y acceso | Asignar permisos administrativos   | Asignar permisos a roles administrativos.                |
|                    | Gestionar roles administrativos    | Gestionar roles administrativos.                         |
|                    | Gestionar usuarios administrativos | Gestionar usuarios administrativos.                      |
|                    | Ver auditoría                      | Consultar la trazabilidad administrativa.                |
|                    | Ver permisos administrativos       | Consultar permisos administrativos.                      |
|                    | Ver roles administrativos          | Consultar roles administrativos.                         |
|                    | Ver usuarios administrativos       | Consultar usuarios administrativos.                      |
| Suministros        | Gestionar suministros              | Crear, modificar y cambiar el estado de los suministros. |
|                    | Ver suministros                    | Consultar los suministros administrados.                 |

_Fuente: elaboración propia._

## Activar o desactivar un rol

El panel del rol muestra su estado (Activo) y el botón Desactivar rol. Utilice esta opción cuando el rol ya no deba emplearse en la organización de accesos.

> **PRECAUCIÓN**
> Antes de desactivar un rol, revise qué usuarios lo tienen asignado: sus permisos dependen de los roles que poseen.

## Asignar un rol a un usuario

Los roles se asignan desde la pestaña Usuarios, con la opción Administrar de cada cuenta (capítulo XXIII, sección «Asignar roles a un usuario»).

## Ejemplo: rol Operador de cobros

Las figuras utilizan el rol de demostración Operador de cobros, descrito como «Gestiona obligaciones y pagos, y consulta la información necesaria para realizar cobros.». Este rol tiene seis permisos asignados: Gestionar obligaciones, Ver obligaciones, Gestionar pagos, Ver pagos, Ver personas y Ver suministros. El rol Demostrador, asignado a la cuenta demo.admin, se describe como «Rol permanente para demostraciones del sistema».

> **INFORMACIÓN**
> Operador de cobros, Demostrador, operador.demo y demo.admin son datos de demostración. El Comité debe definir sus propios roles según las responsabilidades de sus integrantes.

# XXV. RESTRICCIÓN DE ACCESO SEGÚN PERMISOS

El sistema protege la información mediante dos capas de autorización complementarias. Este capítulo las demuestra con la cuenta de prueba operador.demo, que tiene asignado únicamente el rol Operador de cobros.

## Primera capa: la interfaz muestra solo lo autorizado

Al iniciar sesión, el sistema construye el menú lateral y el panel de inicio según los permisos de la cuenta. La Figura 70 compara el menú de demo.admin, con todos los módulos, y el de operador.demo: las opciones Sectores, Cuotas, Jornadas, Finanzas, Dashboard, Reportes, Abastecimiento y Administración desaparecen porque su rol no incluye permisos para esos módulos.

**Figura 70. Comparación del menú lateral: a) demo.admin; b) operador.demo**

_Fuente: elaboración propia._

El panel de inicio de operador.demo (Figura 3) muestra el saludo «Bienvenido, operador.demo» y solo las tarjetas de los módulos disponibles: Personas, Suministros, Solicitudes, Obligaciones y Pagos.

## Operación dentro de un módulo autorizado

**Figura 71. Módulo Pagos consultado por operador.demo**

_Fuente: elaboración propia._

Dentro de un módulo autorizado, el usuario trabaja con normalidad. En la Figura 71, operador.demo consulta los pagos registrados, porque su rol incluye los permisos de pagos. En el momento de la captura no existían obligaciones pendientes; por eso el sistema indicaba «No existen obligaciones pendientes disponibles para registrar un pago.»

## Segunda capa: validación en el servidor

Ocultar opciones en la pantalla no es la única protección. Cada operación protegida se valida nuevamente en el servidor antes de ejecutarse. Si una cuenta intenta realizar una operación para la cual no tiene permiso —por ejemplo, mediante un enlace directo o una acción que no le corresponde

—, el servidor la rechaza y el sistema muestra el mensaje «No tienes permiso para realizar esta operación.»

> **INFORMACIÓN — INTERPRETACIÓN DEL MENSAJE**
> Este mensaje no indica una falla del sistema: es la protección de seguridad funcionando correctamente. La operación no se realizó y la información permaneció sin cambios.

> **RECOMENDACIÓN**
> Si el mensaje aparece al realizar una tarea que sí corresponde a sus responsabilidades, solicite a un usuario autorizado que revise los roles asignados a su cuenta.

## Resumen de la demostración

**Cuadro 21. Resultado de la demostración de restricción de acceso**

| Situación                   | demo.admin          | operador.demo (Operador de cobros)                               |
| --------------------------- | -------------------- | ----------------------------------------------------------------- |
| Módulos visibles en el menú | Los catorce módulos. | Inicio, Personas, Suministros, Solicitudes, Obligaciones y Pagos. |
| Consulta de pagos           | Permitida.           | Permitida: su rol incluye los permisos de pagos.                  |
| Módulo Administración       | Disponible.          | No aparece en el menú.                                            |
| Operación sin permiso       | No aplica.           | Rechazada por el servidor con el mensaje correspondiente.         |

_Fuente: elaboración propia._

# XXVI. AUDITORÍA

La pestaña Auditoría conserva la trazabilidad de las operaciones administrativas: permite conocer qué operación se realizó, quién la realizó, cuándo y sobre qué registro.

> **INFORMACIÓN**
> Los registros de auditoría son de consulta y sirven para conservar la trazabilidad de las operaciones administrativas. El sistema lo indica: «Los registros de auditoría son únicamente de lectura.»

## Consultar el historial de auditoría

**Ruta: Administración → Auditoría**

**Figura 72. Historial de auditoría**

_Fuente: elaboración propia._

La pantalla (Figura 72) presenta los indicadores Registros mostrados, Usuarios y Entidades, el panel Consultar actividad y el listado de operaciones, cuyas columnas se describen en el Cuadro 22.

**Cuadro 22. Columnas del historial de auditoría**

| Columna  | Contenido                                                                                              |
| -------- | ------------------------------------------------------------------------------------------------------ |
| Fecha    | Fecha y hora de la operación.                                                                          |
| Usuario  | Cuenta administrativa que realizó la operación.                                                        |
| Acción   | Tipo de operación —por ejemplo Creación, Asignación, Registro, Estado o Cancelar— y su código técnico. |
| Registro | Entidad afectada —por ejemplo Pago, Obligación o Usuario administrativo— y número de registro.         |
| Detalle  | Enlace Ver detalle.                                                                                    |

_Fuente: elaboración propia._

> **INFORMACIÓN**
> «Se muestran como máximo 200 registros por consulta.» Utilice los filtros para acotar la búsqueda.

## Filtrar la actividad

**Ruta: Administración → Auditoría → Consultar actividad**

1. En Consultar actividad, indique el rango de fechas en Desde y Hasta.
2. Seleccione el Usuario o conserve «Todos».
3. Si lo desea, escriba la Acción (por ejemplo, PAGO) o la Entidad (por ejemplo, Persona).
4. Seleccione Consultar.

> **RESULTADO ESPERADO**
> El listado muestra únicamente las operaciones que cumplen los criterios.

## Consultar el detalle de un registro

**Ruta: Administración → Auditoría → Ver detalle**

1. Localice la operación en el listado.
2. Seleccione Ver detalle.
3. Revise la información de la ventana Detalle de auditoría (Figura 73).
4. Seleccione Cerrar.

**Figura 73. Detalle de un registro de auditoría**

_Fuente: elaboración propia._

**Cuadro 23. Información del detalle de auditoría**

| Campo          | Descripción                                                                                |
| -------------- | ------------------------------------------------------------------------------------------ |
| Fecha y hora   | Momento exacto de la operación.                                                            |
| Usuario        | Cuenta que realizó la operación.                                                           |
| Acción         | Tipo de operación, por ejemplo Creación.                                                   |
| Entidad        | Tipo y número del registro afectado, por ejemplo Usuario administrativo #24.               |
| Valor anterior | Información previa a la operación; en una creación indica «Sin información registrada».    |
| Valor nuevo    | Información resultante, por ejemplo el nombre de usuario y el estado de una cuenta creada. |
| Código técnico | Identificador interno de la operación, por ejemplo SEGURIDAD. USUARIO. CREAR.              |

_Fuente: elaboración propia._

> **RECOMENDACIÓN**
> Revise la auditoría cuando necesite aclarar quién registró, modificó o anuló una operación, por ejemplo un pago, una obligación o un cambio de roles.

# XXVII. MENSAJES DEL SISTEMA

El sistema informa el resultado de las operaciones mediante mensajes de color. Leerlos permite confirmar que una operación se completó o identificar por qué no pudo realizarse.

## Tipos de mensajes

**Cuadro 24. Tipos de mensajes**

|     | Tipo         | Color    | Significado                                                                                                                           |
| --- | ------------ | -------- | ------------------------------------------------------------------------------------------------------------------------------------- |
|     | Confirmación | Verde    | Operación realizada correctamente. Continúe con el siguiente paso.                                                                    |
|     | Advertencia  | Amarillo | ⚠ Requiere atención del usuario. Lea el mensaje antes de confirmar.                                                                   |
|     | Error        | Rojo     | ✕ La operación no pudo completarse o el usuario no posee autorización. Corrija la información o solicite la revisión de sus permisos. |

_Fuente: elaboración propia._

**Figura 74. Mensaje de confirmación**

_Fuente: elaboración propia._

**Figura 75. Mensaje de advertencia en una ventana de confirmación**

_Fuente: elaboración propia._

**Figura 76. Mensaje de error**

_Fuente: elaboración propia._

Además, algunos avisos informativos aparecen en recuadros blancos, por ejemplo «No existen obligaciones pendientes disponibles para registrar un pago.». En el módulo Administración, los mensajes incluyen el enlace Cerrar para ocultarlos.

## Mensajes frecuentes

**Cuadro 25. Mensajes frecuentes del sistema**

| Mensaje                                                                                                                                | Tipo         | Módulo                   |
| -------------------------------------------------------------------------------------------------------------------------------------- | ------------ | ------------------------ |
| «Suministro creado con NIS PAN-000001.»                                                                                                | Confirmación | Suministros              |
| «Responsable actualizado correctamente.»                                                                                               | Confirmación | Suministros              |
| «Solicitud creada correctamente.»                                                                                                      | Confirmación | Solicitudes              |
| «Solicitud aprobada. Se creó el suministro PAN-000002.»                                                                                | Confirmación | Solicitudes              |
| «Al aprobar se creará un nuevo suministro con NIS y código QR propios.»                                                                | Advertencia  | Solicitudes              |
| «Cuota creada correctamente.»                                                                                                          | Confirmación | Cuotas                   |
| «Obligación generada correctamente. El monto quedó registrado con el valor actual de la cuota.»                                        | Confirmación | Obligaciones             |
| «Se generaron 1 obligaciones correctamente.»                                                                                           | Confirmación | Obligaciones             |
| «Jornada creada correctamente.»                                                                                                        | Confirmación | Jornadas                 |
| «Participación actualizada correctamente.»                                                                                             | Confirmación | Jornadas                 |
| «Jornada cerrada correctamente. Las obligaciones aplicables fueron generadas por el servidor.»                                         | Confirmación | Jornadas                 |
| «Pago registrado correctamente. Comprobante PAG-000001.»                                                                               | Confirmación | Pagos                    |
| «No existen obligaciones pendientes disponibles para registrar un pago.»                                                               | Informativo  | Pagos                    |
| «Egreso registrado correctamente.»                                                                                                     | Confirmación | Finanzas                 |
| «4 programaciones creadas correctamente.»                                                                                              | Confirmación | Abastecimiento           |
| «Programación marcada como completada.»                                                                                                | Confirmación | Abastecimiento           |
| «Programación cancelada.»                                                                                                              | Confirmación | Abastecimiento           |
| «La programación no puede modificarse porque ya finalizó o porque el horario se solapa con otra programación activa del mismo sector.» | Error        | Abastecimiento           |
| «La integración del comité fue actualizada.»                                                                                           | Confirmación | Administración: Comité   |
| «No hay personas activas disponibles con ese criterio.»                                                                                | Advertencia  | Administración: Comité   |
| «Después de finalizar este período ya no podrás modificar sus integrantes.»                                                            | Advertencia  | Administración: Comité   |
| «El usuario administrativo fue creado.»                                                                                                | Confirmación | Administración: Usuarios |
| «Los roles de operador.demo fueron actualizados.»                                                                                     | Confirmación | Administración: Usuarios |
| «La cuenta operador.demo fue desactivada.» / «… fue activada.»                                                                        | Confirmación | Administración: Usuarios |
| «Utiliza una contraseña temporal segura y entrégala únicamente a la persona autorizada.»                                               | Advertencia  | Administración: Usuarios |

| Mensaje                                                   | Tipo         | Módulo                |
| --------------------------------------------------------- | ------------ | --------------------- |
| «Los permisos de Operador de cobros fueron actualizados.» | Confirmación | Administración: Roles |
| «No tienes permiso para realizar esta operación.»         | Error        | Cualquier módulo      |

_Fuente: elaboración propia._

Los números y nombres que aparecen en algunos mensajes, como PAN-000001, PAG-000001 u operador.demo, cambian según el registro o la cuenta involucrados.

# XXVIII. BUENAS PRÁCTICAS

Las siguientes recomendaciones ayudan a mantener la seguridad de las cuentas y la calidad de la información registrada.

## Seguridad de las cuentas

1. Cierre la sesión al finalizar, especialmente en equipos compartidos.
2. No comparta sus credenciales; utilice siempre una cuenta individual.
3. Entregue las contraseñas temporales únicamente a la persona titular de la cuenta.
4. Desactive las cuentas de las personas que dejan de colaborar con el Comité.

## Permisos y roles

1. Asigne únicamente los permisos necesarios para las responsabilidades de cada persona.
2. Distinga los permisos de consulta («Ver …») de los de gestión («Gestionar …»).
3. Revise periódicamente los roles asignados, en especial después de un cambio de directiva.

## Registro de información

1. Busque antes de registrar, para evitar personas o suministros duplicados.
2. Verifique la información antes de guardar y confirme el NIS antes de operar sobre un suministro.
3. Utilice direcciones de referencia claras, conocidas por la comunidad.
4. Registre la asistencia de todos los participantes antes de cerrar una jornada.

## Operaciones sensibles

1. Revise cuidadosamente antes de finalizar un período administrativo: sus integrantes ya no podrán modificarse.
2. Anule pagos, obligaciones o egresos solo para corregir errores comprobados y con autorización del Comité.
3. Revise la auditoría cuando sea necesario aclarar quién realizó una operación.

## Información y privacidad

1. Evite compartir capturas de pantalla que contengan información sensible cuando el sistema opere con datos reales.
2. Imprima comprobantes y reportes solo cuando sea necesario y resguarde las copias.
3. Reproduzca únicamente códigos QR generados desde la instalación definitiva del sistema.

# XXIX. SOLUCIÓN DE PROBLEMAS

El Cuadro 26 reúne situaciones que pueden presentarse durante el uso del sistema, sus causas posibles y la acción recomendada.

**Cuadro 26. Solución de problemas frecuentes**

| Situación                                                 | Posible causa                                                     | Acción recomendada                                                                                                                       |
| --------------------------------------------------------- | ----------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| No puedo iniciar sesión.                                  | Credenciales incorrectas o cuenta inactiva.                       | Verifique el usuario y la contraseña; si persiste, solicite al administrador revisar el estado de la cuenta o restablecer la contraseña. |
| No aparece un módulo en el menú.                          | La cuenta no tiene permisos para ese módulo.                      | Solicite la revisión de sus roles.                                                                                                       |
| Aparece «No tienes permiso para realizar esta operación.» | Operación no autorizada para la cuenta.                           | Solicite al administrador revisar sus permisos. No es una falla del sistema.                                                             |
| No encuentro a una persona para asignarla al Comité.      | La persona ya ocupa otro cargo o está inactiva.                   | Revise los integrantes actuales y el estado de la persona en Personas.                                                                   |
| No puedo registrar un pago; el botón no está disponible.  | No existen obligaciones pendientes compatibles.                   | Revise las obligaciones del titular y, si corresponde, genérelas.                                                                        |
| El total del pago no coincide con el dinero recibido.     | El sistema solo admite el total exacto de obligaciones completas. | Ajuste la selección de obligaciones; no se admiten pagos parciales ni saldos a favor.                                                    |
| Una persona no aparece en una lista de selección.         | Está inactiva o ya fue agregada.                                  | Verifique su estado o la lista de participantes.                                                                                         |
| Una ausencia no generó obligación.                        | La jornada no tiene Monto por ausencia.                           | Es el comportamiento esperado: sin monto, las ausencias no generan cobro.                                                                |
| No puedo modificar una programación de abastecimiento.    | Ya finalizó o el horario se solapa con otra del mismo sector.     | Revise el estado de la programación y elija otro horario.                                                                                |
| No encuentro un suministro por NIS.                       | El NIS está incompleto o mal escrito.                             | Escriba el NIS completo (por ejemplo, PAN-000002) o seleccione Limpiar.                                                                  |
| No veo todas las acciones de una fila.                    | La tabla es más ancha que la pantalla.                            | Utilice la barra de desplazamiento horizontal del listado.                                                                               |
| Un código QR no abre la consulta.                         | Se generó en otra instalación o no hay conexión.                  | Verifique la conexión y utilice códigos generados desde la instalación definitiva.                                                       |
| No aparece información en un listado o reporte.           | No existen registros o los filtros excluyen los resultados.       | Revise los filtros y el rango de fechas; seleccione Limpiar.                                                                             |

_Fuente: elaboración propia._

# XXX. GLOSARIO

Términos utilizados en el sistema y en este manual, en orden alfabético.

| Término                        | Definición                                                                                                                                                                             |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Administración vigente         | Período administrativo del Comité que se encuentra activo; sus integrantes pueden asignarse o cambiarse hasta que se finalice.                                                         |
| Auditoría                      | Registro de consulta que conserva quién realizó cada operación administrativa relevante, cuándo y sobre qué registro.                                                                  |
| Balance                        | Resultado de restar los egresos a los ingresos de un período.                                                                                                                          |
| Comité                         | Comité de Agua Potable de la Aldea Panyebar, organización comunitaria que administra el servicio.                                                                                      |
| Comprobante                    | Documento que el sistema genera al registrar un pago; se identifica con un número como PAG-000001.                                                                                     |
| Cuota                          | Configuración administrativa de un cobro, con monto, periodicidad y vigencia, a partir de la cual se generan obligaciones de suministros.                                              |
| Egreso                         | Gasto administrativo del Comité registrado en Finanzas.                                                                                                                                |
| Ingreso                        | Movimiento financiero de entrada que el sistema registra automáticamente a partir de un pago.                                                                                          |
| Jornada comunitaria            | Actividad de trabajo comunitario para el mantenimiento del servicio, en la que se registra la participación o ausencia de las personas.                                                |
| NIS                            | Número de Identificación de Suministro: identificador único y permanente de un suministro, con el formato PAN-000001.                                                                  |
| Obligación                     | Compromiso económico registrado a nombre de un suministro o de una persona; se paga completo o permanece pendiente.                                                                    |
| Pago                           | Registro de la cancelación de una o varias obligaciones completas de un mismo titular.                                                                                                 |
| Permiso                        | Autorización para utilizar un módulo o realizar una operación, por ejemplo Ver pagos.                                                                                                  |
| Programación de abastecimiento | Registro administrativo de la fecha y el horario de distribución de agua para un sector.                                                                                               |
| QR (código QR)                 | Código bidimensional asociado a un suministro; contiene un identificador técnico que permite localizarlo con la cámara de un dispositivo. No contiene datos personales ni financieros. |
| Responsable                    | Persona asociada con un suministro en un momento determinado.                                                                                                                          |
| Rol                            | Conjunto de permisos que se asigna a los usuarios administrativos según sus responsabilidades.                                                                                         |
| Sector                         | División territorial utilizada para organizar los suministros, las personas y el abastecimiento.                                                                                       |

| Término                     | Definición                                                                                           |
| --------------------------- | ---------------------------------------------------------------------------------------------------- |
| Solicitud de nuevo servicio | Trámite que, al aprobarse, crea un nuevo suministro con su propio NIS y código QR.                   |
| Suministro                  | Servicio de agua potable registrado en el sistema, con NIS, código QR, sector, responsable y estado. |
| Titular                     | Suministro o persona a cuyo nombre se registra una obligación o un pago.                             |
| Usuario administrativo      | Cuenta con la que una persona autorizada inicia sesión en el sistema.                                |
