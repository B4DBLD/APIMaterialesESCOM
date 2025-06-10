# Conversación Claude Code - APIMaterialesESCOM

## Fecha: 6/7/2025

### Contexto del Proyecto
**APIMaterialesESCOM** es un API REST para un repositorio digital de ESCOM (IPN) que maneja autenticación, gestión de usuarios e integración con microservicio de autores.

### Estado del Proyecto Analizado

#### Arquitectura Principal:
- **ASP.NET Core Web API** (.NET 8.0)
- **Base de datos**: SQLite local
- **Autenticación**: JWT + códigos de verificación por email
- **Email**: Servicio Resend para envío de correos
- **Patrón**: Repository + Dependency Injection

#### Funcionalidades Implementadas:
1. **Sistema de Autenticación Avanzado:**
   - Registro/login con verificación por email (códigos de 6 dígitos)
   - JWT tokens con claims personalizados
   - Autenticación de dos factores para inicio de sesión

2. **Gestión de Usuarios:**
   - CRUD completo de usuarios
   - Sistema de roles: "1" (estudiante), "2" (profesor/autor), "3" (admin)
   - Validación especial para emails `@ipn.mx` (auto-promoción a rol 2)

3. **Integración con Microservicio de Autores:**
   - Para usuarios rol 2 y 3 se crea automáticamente perfil de "autor"
   - Comunicación con API externa para gestionar autores
   - Mantiene relación `usuarioId <-> autorId`

#### Patrón Outbox Implementado (Pendiente de commit):

**Archivos nuevos:**
- `Models/OutboxEvent.cs`: Modelo para cola de eventos
- `Servicios/AutorBackgroundService.cs`: Procesamiento asíncrono de eventos
- `Servicios/AutorDirectService.cs`: Gestión directa con API de autores
- `Repositorios/InterfazRepositorioOutbox.cs` + `RepositorioOutbox.cs`

**Funcionalidad:**
- Cola de eventos para operaciones con microservicio externo
- Procesamiento en background cada 5 segundos
- Manejo de reintentos automático
- Consistencia transaccional (usuario + evento en misma transacción)

### Lógica de Roles Clarificada:

#### **Rol 1 (Usuario general)**
- Cualquier dominio
- No necesita perfil de autor

#### **Rol 2 (Profesor/Autor)**  
- Auto-asignado en registro para `@ipn.mx`
- Puede ser asignado manualmente a cualquier dominio por admin
- Necesita perfil de autor

#### **Rol 3 (Admin con permisos de autor)**
- Cualquier dominio: `@alumno.ipn.mx`, `@ipn.mx`, etc.
- Asignado manualmente por otro admin
- Necesita perfil de autor

### Análisis del Patrón Outbox:

#### **Ventajas identificadas:**
1. **Consistencia Transaccional**: Usuario + evento en la misma transacción
2. **Mejor UX**: Respuesta inmediata, procesamiento en background
3. **Resiliencia**: Reintentos automáticos ante fallos
4. **Desacoplamiento**: API independiente del microservicio externo

#### **Trade-offs:**
1. **Consistencia Eventual**: Autor no disponible inmediatamente
2. **Complejidad**: Más código y componentes
3. **Monitoreo**: Necesidad de observar cola de eventos

#### **Conclusión**: 
El patrón outbox es la decisión correcta para este proyecto debido a:
- Integración con API externa que puede fallar
- Necesidad de operaciones rápidas de registro/actualización
- Manejo inteligente de cambios de rol

### Configuración Actual:
- **DB**: SQLite local en `C:\Users\alexi\Documents\Materiales/BDRepositorio.db`
- **Email**: Resend API con dominio `onboarding@repoescom.lat`
- **JWT**: Configurado con clave, issuer y audience específicos
- **CORS**: Permitido para todos los orígenes (desarrollo)

### Commits Recientes:
- Cambios en lógica de signin/signup
- Estandarización de respuestas API
- Implementación de reenvío de códigos
- Modificaciones para integración con microservicio

### Archivos Pendientes de Commit:
- Implementación completa del patrón outbox
- Nuevos servicios para manejo de autores
- Modificaciones en controladores para usar outbox

---
*Nota: Esta conversación puede continuarse referenciando este archivo para mantener el contexto del proyecto.*