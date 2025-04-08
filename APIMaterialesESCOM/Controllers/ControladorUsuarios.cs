using APIMaterialesESCOM.Models;
using APIMaterialesESCOM.Repositorios;
using APIMaterialesESCOM.Servicios;
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
        private readonly ITokenService _tokenService;
        private readonly InterfazRepositorioTokens _tokenRepository;
        private readonly InterfazRepositorioLoginTokens _loginTokensRepository;

        // Constructor que inicializa los servicios mediante inyección de dependencias
        public ControladorUsuarios(InterfazRepositorioUsuarios usuarioRepository, IEmailService emailService, ILogger<ControladorUsuarios> logger, ITokenService tokenService, InterfazRepositorioTokens tokenRepository, InterfazRepositorioLoginTokens loginTokensRepository, IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _logger = logger;
            _tokenService = tokenService;
            _tokenRepository = tokenRepository;
            _loginTokensRepository = loginTokensRepository;
            _configuration = configuration;
        }

        // Obtiene la lista completa de usuarios registrados en el sistema
        // GET: repositorio/usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            var usuarios = await _usuarioRepository.GetAllUsuarios();
            return Ok(usuarios);
        }

        // Obtiene la información de un usuario específico por su ID
        // GET: repositorio/usuarios/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _usuarioRepository.GetUsuarioById(id);

            if(usuario == null)
            {
                return NotFound();
            }

            return Ok(usuario);
        }

        // Registra un nuevo usuario en el sistema y envía un correo con sus credenciales
        // POST: repositorio/usuarios/signup
        [HttpPost("signup")]
        public async Task<ActionResult<Usuario>> SignUp(UsuarioSignUp usuarioDto)
        {
            // Validación del modelo de datos recibido
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar si ya existe un usuario con el mismo email
            var existingUser = await _usuarioRepository.GetUsuarioByEmail(usuarioDto.Email);
            if(existingUser != null)
            {
                return Conflict("Ya existe un usuario con este email");
            }

            // Crear el nuevo usuario en la base de datos
            var userId = await _usuarioRepository.CreateUsuario(usuarioDto);

            //Generar token de verificación 
            string token = _tokenService.GenerateToken();
            DateTime expiracíon = _tokenService.GetExpirationTime();

            //Guardar token en base de datos
            await _tokenRepository.CreateTokenAsync(userId, token, expiracíon);

            //Url de verificacion
            string verificacionURL = $"{Request.Scheme}://{Request.Host}/repositorio/usuarios/verify?token={token}";

            // Preparar y enviar correo de bienvenida
            string subject = "Acceso a prototipo de Repositorio Digital ESCOM";
            string message = GenerarCorreoVerificacion(usuarioDto.Nombre, usuarioDto.ApellidoP, usuarioDto.Email, usuarioDto.Boleta, verificacionURL);

            await _emailService.SendEmailAsync(usuarioDto.Email, subject, message);

            // Obtener el usuario creado para devolverlo en la respuesta
            var newUser = await _usuarioRepository.GetUsuarioById(userId);
            return CreatedAtAction(nameof(GetUsuario), new { id = userId }, newUser);
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerificarEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token inválido");
            }

            // Buscar el token en la base de datos
            var verificationToken = await _tokenRepository.GetTokenAsync(token);
            if (verificationToken == null)
            {
                return NotFound("Token no encontrado o ya utilizado");
            }

            // Verificar si el token ha expirado
            if (_tokenService.IsTokenExpired(verificationToken.Expires))
            {
                await _tokenRepository.DeleteTokenAsync(token);
                return BadRequest("El token ha expirado. Solicita un nuevo correo de verificación.");
            }

            // Actualizar estado de verificación del usuario
            bool resultado = await _usuarioRepository.VerificacionEmailAsync(verificationToken.UsuarioId, true);

            // Eliminar el token usado
            await _tokenRepository.DeleteTokenAsync(token);

            // Redirigir al frontend con el resultado
            return Redirect($"http://localhost:3000/");

        }

        // Autentica a un usuario y envía un correo de confirmación de inicio de sesión
        // POST: repositorio/usuarios/signin
        [HttpPost("signin")]
        public async Task<ActionResult<Usuario>> SignIn(UsuarioSignIn signinDto)
        {
            // Verificar credenciales del usuario
            var usuario = await _usuarioRepository.Authenticate(signinDto.Email);
            if(usuario == null)
            {
                return Unauthorized("Email incorrecto");
            }

            // Verificar que el email esté verificado
            bool isVerified = await _usuarioRepository.EmailVerificadoAsync(usuario.Id);
            if(!isVerified)
            {
                return Unauthorized("Tu cuenta no ha sido verificada. Por favor, verifica tu correo electrónico antes de iniciar sesión.");
            }

            // Generar token para autenticación por correo
            string token = _tokenService.GenerateToken();
            DateTime expiracion = _tokenService.GetExpirationTime(); // Usa la expiración estándar

            // Guardar nuevo token en base de datos
            await _tokenRepository.CreateTokenAsync(usuario.Id, token, expiracion);

            // Construir URL de autenticación
            string urlAutenticacion = $"{Request.Scheme}://{Request.Host}/repositorio/usuarios/auth?token={token}&userId={usuario.Id}";

            // Enviar correo de confirmación de inicio de sesión
            string subject = "Confirmar inicio de sesión - Repositorio Digital ESCOM";
            string message = GenerarCorreoConfirmar(
                usuario.Nombre,
                usuario.ApellidoP,
                usuario.Email,
                usuario.Boleta,
                urlAutenticacion
            );

            await _emailService.SendEmailAsync(usuario.Email, subject, message);


            DateTime jwtExpiracion = _tokenService.GetExpirationJWT();
            string jwt = GenerateJwtToken(usuario, jwtExpiracion);

            // Convertir a timestamp (segundos desde epoch)
            long expirationTimestamp = new DateTimeOffset(jwtExpiracion).ToUnixTimeSeconds();

            // Devolver que se requiere verificación por correo
            return Ok(new
            {
                accessToken = jwt,
                expiresAt = expirationTimestamp

            });
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
        private string GenerarCorreoConfirmar(string nombre, string apellido, string email, string boleta, string ConfirmacionURL = null)
        {
            return $@"
        <html>
        <head>
            <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; text-align: center; }}
            h1 {{ color: #2c3e50; }}
            .info {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px auto; max-width: 400px; text-align: left; }}
            .button-container {{ text-align: center; margin: 30px 0; }}
            .button {{ display: inline-block; background-color: #3498db; color: white; padding: 12px 30px; 
                      text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px; }}
            .footer {{ margin-top: 30px; font-size: 12px; color: #7f8c8d; }}
            .note {{ margin: 20px auto; max-width: 450px; }}
        </style>
        </head>
        <body>
            <div class='container'>
                <h1>Confirmar inicio de sesión - Repositorio Digital ESCOM</h1>
                <p>Hola <strong>{nombre} {apellido}</strong>,</p>
                <p>Se ha detectado un intento de inicio de sesión en tu cuenta. Para confirmar que eres tú y completar el proceso, por favor haz clic en el siguiente botón:</p>
                
                <div class='info'>
                    <p><strong>Email:</strong> {email}</p>
                    <p><strong>Boleta:</strong> {boleta}</p>
                </div>
                
                <div class='button-container'>
                    <a href='{ConfirmacionURL}' class='button'>Confirmar inicio de sesión</a>
                </div>
                
                <p>Si no intentaste iniciar sesión, puedes ignorar este correo.</p>
                
                <div class='footer'>
                    <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
                    <p>© ESCOM - IPN {DateTime.Now.Year}</p>
                </div>
            </div>
        </body>
        </html>";
        }

        private string GenerarCorreoVerificacion(string nombre, string apellido, string email, string boleta, string verificacionURL)
        {
            return $@"
                 <html>
        <head>
            <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; text-align: center; }}
            h1 {{ color: #2c3e50; }}
            .info {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px auto; max-width: 400px; text-align: left; }}
            .button-container {{ text-align: center; margin: 30px 0; }}
            .button {{ display: inline-block; background-color: #3498db; color: white; padding: 12px 30px; 
                      text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px; }}
            .footer {{ margin-top: 30px; font-size: 12px; color: #7f8c8d; }}
            .note {{ margin: 20px auto; max-width: 450px; }}
        </style>
        </head>
        <body>
            <div class='container'>
                <h1>Verificación de cuenta - Repositorio Digital ESCOM</h1>
                <p>Hola <strong>{nombre} {apellido}</strong>,</p>
                <p>Para completar tu registro y acceder a nuestra plataforma, por favor verifica tu cuenta:</p>
                
                <div class='info'>
                    <p><strong>Email:</strong> {email}</p>
                    <p><strong>Boleta:</strong> {boleta}</p>
                </div>
                
                <div class='button-container'>
                    <a href='{verificacionURL}' class='button'>Verificar mi cuenta</a>
                </div>
                
                <p>Gracias por utilizar nuestro prototipo de Repositorio Digital ESCOM.</p>
                
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
            // Validar el modelo recibido
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar que el usuario exista
            var usuario = await _usuarioRepository.GetUsuarioById(id);
            if(usuario == null)
            {
                return NotFound();
            }

            // Verificar si se está actualizando el email y si ya existe otro usuario con ese email
            if(!string.IsNullOrEmpty(usuarioDto.Email) && usuarioDto.Email != usuario.Email)
            {
                var existingUser = await _usuarioRepository.GetUsuarioByEmail(usuarioDto.Email);
                if(existingUser != null)
                {
                    return Conflict("Ya existe un usuario con este email");
                }
            }

            // Realizar la actualización
            var result = await _usuarioRepository.UpdateUsuario(id, usuarioDto);
            if(result)
            {
                return NoContent();
            }

            return BadRequest("Error al actualizar el usuario");
        }

        // Elimina un usuario del sistema
        // DELETE: repositorio/usuarios/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            // Verificar que el usuario exista
            var usuario = await _usuarioRepository.GetUsuarioById(id);
            if(usuario == null)
            {
                return NotFound();
            }

            // Realizar la eliminación
            var result = await _usuarioRepository.DeleteUsuario(id);
            if(result)
            {
                return NoContent();
            }

            return BadRequest("Error al eliminar el usuario");
        }

        [HttpGet("auth")]
        public async Task<IActionResult> AutenticarLogin([FromQuery] string token, [FromQuery] int userId)
        {
            if(string.IsNullOrEmpty(token))
            {
                return BadRequest("Token inválido");
            }

            // Buscar el token en la base de datos
            var loginToken = await _tokenRepository.GetTokenAsync(token);
            if(loginToken == null)
            {
                return NotFound("Token no encontrado");
            }

            // Verificar que el token pertenezca al usuario correcto
            if(loginToken.UsuarioId != userId)
            {
                return Unauthorized("El token no corresponde a este usuario");
            }

            // Verificar si el token ha expirado
            if (_tokenService.IsTokenExpired(loginToken.Expires))
            {
                await _tokenRepository.DeleteTokenAsync(token);
                return BadRequest("El token ha expirado. Solicita un nuevo correo de verificación.");
            }

            // Obtener el usuario
            var usuario = await _usuarioRepository.GetUsuarioById(userId);
            if(usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            // Eliminar el token después de usarlo
            await _tokenRepository.DeleteTokenAsync(token);

            // Redirigir a la página principal
            return Redirect($"http://localhost:3000/");
        }

    }
}