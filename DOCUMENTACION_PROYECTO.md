# Documentación del Proyecto APIMaterialesESCOM

## Introducción
(Aquí puedes agregar una breve introducción al proyecto)

## Módulos del Proyecto

### 1. Controlador de Usuarios (`ControladorUsuarios.cs`)
   - **Descripción General:**
     El `ControladorUsuarios` es un componente central de la API, actuando como el principal punto de entrada para todas las operaciones relacionadas con la gestión del ciclo de vida de los usuarios y sus procesos de autenticación y autorización. Sus responsabilidades abarcan desde el registro inicial de nuevos usuarios, la validación de sus credenciales para el inicio de sesión, la actualización de su información personal, y su eventual eliminación del sistema. Un aspecto crucial de este controlador es la implementación de un sistema de verificación de cuentas de correo electrónico mediante el envío de códigos numéricos únicos, así como la generación de JSON Web Tokens (JWT) para securizar las sesiones de los usuarios autenticados. Adicionalmente, el controlador integra una lógica específica para usuarios pertenecientes al dominio `@ipn.mx`, interactuando con un sistema externo para la vinculación de estos usuarios con perfiles de `Autor`.

   - **Dependencias Inyectadas:**
     La arquitectura del controlador sigue el principio de Inyección de Dependencias (DI) para desacoplar sus componentes y facilitar la mantenibilidad y testeo. Las siguientes dependencias son inyectadas en su constructor:
       - `InterfazRepositorioUsuarios _usuarioRepository`: Abstracción del repositorio encargado de la persistencia y recuperación de datos de los usuarios. Proporciona métodos para interactuar con la base de datos (e.g., crear, leer, actualizar, eliminar usuarios, verificar existencia por email, autenticar).
       - `IEmailService _emailService`: Servicio responsable del envío de correos electrónicos. Es utilizado para enviar códigos de verificación y confirmación de inicio de sesión.
       - `ILogger<ControladorUsuarios> _logger`: Interfaz estándar de logging de ASP.NET Core, utilizada para registrar eventos significativos, advertencias y errores ocurridos durante la ejecución de las operaciones del controlador. Esto es fundamental para el monitoreo y la depuración.
       - `ICodeService _codeService`: Servicio que encapsula la lógica de negocio para la generación de códigos de verificación (e.g., numéricos de 6 dígitos) y la definición de sus tiempos de expiración, así como los tiempos de expiración para los JWT.
       - `InterfazRepositorioCodigos _codeRepository`: Abstracción del repositorio para la persistencia y gestión de los códigos de verificación generados, asociándolos a los usuarios y sus tiempos de validez.
       - `IConfiguration _configuration`: Interfaz que permite acceder a la configuración de la aplicación, como las claves secretas, el emisor (issuer) y la audiencia (audience) necesarios para la generación y validación de JWT, usualmente definidos en `appsettings.json`.

   - **Procedimiento de Desarrollo Detallado y Endpoints:**

     - **1. `GET /repositorio/usuarios` (`GetUsuarios`)**
       - **Propósito:** Recuperar una lista completa de todos los usuarios registrados en el sistema.
       - **Lógica de Operación:**
         - Invoca el método `_usuarioRepository.GetAllUsuarios()` para obtener la colección de entidades `Usuario`.
         - Verifica si la lista resultante es nula o vacía. En tal caso, devuelve una respuesta `OkObjectResult` (HTTP 200) con un objeto `Respuesta<IEnumerable<Usuario>>` indicando éxito pero con una lista vacía y un mensaje informativo ("No se encontraron usuarios").
         - Si se encuentran usuarios, los incluye en el objeto `Respuesta<IEnumerable<Usuario>>.Success(usuarios)` y devuelve un `OkObjectResult`.
       - **Manejo de Errores:**
         - Utiliza un bloque `try-catch` para capturar cualquier excepción genérica (`Exception`) que pueda ocurrir durante la operación.
         - En caso de error, registra el incidente utilizando `_logger.LogError(ex, "Error al obtener usuarios")`.
         - Devuelve un `StatusCodeResult` (HTTP 500 - Internal Server Error) con un objeto `Respuesta.Failure` que contiene un mensaje genérico y el mensaje de la excepción.

     - **2. `GET /repositorio/usuarios/{id}` (`GetUsuario`)**
       - **Propósito:** Obtener la información detallada de un usuario específico, identificado por su `id` numérico.
       - **Parámetros de Ruta:** `int id` (ID del usuario).
       - **Lógica de Operación:**
         - Llama a `_usuarioRepository.GetUsuarioById(id)` para buscar al usuario.
         - Si el usuario no se encuentra (el método devuelve `null`), retorna un `NotFoundObjectResult` (HTTP 404) con `Respuesta.Failure("Usuario no encontrado")`.
         - Si se encuentra, devuelve un `OkObjectResult` (HTTP 200) con `Respuesta<Usuario>.Success(usuario, "Usuario obtenido exitosamente")`.
       - **Manejo de Errores:** Similar al endpoint `GetUsuarios`, con logging específico del error y el ID del usuario implicado.

     - **3. `POST /repositorio/usuarios/signup` (`SignUp`)**
       - **Propósito:** Registrar un nuevo usuario en el sistema o reenviar un código de verificación si el usuario ya existe pero no ha verificado su correo.
       - **Cuerpo de la Solicitud:** `UsuarioSignUp usuarioDto` (Data Transfer Object con los datos de registro del usuario, como nombre, apellidos, email, boleta).
       - **Lógica de Operación:**
         - **Validación del Modelo:** Comprueba `ModelState.IsValid`. Si no es válido, recoge los errores de validación y retorna un `BadRequestObjectResult` (HTTP 400) con `Respuesta<object>.Failure("Datos inválidos", errors)`.
         - **Verificación de Usuario Existente:** Llama a `_usuarioRepository.GetUsuarioByEmail(usuarioDto.Email)`.
           - **Caso 1: Email Existente y Verificado:** Si `existingUser != null` y `_usuarioRepository.EmailVerificadoAsync(existingUser.Id)` es `true`, retorna `ConflictObjectResult` (HTTP 409) con `Respuesta.Failure("Ya existe un usuario con este email")`.
           - **Caso 2: Email Existente, No Verificado:** Si `existingUser != null` pero no está verificado, se procede a reenviar un código:
             - Elimina códigos previos para ese usuario: `_codeRepository.EliminaCodigoUsuarioAsync(existingUser.Id)`.
             - Genera un nuevo código: `_codeService.GenerarCodigo()` y su expiración: `_codeService.TiempoExpiracion()`.
             - Guarda el nuevo código: `_codeRepository.CrearCodigoAsync(existingUser.Id, recodigo, expiracion)`.
             - Formatea el código para el email (e.g., "XXX-XXX").
             - Prepara y envía el email de verificación usando `_emailService.SendEmailAsync` con la plantilla de `GenerarCorreoVerificacion`.
             - Retorna `OkObjectResult` con el ID del usuario y un mensaje indicando el reenvío del código.
         - **Caso 3: Usuario Nuevo:**
           - Establece el rol por defecto a "1" (usuario general).
           - Crea el usuario: `_usuarioRepository.CreateUsuario(usuarioDto)`, obteniendo el `userId`.
           - **Lógica Específica para IPN:** Si `usuarioDto.Email.Contains("@ipn.mx")`:
             - Asigna el rol "2" (usuario IPN/autor).
             - Instancia `ApiRequest` para comunicarse con un servicio externo de autores.
             - Intenta obtener el autor por email. Si no existe, lo crea.
             - Crea una relación entre el `userId` local y el `autorID` externo.
             - Si alguna de estas operaciones con la API externa falla, puede retornar `BadRequestObjectResult` con mensajes de error específicos.
           - Elimina cualquier código previo para el nuevo `userId` (medida de precaución): `_codeRepository.EliminaCodigoUsuarioAsync(userId)`.
           - Genera, guarda y envía un nuevo código de verificación (similar al Caso 2).
           - Retorna `OkObjectResult` con el `userId`, `autorID` (si aplica) y un mensaje sobre el envío del código.
       - **Manejo de Errores:** Captura excepciones, las registra y devuelve HTTP 500.

     - **4. `POST /repositorio/usuarios/signin` (`SignIn`)**
       - **Propósito:** Autenticar a un usuario existente y, como parte de un proceso de seguridad mejorado, enviar un código de confirmación a su email antes de conceder acceso completo (aunque el JWT se emite en este paso, la intención es que se use `verifyCode` para validar ese código enviado).
       - **Cuerpo de la Solicitud:** `UsuarioSignIn signinDto` (DTO con email). (Nota: La contraseña parece no usarse directamente aquí, la autenticación es por email y luego código. `_usuarioRepository.Authenticate` podría estar simplificado o la contraseña manejada internamente en el repo).
       - **Lógica de Operación:**
         - Llama a `_usuarioRepository.Authenticate(signinDto.Email)` para obtener el usuario. Si no existe, retorna `UnauthorizedObjectResult` (HTTP 401) con `Respuesta.Failure("Email incorrecto")`.
         - Verifica si el email está verificado: `_usuarioRepository.EmailVerificadoAsync(usuario.Id)`. Si no, retorna `UnauthorizedObjectResult` indicando que la cuenta no ha sido verificada.
         - Elimina códigos previos: `_codeRepository.EliminaCodigoUsuarioAsync(usuario.Id)`.
         - Genera un nuevo código de confirmación (`_codeService.GenerarCodigo()`, `_codeService.TiempoExpiracion()`) y lo guarda (`_codeRepository.CrearCodigoAsync`).
         - Envía el email de confirmación (`_emailService.SendEmailAsync` con `GenerarCorreoConfirmar`).
         - **Obtención de `autorID` para Rol IPN:** Si `usuario.Rol == "2"`, consulta la API externa para obtener el `autorID` asociado. Si falla, retorna `BadRequestObjectResult`.
         - **Generación de JWT:**
           - Define la expiración del JWT: `_codeService.TiempoExpiracionJWT()`.
           - Llama a `GenerateJwtToken(usuario, jwtExpiracion)` para crear el token.
           - Convierte la fecha de expiración a timestamp Unix.
         - Retorna `OkObjectResult` con un objeto anónimo que contiene `Id` (del usuario), `autorID` (si aplica), `accessToken` (el JWT), y `expiresAt` (timestamp de expiración del JWT).
       - **Manejo de Errores:** Captura excepciones, las registra y devuelve HTTP 500.

     - **5. `POST /repositorio/usuarios/verifyCode` (`VerificarEmail`)**
       - **Propósito:** Verificar un código enviado al email del usuario, ya sea para activar la cuenta por primera vez o para confirmar una operación sensible (como el inicio de sesión, según el flujo de `SignIn`). Al verificar exitosamente, se emite (o re-emite) un JWT.
       - **Cuerpo de la Solicitud:** `VerificacionCodigo verificacion` (DTO con `Codigo` y `UsuarioId`).
       - **Lógica de Operación:**
         - Validación básica: `verificacion.Codigo` y `verificacion.UsuarioId` no deben ser nulos/vacíos. Si no, `BadRequestObjectResult`.
         - Obtiene el usuario: `_usuarioRepository.GetUsuarioById(verificacion.UsuarioId)`. Si no existe, `NotFoundObjectResult`.
         - Obtiene el código de la BD: `_codeRepository.ObtenerCodigoAsync(verificacion.Codigo)`. Si no existe, `NotFoundObjectResult` ("Código no encontrado o ya utilizado").
         - Valida que el código pertenezca al `UsuarioId` proporcionado. Si no, `UnauthorizedObjectResult`.
         - Valida que el código no haya expirado: `_codeService.ExpiracionCodigo(verificationCode.Expires)`. Si expiró, elimina el código (`_codeRepository.EliminarCodigoAsync`) y retorna `BadRequestObjectResult` ("El código ha expirado...").
         - **Diferenciación de Flujo (Registro vs. Confirmación):**
           - Verifica si el email ya está verificado: `_usuarioRepository.EmailVerificadoAsync(usuario.Id)`.
           - Si `!isVerified` (es un código de registro/activación), marca el email como verificado: `_usuarioRepository.VerificacionEmailAsync(verificationCode.UsuarioId, true)`.
         - Elimina el código usado de la BD: `_codeRepository.EliminarCodigoAsync(verificacion.Codigo)`.
         - **Generación de JWT:** Similar a `SignIn`, genera un JWT con `GenerateJwtToken` y su timestamp de expiración.
         - Retorna `OkObjectResult` con `accessToken` y `expiresAt`.
       - **Manejo de Errores:** Captura excepciones, las registra y devuelve HTTP 500.

     - **6. `PUT /repositorio/usuarios/{id}` (`UpdateUsuario`)**
       - **Propósito:** Actualizar la información de un usuario existente.
       - **Parámetros de Ruta:** `int id` (ID del usuario a actualizar).
       - **Cuerpo de la Solicitud:** `UsuarioUpdate usuarioDto` (DTO con los campos actualizables del usuario).
       - **Lógica de Operación:**
         - Validación del modelo `usuarioDto`. Si no es válido, `BadRequestObjectResult`.
         - Verifica que el usuario exista: `_usuarioRepository.GetUsuarioById(id)`. Si no, `NotFoundObjectResult`.
         - **Conflicto de Email:** Si se intenta cambiar el email (`!string.IsNullOrEmpty(usuarioDto.Email) && usuarioDto.Email != usuario.Email`), verifica si el nuevo email ya está en uso por otro usuario (`_usuarioRepository.GetUsuarioByEmail(usuarioDto.Email)`). Si es así, `ConflictObjectResult`.
         - Realiza la actualización: `_usuarioRepository.UpdateUsuario(id, usuarioDto)`.
         - Si la actualización es exitosa (devuelve `true`), retorna `OkObjectResult` con `Respuesta.Success("Usuario actualizado exitosamente")`.
         - Si falla, `BadRequestObjectResult` con `Respuesta.Failure("Error al actualizar el usuario")`.
       - **Manejo de Errores:** Captura excepciones, las registra y devuelve HTTP 500.

     - **7. `DELETE /repositorio/usuarios/{id}` (`DeleteUsuario`)**
       - **Propósito:** Eliminar un usuario del sistema.
       - **Parámetros de Ruta:** `int id` (ID del usuario a eliminar).
       - **Lógica de Operación:**
         - Verifica que el usuario exista: `_usuarioRepository.GetUsuarioById(id)`. Si no, `NotFoundObjectResult`.
         - Realiza la eliminación: `_usuarioRepository.DeleteUsuario(id)`.
         - Si la eliminación es exitosa (devuelve `true`), retorna `OkObjectResult` con `Respuesta.Success("Usuario eliminado exitosamente")`.
         - Si falla, `BadRequestObjectResult` con `Respuesta.Failure("Error al eliminar el usuario")`.
       - **Manejo de Errores:** Captura excepciones, las registra y devuelve HTTP 500.

     - **8. Métodos Privados Auxiliares:**
       - **`GenerateJwtToken(Usuario usuario, DateTime expiracion)`:**
         - **Función:** Construye y firma un JSON Web Token.
         - **Clave de Seguridad:** Obtiene la clave secreta de `_configuration["Jwt:Key"]`, la convierte a bytes (`Encoding.UTF8.GetBytes`) y crea una `SymmetricSecurityKey`.
         - **Credenciales de Firma:** Crea `SigningCredentials` usando la clave y el algoritmo `SecurityAlgorithms.HmacSha256`.
         - **Claims (Afirmaciones):** Define un array de `Claim` que se incluirán en el payload del JWT:
           - `JwtRegisteredClaimNames.Sub`: Subject, el ID del usuario (`usuario.Id.ToString()`).
           - `JwtRegisteredClaimNames.Email`: Email del usuario.
           - `Claim("nombre", usuario.Nombre)`: Nombre del usuario.
           - `Claim("apellido", usuario.ApellidoP)`: Apellido paterno.
           - `Claim("boleta", usuario.Boleta)`: Boleta del usuario.
           - `Claim("rol", usuario.Rol)`: Rol del usuario.
           - `JwtRegisteredClaimNames.Jti`: JWT ID, un identificador único para el token (`Guid.NewGuid().ToString()`).
         - **Creación del Token:** Instancia `JwtSecurityToken` con:
           - `issuer`: Emisor del token, desde `_configuration["Jwt:Issuer"]`.
           - `audience`: Audiencia del token, desde `_configuration["Jwt:Audience"]`.
           - `claims`: El array de claims definido.
           - `expires`: La fecha y hora de expiración del token.
           - `signingCredentials`: Las credenciales de firma.
         - **Serialización:** Utiliza `new JwtSecurityTokenHandler().WriteToken(token)` para serializar el objeto token a su representación string.
         - **Retorno:** El string del JWT.

       - **`GenerarCorreoConfirmar(string nombre, string apellido, string email, string boleta, string codigo)`:**
         - **Función:** Construye el cuerpo HTML del correo electrónico para la confirmación de inicio de sesión.
         - **Contenido:** Formatea el `codigo` de 6 dígitos a "XXX-XXX" para mejor legibilidad.
         - Genera una cadena de texto HTML con estilos CSS inline para una presentación visual adecuada en clientes de correo.
         - Incluye placeholders para `nombre`, `apellido`, `email`, `boleta` y el `codigoFormateado`.
         - Informa al usuario sobre el propósito del código (confirmar inicio de sesión) y su tiempo de expiración (implícito, usualmente 1 hora).
         - Contiene un pie de página estándar con un aviso de mensaje automático y copyright.
         - **Retorno:** String con el contenido HTML del email.

       - **`GenerarCorreoVerificacion(string nombre, string apellido, string email, string boleta, string codigo)`:**
         - **Función:** Similar a `GenerarCorreoConfirmar`, pero construye el cuerpo HTML para el correo de verificación de cuenta nueva o re-verificación.
         - **Contenido:** La estructura y formato son análogos, pero el mensaje se enfoca en la necesidad de verificar la cuenta para completar el registro o reactivarla.
         - **Retorno:** String con el contenido HTML del email.

