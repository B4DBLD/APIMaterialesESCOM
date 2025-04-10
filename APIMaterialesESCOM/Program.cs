using APIMaterialesESCOM.Conexion;
using APIMaterialesESCOM.Models;
using APIMaterialesESCOM.Repositorios;
using APIMaterialesESCOM.Services;
using APIMaterialesESCOM.Servicios;
using Resend;


// Inicializar SQLite
SQLitePCL.Batteries.Init();

var builder = WebApplication.CreateBuilder(args);

// Registrar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuración de la base de datos
var dbConfig = new DBConfig
{
    ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
};
builder.Services.AddSingleton(dbConfig);

// Registrar servicios de la aplicación
builder.Services.AddScoped<InterfazRepositorioUsuarios, RepositorioUsuarios>();

// Configurar los servicios para el envío de correos
builder.Services.AddOptions();
builder.Services.AddHttpClient();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
// Añadir en Program.cs donde registras otros servicios
builder.Services.AddScoped<ICodeService, CodeService>();
builder.Services.AddScoped<InterfazRepositorioCodigos, RepositorioCodigos>();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

app.Urls.Add("http://10.0.0.4:8080");

// Configurar middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();  