# Backend API Base (.NET 8)

Plantilla base para construir APIs REST sobre .NET 8 con autenticacion JWT, validaciones con FluentValidation y un repositorio ADO.NET compatible con MySQL, SQL Server, PostgreSQL u Oracle. El objetivo es ofrecer un punto de partida neutro que resuelva autenticacion y gestion basica de usuarios sin imponer reglas de negocio especificas.

## Caracteristicas clave
- Arquitectura por capas (`Domain`, `Application`, `Infrastructure`, `Web`) con responsabilidades bien separadas.
- Autenticacion con JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`) y generacion de tokens a traves de `TokenService`.
- Validaciones desacopladas con FluentValidation registradas via `AddValidatorsFromAssemblyContaining`.
- Repositorio de usuarios basado en ADO.NET con fabrica de conexiones y SQL adaptado por proveedor.
- Servicio de contrasenas que genera credenciales robustas y almacena hashes con `BCrypt.Net`.
- Documentacion y pruebas manuales via Swagger con esquema de seguridad Bearer preconfigurado.

## Requisitos previos
- .NET SDK 8.0 o superior.
- Un motor de base de datos compatible (MySQL, SQL Server, PostgreSQL u Oracle).
- Herramienta cliente para bases de datos y cualquier IDE/editor que prefieras.

## Puesta en marcha rapida
```bash
dotnet restore
dotnet build
dotnet run
```
La API se publica en `https://localhost:5001` y `http://localhost:5000`. Swagger UI queda disponible en `/swagger`.

## Configuracion

### JWT
Define la seccion `Jwt` en `appsettings.json` (o en el origen de configuracion que prefieras):

```json
"Jwt": {
  "Key": "clave-secreta-de-al-menos-32-caracteres",
  "Issuer": "backend-api-base",
  "Audience": "backend-api-base-clients",
  "ExpiresMinutes": 60
}
```

La aplicacion valida en el arranque que `Key`, `Issuer` y `Audience` existan. Para entornos locales puedes mover la clave a `dotnet user-secrets`:

```bash
dotnet user-secrets set "Jwt:Key" "<tu-clave-super-secreta>"
```

### Base de datos
Selecciona el proveedor mediante `DatabaseProvider` (`MySql`, `SqlServer`, `PostgreSql`, `Oracle`). La aplicacion usa `ConnectionStrings:DefaultConnection`, y si existe una cadena especifica por proveedor, esta tiene prioridad. Ejemplo:

```json
"DatabaseProvider": "MySql",
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=users_db;User Id=root;Password=secret;",
  "MySql": "Server=localhost;Port=3306;Database=users_db;User Id=root;Password=secret;"
}
```

La fabrica `DbConnectionFactory` construye la instancia de `DbConnection` adecuada (MySqlConnector, SqlClient, Npgsql u Oracle.ManagedDataAccess). El repositorio detecta automaticamente el prefijo de la tabla (`dbo`, `public`, etc.) y el formato de parametros.

### Variables de entorno utiles
Cualquier clave puede sobrescribirse con la notacion jerarquica habitual:

```powershell
$env:DatabaseProvider = "PostgreSql"
$env:ConnectionStrings__PostgreSql = "Host=localhost;Port=5432;Database=users_db;Username=postgres;Password=secret;"
$env:Jwt__Key = "clave_desde_entorno"
```

En Linux o macOS utiliza `export` en lugar de `set`.

## Estructura del proyecto
- `Domain`: entidades y contratos que definen el modelo (`User`).
- `Application`: DTOs, servicios y validadores. Contiene `AuthService` y `PasswordService`.
- `Infrastructure`: capa de datos, seguridad y adaptadores externos (repositorios, factories, JWT, filtros de Swagger).
- `Web`: configuracion de ASP.NET Core, controladores y pipeline HTTP (solo `AuthController` por defecto).

## Flujo de autenticacion
1. `AuthController` valida la entrada (`LoginRequestValidator`).
2. `AuthService` consulta al repositorio con el usuario y compara el hash con `BCrypt`.
3. `TokenService` arma el JWT usando los claims configurados y la clave simetrica.
4. La respuesta expone el token y el tiempo de expiracion.

El endpoint `POST /api/auth/password` reutiliza `PasswordService` para crear una contrasena aleatoria, actualizar el hash y devolver la contrasena en texto claro al cliente.

## Endpoints

| Metodo | Ruta                 | Descripcion                                               |
| ------ | -------------------- | --------------------------------------------------------- |
| POST   | `/api/auth/login`    | Autentica a un usuario y devuelve el token JWT.           |
| POST   | `/api/auth/password` | Regenera la contrasena del usuario y retorna la nueva.    |

### Ejemplos de payload

```json
POST /api/auth/login
{
  "username": "demo.user",
  "password": "P@ssw0rd!"
}
```

```json
POST /api/auth/password
{
  "userId": 1,
  "length": 12
}
```

## Esquema esperado de la tabla `users`
El repositorio trabaja con una tabla `users` cuyos campos principales encajan con `Domain/Entities/User.cs`: `id`, `role_id`, `name`, `first_name`, `email`, `password`, `degree_id`, `remember_token`, `phone`, `cip`. Adapta los nombres o los `SELECT` si tu base usa un esquema distinto.