### 2. Gestión de Códigos de Verificación
   - **Descripción:** Este módulo se encarga de toda la lógica relacionada con los códigos de verificación utilizados para la confirmación de cuentas de correo electrónico y la autenticación de dos factores durante el inicio de sesión.
   - **Componentes:**
     - `RepositorioCodigos.cs`: Implementación del repositorio para interactuar con la tabla `CodigoVerificacion` en la base de datos SQLite. Permite crear, obtener y eliminar códigos.
     - `InterfazRepositorioCodigos.cs`: Define la interfaz para el repositorio de códigos.
     - `CodeService.cs`: Servicio para la lógica de negocio de los códigos. Genera códigos numéricos de 6 dígitos, define los tiempos de expiración para los códigos de verificación (1 hora) y para los tokens JWT (30 días), y comprueba si un código ha expirado.
     - `ICodeService.cs`: Define la interfaz para el servicio de códigos.
   - **Procedimiento de Desarrollo:**
     - **1. `ICodeService` (Interfaz del Servicio de Códigos):**
       - Se define la interfaz `ICodeService` con los siguientes métodos:
         - `string GenerarCodigo()`: Para generar un nuevo código.
         - `DateTime TiempoExpiracion()`: Para obtener la fecha y hora de expiración de un código de verificación.
         - `DateTime TiempoExpiracionJWT()`: Para obtener la fecha y hora de expiración de un token JWT.
         - `bool ExpiracionCodigo(DateTime expirationTime)`: Para verificar si un código ha expirado.
     - **2. `CodeService` (Implementación del Servicio de Códigos):**
       - Se implementa `ICodeService`.
       - `GenerarCodigo()`: Utiliza `_random.Next(100000, 999999).ToString()` para crear un código numérico aleatorio de 6 dígitos.
       - `TiempoExpiracion()`: Devuelve `DateTime.UtcNow.AddHours(1)`, estableciendo la validez de un código de verificación a 1 hora desde su creación.
       - `TiempoExpiracionJWT()`: Devuelve `DateTime.UtcNow.AddDays(30)`, estableciendo la validez de un JWT a 30 días.
       - `ExpiracionCodigo(DateTime expirationTime)`: Compara `DateTime.UtcNow` con `expirationTime` para determinar si el código ha expirado.
     - **3. `InterfazRepositorioCodigos` (Interfaz del Repositorio de Códigos):**
       - Se define la interfaz `InterfazRepositorioCodigos` con métodos asíncronos para las operaciones CRUD en la base de datos relacionadas con los códigos:
         - `Task<CodigoVerificacion> CrearCodigoAsync(int userId, string codigo, DateTime expirationTime)`: Para guardar un nuevo código.
         - `Task<CodigoVerificacion> ObtenerCodigoAsync(string codigo)`: Para recuperar un código por su valor.
         - `Task<bool> EliminarCodigoAsync(string codigo)`: Para eliminar un código específico.
         - `Task<bool> EliminaCodigoUsuarioAsync(int usuarioId)`: Para eliminar todos los códigos asociados a un ID de usuario.
     - **4. `RepositorioCodigos` (Implementación del Repositorio de Códigos):**
       - Se implementa `InterfazRepositorioCodigos`.
       - Utiliza `DBConfig` (inyectado) para obtener la cadena de conexión a la base de datos SQLite.
       - `CrearCodigoAsync()`: Abre una conexión SQLite, ejecuta un comando SQL `INSERT` en la tabla `CodigoVerificacion` con los datos del código (ID de usuario, valor del código, fecha de expiración). La fecha de expiración se guarda como string en formato ISO 8601 ("o"). Devuelve el objeto `CodigoVerificacion` creado.
       - `ObtenerCodigoAsync()`: Ejecuta un `SELECT` para buscar un código por su valor. Si se encuentra, mapea los datos a un objeto `CodigoVerificacion` (parseando la fecha de expiración) y lo devuelve. Si no, devuelve `null`.
       - `EliminarCodigoAsync()`: Ejecuta un `DELETE` para eliminar un código por su valor. Devuelve `true` si se afectaron filas, `false` en caso contrario.
       - `EliminaCodigoUsuarioAsync()`: Ejecuta un `DELETE` para eliminar todos los códigos asociados a un `usuarioId`. Devuelve `true` si se afectaron filas.
     - **Integración con `ControladorUsuarios`:**
       - El `ControladorUsuarios` utiliza `ICodeService` para generar códigos y tiempos de expiración, y `InterfazRepositorioCodigos` para persistir y recuperar estos códigos durante los flujos de registro (`SignUp`), inicio de sesión (`SignIn`) y verificación de email (`VerificarEmail`).

