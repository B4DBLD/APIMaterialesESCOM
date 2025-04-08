using APIMaterialesESCOM.Models;

namespace APIMaterialesESCOM.Repositorios
{
    // Interfaz que define las operaciones disponibles para el repositorio de usuarios
    // Establece el contrato que cualquier implementación debe seguir
    public interface InterfazRepositorioUsuarios
    {
        // Obtiene todos los usuarios registrados en el sistema
        // Retorna: Lista de objetos Usuario
        Task<IEnumerable<Usuario>> GetAllUsuarios();

        // Busca un usuario específico por su ID
        // Parámetro: id - Identificador único del usuario
        // Retorna: El objeto Usuario si existe, null si no se encuentra
        Task<Usuario?> GetUsuarioById(int id);

        // Busca un usuario por su dirección de correo electrónico
        // Parámetro: email - Correo electrónico a buscar
        // Retorna: El objeto Usuario si existe, null si no se encuentra
        Task<Usuario?> GetUsuarioByEmail(string email);

        // Crea un nuevo usuario en el sistema
        // Parámetro: usuario - Datos del nuevo usuario a crear
        // Retorna: El ID del usuario creado
        Task<int> CreateUsuario(UsuarioSignUp usuario);

        // Actualiza la información de un usuario existente
        // Parámetros: id - ID del usuario a actualizar, usuario - Datos a actualizar
        // Retorna: true si la actualización fue exitosa, false en caso contrario
        Task<bool> UpdateUsuario(int id, UsuarioUpdate usuario);

        // Elimina un usuario del sistema
        // Parámetro: id - ID del usuario a eliminar
        // Retorna: true si la eliminación fue exitosa, false en caso contrario
        Task<bool> DeleteUsuario(int id);

        // Autentica a un usuario utilizando email y boleta
        // Parámetros: email - Correo electrónico del usuario, boleta - Identificación del usuario
        // Retorna: El objeto Usuario si la autenticación es exitosa, null si las credenciales son incorrectas
        Task<Usuario?> Authenticate(string email);

        Task<bool> VerificacionEmailAsync(int userId, bool verified);
        Task<bool> EmailVerificadoAsync(int userId);
    }
}