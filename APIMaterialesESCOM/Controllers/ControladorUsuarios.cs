using APIMaterialesESCOM.Models;
using APIMaterialesESCOM.Repositorios;
using APIMaterialesESCOM.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace APIMaterialesESCOM.Controllers
{
    // Controlador que maneja las operaciones relacionadas con los usuarios del repositorio digital
    [ApiController]
    [Route("repositorio/usuarios")]
    public class ControladorUsuarios : ControllerBase
    {
        private readonly InterfazRepositorioUsuarios _usuarioRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ControladorUsuarios> _logger;

        // Constructor que inicializa los servicios mediante inyección de dependencias
        public ControladorUsuarios(InterfazRepositorioUsuarios usuarioRepository, IEmailService emailService, ILogger<ControladorUsuarios> logger)
        {
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _logger = logger;
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

            // Preparar y enviar correo de bienvenida
            string subject = "Acceso a prototipo de Repositorio Digital ESCOM";
            string message = GenerarCorreoAcceso(usuarioDto.Nombre, usuarioDto.ApellidoP, usuarioDto.Email, usuarioDto.Boleta);

            await _emailService.SendEmailAsync(usuarioDto.Email, subject, message);

            // Obtener el usuario creado para devolverlo en la respuesta
            var newUser = await _usuarioRepository.GetUsuarioById(userId);
            return CreatedAtAction(nameof(GetUsuario), new { id = userId }, newUser);
        }

        // Autentica a un usuario y envía un correo de confirmación de inicio de sesión
        // POST: repositorio/usuarios/signin
        [HttpPost("signin")]
        public async Task<ActionResult<Usuario>> SignIn(UsuarioSignIn signinDto)
        {
            // Verificar credenciales del usuario
            var usuario = await _usuarioRepository.Authenticate(signinDto.Email, signinDto.Boleta);
            if(usuario == null)
            {
                return Unauthorized("Email o boleta incorrectos");
            }

            // Enviar correo de confirmación de inicio de sesión
            string subject = "Acceso a prototipo de Repositorio Digital ESCOM";
            string message = GenerarCorreoAcceso(usuario.Nombre, usuario.ApellidoP, usuario.Email, usuario.Boleta);

            // Enviamos el correo de forma asíncrona sin esperar su finalización
            // para no bloquear la respuesta al usuario
            _ = _emailService.SendEmailAsync(usuario.Email, subject, message);

            return Ok(usuario);
        }

        // Método auxiliar para generar el contenido HTML del correo electrónico
        // con información personalizada del usuario y un botón de acceso
        private string GenerarCorreoAcceso(string nombre, string apellido, string email, string boleta)
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
                <h1>Acceso al prototipo de Repositorio Digital ESCOM</h1>
                <p>Hola <strong>{nombre} {apellido}</strong>,</p>
                <p>Puedes acceder a nuestra plataforma con la siguiente información:</p>
                
                <div class='info'>
                    <p><strong>Email:</strong> {email}</p>
                    <p><strong>Boleta:</strong> {boleta}</p>
                </div>
                
                <div class='button-container'>
                    <a href='http://localhost:3000' class='button'>Ir a la página principal</a>
                </div>
                
                <p>Gracias por utilizar nuestro prototipo de Repositorio Digital ESCOM.</p>
                
                <div class='footer'>
                    <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
                    <p>© ESCOM - IPN 2025</p>
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
    }
}