### 3. Servicio de Email
   - **Descripción General:**
     El módulo de "Servicio de Email" es responsable de gestionar todas las comunicaciones por correo electrónico salientes de la aplicación. Su función principal es enviar emails transaccionales, como los códigos de verificación para el registro de cuentas y las notificaciones de confirmación de inicio de sesión. Este servicio abstrae la lógica de interacción con el proveedor de correo electrónico externo (en este caso, Resend) y proporciona una interfaz simple para que otros componentes de la aplicación, como el `ControladorUsuarios`, puedan solicitar el envío de emails sin preocuparse por los detalles de implementación subyacentes.

   - **Componentes Clave:**
     - **`EmailSettings.cs` (Modelo de Configuración):**
       - **Ubicación:** `APIMaterialesESCOM/Models/EmailSettings.cs`
       - **Propósito:** Esta clase actúa como un contenedor fuertemente tipado para los parámetros de configuración del servicio de correo. Estos parámetros se cargan desde el archivo `appsettings.json` (o `appsettings.Development.json`) durante el inicio de la aplicación mediante el sistema de configuración de ASP.NET Core (utilizando `IOptions<EmailSettings>`).
       - **Propiedades:**
         - `Mail` (string): Almacena la dirección de correo electrónico del remitente (e.g., "noreply@sistemarepositorio.com"). Esta dirección debe estar verificada y autorizada por el proveedor de servicios de email (Resend).
         - `DisplayName` (string): El nombre que se mostrará como remitente en los correos electrónicos enviados (e.g., "Repositorio Digital ESCOM").
         - `ApiKey` (string): La clave API secreta proporcionada por Resend, utilizada para autenticar las solicitudes HTTP a su servicio de envío de correos.

     - **`IEmailService.cs` (Interfaz del Servicio):**
       - **Ubicación:** `APIMaterialesESCOM/Servicios/IEmailService.cs`
       - **Propósito:** Define el contrato que debe cumplir cualquier implementación del servicio de email. El uso de una interfaz promueve el desacoplamiento, permitiendo que la implementación concreta (`EmailService`) pueda ser sustituida (por ejemplo, por un servicio de email diferente o una implementación simulada para pruebas) sin afectar a los consumidores del servicio.
       - **Métodos Definidos:**
         - `Task<bool> SendEmailAsync(string toEmail, string subject, string message)`: Método asíncrono que toma la dirección del destinatario, el asunto del correo y el cuerpo del mensaje (en formato HTML) como parámetros. Devuelve un booleano indicando si el envío fue exitoso (`true`) o no (`false`).

     - **`EmailService.cs` (Implementación del Servicio):**
       - **Ubicación:** `APIMaterialesESCOM/Servicios/EmailService.cs` (Namespace: `APIMaterialesESCOM.Services`)
       - **Propósito:** Contiene la lógica concreta para enviar correos electrónicos utilizando la API REST de Resend.
       - **Dependencias Inyectadas:**
         - `IOptions<EmailSettings> emailSettings`: Para acceder a los valores de configuración (`Mail`, `DisplayName`, `ApiKey`).
         - `ILogger<EmailService> _logger`: Para registrar información sobre el proceso de envío, así como errores o advertencias.
         - `IHttpClientFactory httpClientFactory`: Para crear instancias de `HttpClient` de manera optimizada y gestionada por el framework. Se utiliza para realizar las llamadas HTTP a la API de Resend.

   - **Procedimiento de Desarrollo Detallado:**
     - **1. Definición del Modelo `EmailSettings`:**
       - Se crea la clase `EmailSettings` con las propiedades `Mail`, `DisplayName`, y `ApiKey`. Estas propiedades se mapearán desde una sección correspondiente en `appsettings.json`, por ejemplo:
         ```json
         "EmailSettings": {
           "Mail": "tu_email_verificado@resend.dev",
           "DisplayName": "Tu Aplicación",
           "ApiKey": "tu_clave_api_de_resend"
         }
         ```
     - **2. Configuración en `Program.cs` (o `Startup.cs`):**
       - Se registra `EmailSettings` en el contenedor de servicios para que pueda ser inyectado mediante `IOptions<EmailSettings>`:
         ```csharp
         builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
         ```
       - Se registra el `HttpClient` nombrado "ResendApi" y el servicio `IEmailService` con su implementación `EmailService`:
         ```csharp
         builder.Services.AddHttpClient("ResendApi");
         builder.Services.AddTransient<IEmailService, EmailService>();
         ```
     - **3. Definición de la Interfaz `IEmailService`:**
       - Se crea la interfaz `IEmailService` con la firma del método `SendEmailAsync` como se describió anteriormente, asegurando que el contrato sea claro y conciso.
     - **4. Implementación de `EmailService`:**
       - **Constructor:**
         - Recibe las dependencias `IOptions<EmailSettings>`, `ILogger<EmailService>`, y `IHttpClientFactory`.
         - Almacena `emailSettings.Value` en un campo privado `_emailSettings`.
         - Obtiene una instancia de `HttpClient` usando `_httpClientFactory.CreateClient("ResendApi")`.
         - Configura el `HttpClient`:
           - `BaseAddress`: Se establece a la URL base de la API de Resend (`https://api.resend.com`).
           - `DefaultRequestHeaders.Authorization`: Se añade una cabecera de autorización `Bearer` con el `_emailSettings.ApiKey`.
           - `DefaultRequestHeaders.Accept`: Se configura para aceptar respuestas en formato `application/json`.
       - **Método `SendEmailAsync(string toEmail, string subject, string message)`:**
         - **Logging Inicial:** Registra el inicio del envío del correo a la dirección `toEmail`.
         - **Creación del Payload:**
           - Construye un objeto anónimo o una clase DTO específica que coincida con la estructura esperada por la API de Resend para el endpoint `/emails`. Este payload incluye:
             - `from`: Formateado como `"DisplayName <email@dominio.com>"` usando `_emailSettings.DisplayName` y `_emailSettings.Mail`.
             - `to`: Un array de strings con la dirección del destinatario (`toEmail`).
             - `subject`: El asunto del correo.
             - `html`: El contenido del mensaje en formato HTML.
         - **Serialización a JSON:** Serializa el objeto del payload a una cadena JSON usando `System.Text.Json.JsonSerializer.Serialize()`.
         - **Registro del Payload:** (Opcional pero útil para depuración) Registra el payload JSON.
         - **Configuración del Contenido HTTP:** Crea un `StringContent` a partir de la cadena JSON, especificando `Encoding.UTF8` y el tipo de contenido `"application/json"`.
         - **Timeout de la Solicitud:** Crea un `CancellationTokenSource` con un timeout (e.g., 30 segundos) para evitar que la solicitud se bloquee indefinidamente.
         - **Envío de la Solicitud HTTP:**
           - Registra el intento de envío a la API de Resend.
           - Realiza una solicitud `POST` asíncrona a `"/emails"` (relativo a la `BaseAddress`) con el `content` y el `cancellationToken`.
         - **Procesamiento de la Respuesta:**
           - Lee el cuerpo de la respuesta (`response.Content.ReadAsStringAsync()`).
           - Registra el código de estado HTTP y el cuerpo de la respuesta de Resend. Esto es crucial para diagnosticar problemas con el envío de correos.
           - Devuelve `response.IsSuccessStatusCode` (que será `true` para códigos de estado HTTP 2xx).
         - **Manejo de Errores:**
           - **`TaskCanceledException`:** Captura específicamente esta excepción, que puede ocurrir si la solicitud excede el timeout configurado. Registra el error de timeout.
           - **`Exception` Genérica:** Captura cualquier otra excepción que pueda surgir. Registra el tipo de excepción, el mensaje y, si existe, el mensaje de la `InnerException` para un diagnóstico más profundo.
           - En ambos casos de error, devuelve `false`.

   - **Integración y Uso:**
     - El `ControladorUsuarios`, por ejemplo, inyecta `IEmailService` y lo utiliza en los métodos `SignUp` y `SignIn` para enviar correos con códigos de verificación o confirmación, pasando el email del destinatario, el asunto y el cuerpo HTML generado por métodos auxiliares como `GenerarCorreoVerificacion` o `GenerarCorreoConfirmar`.

