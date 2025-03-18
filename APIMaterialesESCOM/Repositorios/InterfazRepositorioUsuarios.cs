using APIMaterialesESCOM.Models;
using static APIMaterialesESCOM.Models.Usuario;

namespace APIMaterialesESCOM.Repositorios
{
    public interface InterfazRepositorioUsuarios
    {
            // Operaciones CRUD básicas
            Task<IEnumerable<Usuario>> GetAllUsuarios();
            Task<Usuario?> GetUsuarioById(int id);
            Task<Usuario?> GetUsuarioByEmail(string email);
            Task<int> CreateUsuario(UsuarioSignUp usuario);
            Task<bool> UpdateUsuario(int id, UsuarioUpdate usuario);
            Task<bool> DeleteUsuario(int id);

            // Operación de autenticación
            Task<Usuario?> Authenticate(string email, string boleta);
    }
}
