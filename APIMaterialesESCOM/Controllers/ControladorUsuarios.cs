using APIMaterialesESCOM.Models;
using APIMaterialesESCOM.Repositorios;
using APIMaterialesESCOM.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace APIMaterialesESCOM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ControladorUsuarios: ControllerBase
    {
        private readonly InterfazRepositorioUsuarios _usuarioRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ControladorUsuarios> _logger;

        public ControladorUsuarios(InterfazRepositorioUsuarios usuarioRepository, IEmailService emailService, ILogger<ControladorUsuarios> logger)
        {
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
        var usuarios = await _usuarioRepository.GetAllUsuarios();
        return Ok(usuarios);
        }

        // GET: api/Usuarios/
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
        var usuario = await _usuarioRepository.GetUsuarioById(id);

        if (usuario == null)
        {
        return NotFound();
        }

        return Ok(usuario);
        }

        // POST: api/Usuarios/signup
        [HttpPost("signup")]
        public async Task<ActionResult<Usuario>> SignUp(UsuarioSignUp usuarioDto)
        {

        // ModelState se validará automáticamente gracias al atributo [ApiController]
        if (!ModelState.IsValid)
        {
        return BadRequest(ModelState);
        }

        // Verificar si ya existe un usuario con el mismo email
        var existingUser = await _usuarioRepository.GetUsuarioByEmail(usuarioDto.Email);
        if (existingUser != null)
        {
                return Conflict("Ya existe un usuario con este email");
        }

        var userId = await _usuarioRepository.CreateUsuario(usuarioDto);
            string subject = "Acceso a prototipo de Repositorio Digital ESCOM";
            string message = GenerarCorreoAcceso(usuarioDto.Nombre, usuarioDto.ApellidoP, usuarioDto.Email, usuarioDto.Boleta);

            await _emailService.SendEmailAsync(usuarioDto.Email, subject, message);

            var newUser = await _usuarioRepository.GetUsuarioById(userId);
            return CreatedAtAction(nameof(GetUsuario), new { id = userId }, newUser);
        }



        // POST: api/Usuarios/signin
        [HttpPost("signin")]
        public async Task<ActionResult<Usuario>> SignIn(UsuarioSignIn signinDto)
        {
        var usuario = await _usuarioRepository.Authenticate(signinDto.Email, signinDto.Boleta);
        if (usuario == null)
        {
        return Unauthorized("Email o boleta incorrectos");
        }

            // Enviar el mismo correo de acceso
            string subject = "Acceso a prototipo de Repositorio Digital ESCOM";
            string message = GenerarCorreoAcceso(usuario.Nombre, usuario.ApellidoP, usuario.Email, usuario.Boleta);

            // Enviamos el correo de forma asíncrona
            _ = _emailService.SendEmailAsync(usuario.Email, subject, message);

            return Ok(usuario);
        }

        // Método auxiliar para generar el mismo formato de correo en ambos casos
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

        // PUT: api/Usuarios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, UsuarioUpdate usuarioDto)
        {

        if (!ModelState.IsValid)
        {
        return BadRequest(ModelState);
        }

        var usuario = await _usuarioRepository.GetUsuarioById(id);
        if (usuario == null)
        {
                return NotFound();
        }

        // Verificar si se está actualizando el email y si ya existe otro usuario con ese email
        if (!string.IsNullOrEmpty(usuarioDto.Email) && usuarioDto.Email != usuario.Email)
        {
        var existingUser = await _usuarioRepository.GetUsuarioByEmail(usuarioDto.Email);
        if (existingUser != null)
        {
        return Conflict("Ya existe un usuario con este email");
        }
        }

        var result = await _usuarioRepository.UpdateUsuario(id, usuarioDto);
        if (result)
        {
        return NoContent();
        }

        return BadRequest("Error al actualizar el usuario");
        }

        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
        var usuario = await _usuarioRepository.GetUsuarioById(id);
        if (usuario == null)
        {
        return NotFound();
        }

        var result = await _usuarioRepository.DeleteUsuario(id);
        if (result)
        {
        return NoContent();
        }

        return BadRequest("Error al eliminar el usuario");
        }
    }
}