### 4. Modelos de Datos (Directorio `Models`)
   - **Descripción:** (Breve descripción del módulo)
   - **Modelos Principales:**
     - `Usuario.cs`
     - `Autor.cs`
     - `CodigoVerificacion.cs`
     - `ApiRequest.cs`
     - `ApiResponse.cs`
     - `Respuesta.cs`
     - `VerificacionCodigo.cs`
   - **Procedimiento de Desarrollo:**
     - (Paso 1: Explicación detallada para cada modelo o grupo de modelos)
     - ...

### 5. Acceso a Datos (Repositorios - Directorio `Repositorios`)
   - **Descripción:** (Breve descripción del módulo)
   - **Componentes:**
     - `RepositorioUsuarios.cs`
     - `InterfazRepositorioUsuarios.cs`
     - `RepositorioCodigos.cs` (mencionado también en Gestión de Códigos)
     - `InterfazRepositorioCodigos.cs` (mencionado también en Gestión de Códigos)
   - **Procedimiento de Desarrollo:**
     - (Paso 1: Explicación detallada)
     - ...

### 6. Configuración de Conexión a Base de Datos (`Conexion/DBConfig.cs`)
   - **Descripción:** (Breve descripción del módulo)
   - **Procedimiento de Desarrollo:**
     - (Paso 1: Explicación detallada)
     - ...

### 7. Validación (`Validacion/ValidadorAttribute.cs`)
   - **Descripción:** (Breve descripción del módulo)
   - **Procedimiento de Desarrollo:**
     - (Paso 1: Explicación detallada)
     - ...

## Conclusión
(Aquí puedes agregar una conclusión general) 