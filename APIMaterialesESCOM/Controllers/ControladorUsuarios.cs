using APIMaterialesESCOM.Models;
using APIMaterialesESCOM.Repositorios;
using APIMaterialesESCOM.Servicios;
using MicroserviciosRepoEscom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

        // Constructor que inicializa los servicios mediante inyección de dependencias
        public ControladorUsuarios(InterfazRepositorioUsuarios usuarioRepository, IEmailService emailService, ILogger<ControladorUsuarios> logger, ICodeService tokenService, InterfazRepositorioCodigos tokenRepository, IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _logger = logger;
            _codeService = tokenService;
            _codeRepository = tokenRepository;
            _configuration = configuration;
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
                int autorID = 0;
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

                // Verificar si ya existe un usuario con el mismo email
                var existingUser = await _usuarioRepository.GetUsuarioByEmail(usuarioDto.Email);
                if (existingUser != null)
                {
                    // Verificar si el email ya está verificado
                    bool isVerified = await _usuarioRepository.EmailVerificadoAsync(existingUser.Id);

                    if (isVerified)
                    {
                        // Si el email ya está verificado, devolver conflicto
                        return Conflict(Respuesta.Failure("Ya existe un usuario con este email"));
                    }
                    else
                    {
                        // Si el email no está verificado, enviar nuevo código

                        // Eliminar cualquier código previo
                        await _codeRepository.EliminaCodigoUsuarioAsync(existingUser.Id);

                        // Generar nuevo código numérico
                        string recodigo = _codeService.GenerarCodigo();
                        DateTime expiracion = _codeService.TiempoExpiracion();

                        // Guardar código en base de datos
                        await _codeRepository.CrearCodigoAsync(existingUser.Id, recodigo, expiracion);

                        // Formatear código para el correo
                        string codigoFormateado = recodigo.Length == 6
                            ? $"{recodigo.Substring(0, 3)}-{recodigo.Substring(3, 3)}"
                            : recodigo;

                        // Preparar y enviar correo con código
                        string resubject = "Acceso a prototipo de Repositorio Digital ESCOM";
                        string remessage = GenerarCorreoVerificacion(
                            existingUser.Nombre,
                            existingUser.ApellidoP,
                            existingUser.Email,
                            existingUser.Boleta,
                            codigoFormateado
                        );

                        await _emailService.SendEmailAsync(existingUser.Email, resubject, remessage);

                        // Retornar respuesta exitosa
                        return Ok(Respuesta<object>.Success(
                            new { Id = existingUser.Id },
                            "Se ha reenviado un código de verificación a tu correo electrónico"
                        ));
                    }
                }



                // Crear el nuevo usuario en la base de datos
                var userId = await _usuarioRepository.CreateUsuario(usuarioDto);

                if (usuarioDto.Email.Contains("@ipn.mx"))
                {
                    usuarioDto.rol = "2";
                    ApiRequest apiCliente = new ApiRequest();
                    var autor = new Autor
                    {
                        Nombre = usuarioDto.Nombre,
                        Apellido = $"{usuarioDto.ApellidoP} {usuarioDto.ApellidoM}",
                        Email = usuarioDto.Email
                    };
                    ApiResponse GetResponse = await apiCliente.ObtenerAutor(autor.Email.ToString());
                    if (GetResponse != null)
                    {
                        if (GetResponse.Ok == true)
                        {
                            autorID = GetResponse.Data.Id;
                        }
                        else
                        {
                            // Si no se encuentra el autor, se crea uno nuevo
                            var createResponse = await apiCliente.CrearAutor(autor);
                            if (createResponse.Ok)
                            {
                                autorID = createResponse.Data.Id;
                            }
                            else
                            {
                                return BadRequest(Respuesta.Failure("Error al crear el autor en el sistema externo"));
                            }
                        }

                        var relacionResponse = await apiCliente.CrearRelacion(userId, autorID);
                        if (relacionResponse.Ok == false)
                        {
                            return BadRequest(Respuesta.Failure("Error al crear la relación entre el usuario y el autor en el sistema externo"));
                        }

                    }
                    else
                    {
                        return BadRequest(Respuesta.Failure("La respuesta fue nula"));
                    }

                }


                await _codeRepository.EliminaCodigoUsuarioAsync(userId);

                //Generar el código de verificación 
                string codigo = _codeService.GenerarCodigo();
                DateTime expiracíon = _codeService.TiempoExpiracion();

                //Guardar token en base de datos
                await _codeRepository.CrearCodigoAsync(userId, codigo, expiracíon);

                // Preparar y enviar correo con código
                string subject = "Acceso a prototipo de Repositorio Digital ESCOM";
                string message = GenerarCorreoVerificacion(
                    usuarioDto.Nombre,
                    usuarioDto.ApellidoP,
                    usuarioDto.Email,
                    usuarioDto.Boleta,
                    codigo
                );

                await _emailService.SendEmailAsync(usuarioDto.Email, subject, message);

                // Obtener el usuario creado para devolverlo en la respuesta
                return Ok(Respuesta<object>.Success(
                    new { Id = userId, autorID = autorID },
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
                // Verificar credenciales del usuario}
                int autorID = 0;
                var usuario = await _usuarioRepository.Authenticate(signinDto.Email);
                if (usuario == null)
                {
                    return Unauthorized(Respuesta.Failure("Email incorrecto"));
                }

                // Verificar que el email esté verificado
                bool isVerified = await _usuarioRepository.EmailVerificadoAsync(usuario.Id);
                if (!isVerified)
                {
                    return Unauthorized(Respuesta.Failure("Tu cuenta no ha sido verificada. Por favor, verifica tu correo electrónico antes de iniciar sesión."));
                }

                await _codeRepository.EliminaCodigoUsuarioAsync(usuario.Id);

                // Generar token para autenticación por correo
                string codigo = _codeService.GenerarCodigo();
                DateTime expiracion = _codeService.TiempoExpiracion(); // Usa la expiración estándar

                // Guardar nuevo token en base de datos
                await _codeRepository.CrearCodigoAsync(usuario.Id, codigo, expiracion);


                // Enviar correo de confirmación de inicio de sesión
                string subject = "Confirmar inicio de sesión - Repositorio Digital ESCOM";
                string message = GenerarCorreoConfirmar(
                    usuario.Nombre,
                    usuario.ApellidoP,
                    usuario.Email,
                    usuario.Boleta,
                    codigo
                );

                await _emailService.SendEmailAsync(usuario.Email, subject, message);

                if (usuario.Rol == "2")
                {
                    ApiRequest apiCliente = new ApiRequest();
                    var GetRelacionResponse = await apiCliente.GetRelacion(usuario.Id);
                    if (GetRelacionResponse.Ok)
                    {
                        autorID = GetRelacionResponse.Data.Id;
                    }
                    else
                    {
                        return BadRequest(Respuesta.Failure($"Error al obtener la relación del ID {usuario.Id}"));
                    }

                }


                DateTime jwtExpiracion = _codeService.TiempoExpiracionJWT();
                string jwt = GenerateJwtToken(usuario, jwtExpiracion);

                // Convertir a timestamp (segundos desde epoch)
                long expirationTimestamp = new DateTimeOffset(jwtExpiracion).ToUnixTimeSeconds();

                // Devolver que se requiere verificación por correo
                return Ok(Respuesta<object>.Success(
                    new
                    {
                        Id = usuario.Id,
                        autorID = autorID,
                        accessToken = jwt,
                        expiresAt = expirationTimestamp
                    }
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión");
                return StatusCode(500, Respuesta.Failure("Error interno del servidor.", new List<string> { ex.Message }));
            }

        }

        private string GenerateJwtToken(Usuario usuario, DateTime expiracion)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim("nombre", usuario.Nombre),
                new Claim("apellido", usuario.ApellidoP),
                new Claim("boleta", usuario.Boleta),
                new Claim("rol", usuario.Rol),
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
                    var existingUser = await _usuarioRepository.GetUsuarioByEmail(usuarioDto.Email);
                    if (existingUser != null)
                    {
                        return Conflict(Respuesta.Failure("Ya existe un usuario con este email"));
                    }
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

                // Verificar si es registro o login basado en emailVerified
                bool isVerified = await _usuarioRepository.EmailVerificadoAsync(usuario.Id);

                // Si no está verificado, es un código de registro (actualizar estado)
                if (!isVerified)
                {
                    await _usuarioRepository.VerificacionEmailAsync(verificationCode.UsuarioId, true);
                }

                // Eliminar el código usado
                await _codeRepository.EliminarCodigoAsync(verificacion.Codigo);

                // Generar JWT
                DateTime jwtExpiracion = _codeService.TiempoExpiracionJWT();
                string jwt = GenerateJwtToken(usuario, jwtExpiracion);
                long expirationTimestamp = new DateTimeOffset(jwtExpiracion).ToUnixTimeSeconds();

                // Devolver respuesta exitosa con JWT
                return Ok(Respuesta<object>.Success(
                     new
                     {
                         accessToken = jwt,
                         expiresAt = expirationTimestamp
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