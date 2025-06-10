using APIMaterialesESCOM.Models;
using APIMaterialesESCOM.Repositorios;
using APIMaterialesESCOM.Servicios;
using MicroserviciosRepoEscom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace APIMaterialesESCOM.Controllers
{
    
    // Controlador que maneja las operaciones relacionadas con los usuarios del repositorio digital
    [ApiController]
    [Route("repositorio/usuarios")]
    public class ControladorUsuarios : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly InterfazRepositorioUsuarios _usuarioRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ControladorUsuarios> _logger;
        private readonly ICodeService _codeService;
        private readonly InterfazRepositorioCodigos _codeRepository;
        private readonly InterfazRepositorioOutbox _outboxRepository;
        private readonly IAutorDirectService _autorDirectService;

        // Constantes para roles y mensajes
        private const string ROL_USUARIO_GENERAL = "1";
        private const string ROL_PROFESOR_AUTOR = "2";
        private const string ROL_ADMIN = "3";
        private const string DOMINIO_IPN = "@ipn.mx";
        
        private const string SUBJECT_VERIFICACION = "Acceso a prototipo de Repositorio Digital ESCOM";
        private const string SUBJECT_CONFIRMACION = "Confirmar inicio de sesión - Repositorio Digital ESCOM";
        
        private static readonly string[] ROLES_CON_AUTOR = { ROL_PROFESOR_AUTOR, ROL_ADMIN };

        // Constructor que inicializa los servicios mediante inyección de dependencias
        public ControladorUsuarios(InterfazRepositorioUsuarios usuarioRepository, 
                                   IEmailService emailService, 
                                   InterfazRepositorioOutbox outboxRepository, 
                                   ILogger<ControladorUsuarios> logger, 
                                   ICodeService tokenService, 
                                   InterfazRepositorioCodigos tokenRepository, 
                                   IConfiguration configuration, IAutorDirectService autorDirectService)
        {
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _logger = logger;
            _codeService = tokenService;
            _outboxRepository = outboxRepository;
            _codeRepository = tokenRepository;
            _configuration = configuration;
            _autorDirectService = autorDirectService;
        }

        // Obtiene la lista completa de usuarios registrados en el sistema
        // GET: repositorio/usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            try
            {
                var usuarios = await _usuarioRepository.GetAllUsuarios();

                // Opcional: verificar si hay usuarios
                if (usuarios == null || !usuarios.Any())
                {
                    return Ok(Respuesta<IEnumerable<Usuario>>.Success(new List<Usuario>(), "No se encontraron usuarios"));
                }

                return Ok(Respuesta<IEnumerable<Usuario>>.Success(usuarios));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(500, Respuesta.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // Obtiene la información de un usuario específico por su ID
        // GET: repositorio/usuarios/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            try
            {
                var usuario = await _usuarioRepository.GetUsuarioById(id);

                if (usuario == null)
                {
                    return NotFound(Respuesta.Failure("Usuario no encontrado"));
                }

                return Ok(Respuesta<Usuario>.Success(usuario, "Usuario obtenido exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener usuario con ID {id}");
                return StatusCode(500, Respuesta.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // Registra un nuevo usuario en el sistema y envía un correo con sus credenciales
        // POST: repositorio/usuarios/signup
        [HttpPost("signup")]
        public async Task<ActionResult<Usuario>> SignUp(UsuarioSignUp usuarioDto)
        {
            try
            {
                usuarioDto.rol = "1";
                // Validación del modelo de datos recibido
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(Respuesta<object>.Failure("Datos inválidos", errors));
                }

                // Verificar si ya existe un usuario con el mismo email y su estado de verificación en una sola consulta
                var (existingUser, isVerified) = await _usuarioRepository.GetUsuarioWithVerificationAsync(usuarioDto.Email);
                if (existingUser != null)
                {
                    if (isVerified)
                    {
                        // Si el email ya está verificado, devolver conflicto
                        return Conflict(Respuesta.Failure("Ya existe un usuario con este email"));
                    }
                    else
                    {
                        // Si el email no está verificado, enviar nuevo código
                        if (!await EnviarCodigoVerificacionAsync(existingUser, TipoCorreo.Verificacion))
                        {
                            return StatusCode(500, Respuesta.Failure("Error al enviar el correo de verificación"));
                        }

                        // Retornar respuesta exitosa
                        return Ok(Respuesta<object>.Success(
                            new { Id = existingUser.Id },
                            "Se ha reenviado un código de verificación a tu correo electrónico"
                        ));
                    }
                }


                // Crear el nuevo usuario en la base de datos
                var userId = await _usuarioRepository.CreateUsuario(usuarioDto);

                // Si es email @ipn.mx, programar creación de autor en background
                if (usuarioDto.Email.Contains("@ipn.mx"))
                {
                    usuarioDto.rol = "2";
                    
                    var eventData = new UsuarioEventData
                    {
                        UsuarioId = userId,
                        Email = usuarioDto.Email,
                        Nombre = usuarioDto.Nombre,
                        ApellidoP = usuarioDto.ApellidoP,
                        ApellidoM = usuarioDto.ApellidoM,
                        RolNuevo = "2",
                        RolAnterior = "1"
                    };
                    
                    await _outboxRepository.AddEventAsync("CREAR_AUTOR", JsonSerializer.Serialize(eventData), userId);
                }


                // Crear objeto Usuario para el método auxiliar
                var nuevoUsuario = new Usuario
                {
                    Id = userId,
                    Nombre = usuarioDto.Nombre,
                    ApellidoP = usuarioDto.ApellidoP,
                    ApellidoM = usuarioDto.ApellidoM,
                    Email = usuarioDto.Email,
                    Boleta = usuarioDto.Boleta
                };

                // Enviar código de verificación usando método auxiliar
                if (!await EnviarCodigoVerificacionAsync(nuevoUsuario, TipoCorreo.Verificacion))
                {
                    return StatusCode(500, Respuesta.Failure("Error al enviar el correo de verificación"));
                }

                // Devolver respuesta estándar
                return Ok(Respuesta<object>.Success(
                    new 
                    { 
                        Id = userId
                    },
                    "Se ha enviado un código de verificación a tu correo electrónico"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario");
                return StatusCode(500, Respuesta.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        // Autentica a un usuario y envía un correo de confirmación de inicio de sesión
        // POST: repositorio/usuarios/signin
        [HttpPost("signin")]
        public async Task<ActionResult<Usuario>> SignIn(UsuarioSignIn signinDto)
        {
            try
            {
                // Verificar credenciales del usuario y estado de verificación en una sola consulta
                int autorID = 0;
                var (usuario, isVerified) = await _usuarioRepository.GetUsuarioWithVerificationAsync(signinDto.Email);
                if (usuario == null)
                {
                    return Unauthorized(Respuesta.Failure("Email incorrecto"));
                }

                // Verificar que el email esté verificado
                if (!isVerified)
                {
                    return Unauthorized(Respuesta.Failure(
                        "Tu cuenta no ha sido verificada. Por favor, verifica tu correo electrónico antes de iniciar sesión."));
                }

                // Enviar código de confirmación usando método auxiliar
                if (!await EnviarCodigoVerificacionAsync(usuario, TipoCorreo.Confirmacion))
                {
                    return StatusCode(500, Respuesta.Failure("Error al enviar el correo de confirmación"));
                }

                // Devolver respuesta estándar
                return Ok(Respuesta<object>.Success(
                    new
                    {
                        Id = usuario.Id
                    }
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión");
                return StatusCode(500, Respuesta.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }

        }

        //Metodo auxiliar para generar el JWT
        private string GenerateJwtToken(Usuario usuario, DateTime expiracion, int autorId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("id", usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim("nombre", usuario.Nombre),
                new Claim("apellidoP", usuario.ApellidoP),
                new Claim("apellidoM", usuario.ApellidoM),
                new Claim("boleta", usuario.Boleta),
                new Claim("rol", usuario.Rol),
                new Claim("autorId", autorId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiracion,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Enum para tipos de correo
        private enum TipoCorreo
        {
            Verificacion,
            Confirmacion
        }

        // Método auxiliar centralizado para generar y enviar códigos de verificación
        private async Task<bool> EnviarCodigoVerificacionAsync(Usuario usuario, TipoCorreo tipoCorreo)
        {
            try
            {
                // 1. Limpiar códigos previos
                await _codeRepository.EliminaCodigoUsuarioAsync(usuario.Id);

                // 2. Generar nuevo código
                string codigo = _codeService.GenerarCodigo();
                DateTime expiracion = _codeService.TiempoExpiracion();

                // 3. Guardar en BD
                await _codeRepository.CrearCodigoAsync(usuario.Id, codigo, expiracion);

                // 4. Determinar contenido según tipo (los métodos HTML formatean el código internamente)
                var (subject, message) = tipoCorreo switch
                {
                    TipoCorreo.Verificacion => (
                        SUBJECT_VERIFICACION,
                        GenerarCorreoVerificacion(usuario.Nombre, usuario.ApellidoP, usuario.Email, usuario.Boleta, codigo)
                    ),
                    TipoCorreo.Confirmacion => (
                        SUBJECT_CONFIRMACION,
                        GenerarCorreoConfirmar(usuario.Nombre, usuario.ApellidoP, usuario.Email, usuario.Boleta, codigo)
                    ),
                    _ => throw new ArgumentException("Tipo de correo no válido")
                };

                // 6. Enviar correo
                return await _emailService.SendEmailAsync(usuario.Email, subject, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enviando código de verificación a {usuario.Email}");
                return false;
            }
        }

        // Método auxiliar para generar el contenido HTML del correo electrónico
        // con información personalizada del usuario y un botón de acceso
        private string GenerarCorreoConfirmar(string nombre, string apellido, string email, string boleta, string codigo)
        {
            // Formatear el código como XXX-XXX
            string codigoFormateado = codigo.Length == 6
                ? $"{codigo.Substring(0, 3)}-{codigo.Substring(3, 3)}"
                : codigo;

            return $@"
                <html>
                <head>
                    <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; text-align: center; }}
                    h1 {{ color: #2c3e50; }}
                    .info {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px auto; max-width: 400px; text-align: left; }}
                    .code {{ font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #3498db; display: block; margin: 30px 0; }}
                    .footer {{ margin-top: 30px; font-size: 12px; color: #7f8c8d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>Confirmar inicio de sesión - Repositorio Digital ESCOM</h1>
                        <p>Hola <strong>{nombre} {apellido}</strong>,</p>
                        <p>Se ha detectado un intento de inicio de sesión en tu cuenta. Para confirmar que eres tú, utiliza el siguiente código:</p>
                        
                        <div class='info'>
                            <p><strong>Email:</strong> {email}</p>
                            <p><strong>Boleta:</strong> {boleta}</p>
                        </div>
                        
                        <span class='code'>{codigoFormateado}</span>
                        
                        <p>Ingresa este código en la página de inicio de sesión para completar el proceso.</p>
                        <p>Este código expirará en 1 hora.</p>
                        
                        <div class='footer'>
                            <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
                            <p>© ESCOM - IPN {DateTime.UtcNow.Year}</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerarCorreoVerificacion(string nombre, string apellido, string email, string boleta, string codigo)
        {
            // Formatear el código como XXX-XXX
            string codigoFormateado = codigo.Length == 6
                ? $"{codigo.Substring(0, 3)}-{codigo.Substring(3, 3)}"
                : codigo;

            return $@"
                <html>
                <head>
                    <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; text-align: center; }}
                    h1 {{ color: #2c3e50; }}
                    .info {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px auto; max-width: 400px; text-align: left; }}
                    .code {{ font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #3498db; display: block; margin: 30px 0; }}
                    .footer {{ margin-top: 30px; font-size: 12px; color: #7f8c8d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>Verificación de cuenta - Repositorio Digital ESCOM</h1>
                        <p>Hola <strong>{nombre} {apellido}</strong>,</p>
                        <p>Para completar tu registro, utiliza el siguiente código de verificación:</p>
                        
                        <div class='info'>
                            <p><strong>Email:</strong> {email}</p>
                            <p><strong>Boleta:</strong> {boleta}</p>
                        </div>
                        
                        <span class='code'>{codigoFormateado}</span>
                        
                        <p>Ingresa este código en la página de verificación para activar tu cuenta.</p>
                        <p>Este código expirará en 1 hora.</p>
                        
                        <div class='footer'>
                            <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
                            <p>© ESCOM - IPN {DateTime.Now.Year}</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        // Actualiza la información de un usuario existente
        // PUT: repositorio/usuarios/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, UsuarioUpdate usuarioDto)
        {
            try
            {
                // Validar el modelo recibido
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(Respuesta.Failure("Datos inválidos", errors));
                }

                // Verificar que el usuario exista
                var usuario = await _usuarioRepository.GetUsuarioById(id);
                if (usuario == null)
                {
                    return NotFound(Respuesta.Failure("Usuario no encontrado"));
                }

                // Verificar si se está actualizando el email y si ya existe otro usuario con ese email
                if (!string.IsNullOrEmpty(usuarioDto.Email) && usuarioDto.Email != usuario.Email)
                {
                    var (existingUser, _) = await _usuarioRepository.GetUsuarioWithVerificationAsync(usuarioDto.Email);
                    if (existingUser != null)
                    {
                        return Conflict(Respuesta.Failure("Ya existe un usuario con este email"));
                    }
                }

                // Determinar qué evento programar según el cambio de rol
                string tipoEvento = _autorDirectService.DeterminarTipoEventoActualizacion(usuario.Rol, usuarioDto.Rol);
                
                if (tipoEvento != "SIN_CAMBIOS")
                {
                    var eventData = new UsuarioEventData
                    {
                        UsuarioId = id,
                        Email = usuarioDto.Email,
                        Nombre = usuarioDto.Nombre,
                        ApellidoP = usuarioDto.ApellidoP,
                        ApellidoM = usuarioDto.ApellidoM,
                        RolAnterior = usuario.Rol,
                        RolNuevo = usuarioDto.Rol
                    };
                    
                    await _outboxRepository.AddEventAsync(tipoEvento, JsonSerializer.Serialize(eventData), id);
                }

                    // Realizar la actualización
                    var result = await _usuarioRepository.UpdateUsuario(id, usuarioDto);
                if (result)
                {
                    return Ok(Respuesta.Success("Usuario actualizado exitosamente"));
                }



                return BadRequest(Respuesta.Failure("Error al actualizar el usuario"));

            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión");
                return StatusCode(500, Respuesta.Failure("Error interno del servidor.", new List<string> { ex.Message }));

            }
        }

        // Elimina un usuario del sistema
        // DELETE: repositorio/usuarios/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            try
            {
                // Verificar que el usuario exista
                var usuario = await _usuarioRepository.GetUsuarioById(id);
                if (usuario == null)
                {
                    return NotFound(Respuesta.Failure("Usuario no encontrado"));
                }

                // Realizar la eliminación
                var result = await _usuarioRepository.DeleteUsuario(id);
                if (result)
                {
                    return Ok(Respuesta.Success("Usuario eliminado exitosamente"));
                }

                return BadRequest(Respuesta.Failure("Error al eliminar el usuario"));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión");
                return StatusCode(500, Respuesta.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }
        }

        [HttpPost("verifyCode")]
        public async Task<IActionResult> VerificarEmail([FromBody] VerificacionCodigo verificacion)
        {
            try
            {
                int autorID = 0;
                if (string.IsNullOrEmpty(verificacion.Codigo) || verificacion.UsuarioId <= 0)
                {
                    return BadRequest(Respuesta.Failure("Código inválido o usuario no especificado"));
                }

                // Obtener el usuario
                var usuario = await _usuarioRepository.GetUsuarioById(verificacion.UsuarioId);
                if (usuario == null)
                {
                    return NotFound(Respuesta.Failure("Usuario no encontrado"));
                }

                // Obtener autorID para JWT (llamada síncrona solo para login)
                if (usuario.Rol == "2" || usuario.Rol == "3")
                {
                    autorID = await _autorDirectService.ObtenerAutorIdAsync(usuario.Id);
                }

                // Buscar el código en la base de datos
                var verificationCode = await _codeRepository.ObtenerCodigoAsync(verificacion.Codigo);
                if (verificationCode == null)
                {
                    return NotFound(Respuesta.Failure("Código no encontrado o ya utilizado"));
                }

                // Verificar que el código pertenezca al usuario correcto
                if (verificationCode.UsuarioId != verificacion.UsuarioId)
                {
                    return Unauthorized(Respuesta.Failure("El código no corresponde a este usuario"));
                }

                // Verificar si el código ha expirado
                if (_codeService.ExpiracionCodigo(verificationCode.Expires))
                {
                    await _codeRepository.EliminarCodigoAsync(verificacion.Codigo);
                    return BadRequest(Respuesta.Failure("El código ha expirado. Solicita un nuevo código de verificación."));
                }

                // Verificar si es registro o login basado en emailVerified (ya disponible en el objeto usuario)
                bool isVerified = usuario.VerificacionEmail;

                // Si no está verificado, es un código de registro (actualizar estado)
                if (!isVerified)
                {
                    await _usuarioRepository.VerificacionEmailAsync(verificationCode.UsuarioId, true);
                }

                // Eliminar el código usado
                await _codeRepository.EliminarCodigoAsync(verificacion.Codigo);

                // Generar JWT
                DateTime jwtExpiracion = _codeService.TiempoExpiracionJWT();
                string jwt = GenerateJwtToken(usuario, jwtExpiracion, autorID);
                long expirationTimestamp = new DateTimeOffset(jwtExpiracion).ToUnixTimeSeconds();

                // Devolver respuesta exitosa con JWT
                return Ok(Respuesta<object>.Success(
                     new
                     {
                         accessToken = jwt
                     }
                 ));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión");
                return StatusCode(500, Respuesta.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }

        }

    }
}