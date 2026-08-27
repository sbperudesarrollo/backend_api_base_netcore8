# Backend API Base (.NET 10)

Plantilla base para construir APIs REST sobre .NET 10 con autenticacion JWT, validaciones con FluentValidation y un repositorio ADO.NET compatible con MySQL, SQL Server, PostgreSQL u Oracle. El objetivo es ofrecer un punto de partida neutro que resuelva autenticacion y gestion basica de usuarios sin imponer reglas de negocio especificas.

## Caracteristicas clave

- Arquitectura por capas (`Domain`, `Application`, `Infrastructure`, `Web`) con responsabilidades bien separadas.
- Autenticacion con JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`) y generacion de tokens a traves de `TokenService`.
- Validaciones desacopladas con FluentValidation registradas via `AddValidatorsFromAssemblyContaining`.
- Repositorio de usuarios basado en ADO.NET con fabrica de conexiones y SQL adaptado por proveedor.
- Servicio de contrasenas que genera credenciales robustas y almacena hashes con `BCrypt.Net`.
- Documentacion y pruebas manuales via Swagger con esquema de seguridad Bearer preconfigurado.

## Requisitos previos

- .NET SDK 10.0 o superior.
- Un motor de base de datos compatible (MySQL, SQL Server, PostgreSQL u Oracle).
- Herramienta cliente para bases de datos y cualquier IDE/editor que prefieras.

## Puesta en marcha rapida

```bash
dotnet restore
dotnet build
dotnet run --project backend_api_base_netcore8
```

Segun [launchSettings.json](/D:/SOFTBRILLIANCE/MIGRACION/proyecto-nuevo/backend_api_base_netcore8/Properties/launchSettings.json), la API se publica en:

- `http://localhost:5079`
- `https://localhost:7261`

Swagger UI queda disponible en `/swagger`.

## Configuracion

La configuracion principal esta en [appsettings.json](/D:/SOFTBRILLIANCE/MIGRACION/proyecto-nuevo/backend_api_base_netcore8/appsettings.json). Para desarrollo puedes sobreescribirla con `appsettings.Development.json`, variables de entorno o `user-secrets`.

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

Selecciona el proveedor mediante `DatabaseProvider` (`MySql`, `SqlServer`, `PostgreSql`, `Oracle`). La aplicacion registra el repositorio correcto en `Program.cs` segun ese valor y usa una cadena por proveedor en `ConnectionStrings`.

```json
"DatabaseProvider": "SqlServer",
"ConnectionStrings": {
  "SqlServer": "Server=YOUR_SQL_HOST;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;",
  "MySql": "Server=YOUR_MYSQL_HOST;Port=3306;Database=YOUR_DATABASE;User ID=YOUR_USER;Password=YOUR_PASSWORD;",
  "PostgreSql": "Host=YOUR_PG_HOST;Port=5432;Database=YOUR_DATABASE;Username=YOUR_USER;Password=YOUR_PASSWORD;",
  "Oracle": "User Id=YOUR_USER;Password=YOUR_PASSWORD;Data Source=YOUR_TNS_ALIAS"
}
```

Si quieres cambiar de motor, solo modifica `DatabaseProvider` y completa la connection string correspondiente.

### CORS

Los origenes permitidos salen de `Cors:AllowedOrigins`:

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:4200",
    "https://tu-frontend.com"
  ]
}
```

La API aplica esa lista al policy `FrontendCors`. Si la lista esta vacia, el codigo actual usa `http://localhost:4200` como fallback.

### Variables de entorno utiles

Cualquier clave puede sobrescribirse con la notacion jerarquica habitual:

```powershell
$env:DatabaseProvider = "PostgreSql"
$env:ConnectionStrings__PostgreSql = "Host=localhost;Port=5432;Database=users_db;Username=postgres;Password=secret;"
$env:Cors__AllowedOrigins__0 = "http://localhost:4200"
$env:Cors__AllowedOrigins__1 = "https://tu-frontend.com"
$env:Jwt__Key = "clave_desde_entorno"
```

En Linux o macOS utiliza `export` en lugar de `set`.

## Estructura del proyecto

- `Domain`: entidades y contratos que definen el modelo (`User`).
- `Application`: DTOs, servicios y validadores. Contiene `AuthService` y `PasswordService`.
- `Infrastructure`: capa de datos, seguridad y adaptadores externos (repositorios, factories, JWT, filtros de Swagger).
- `Web`: configuracion de ASP.NET Core, controladores y pipeline HTTP.

## Flujo de autenticacion

1. `AuthController` valida la entrada (`LoginRequestValidator`).
2. `AuthService` consulta al repositorio con el usuario y compara el hash con `BCrypt`.
3. `TokenService` arma el JWT usando los claims configurados y la clave simetrica.
4. La respuesta expone el token y el tiempo de expiracion.

El endpoint `POST /api/auth/password` reutiliza `PasswordService` para crear una contrasena aleatoria, actualizar el hash y devolver la contrasena en texto claro al cliente.

## Endpoints

| Metodo | Ruta                     | Descripcion                                                     |
| ------ | ------------------------ | --------------------------------------------------------------- |
| POST   | `/api/auth/authenticate` | Autentica a un usuario y devuelve el token JWT.                 |
| POST   | `/api/auth/google`       | Autentica con Google y devuelve el token JWT.                   |
| POST   | `/api/auth/password`     | Regenera la contrasena del usuario y retorna la nueva.          |
| GET    | `/api/users`             | Lista usuarios paginados. Requiere Bearer token.                |
| GET    | `/api/users/{id}`        | Obtiene un usuario por id. Requiere Bearer token.               |
| GET    | `/api/users/roles`       | Lista roles disponibles. Requiere Bearer token.                 |
| POST   | `/api/users`             | Crea un usuario. Requiere Bearer token.                         |
| PUT    | `/api/users`             | Actualiza un usuario. Requiere Bearer token.                    |
| DELETE | `/api/users/{id}`        | Elimina un usuario. Requiere Bearer token.                      |

### Ejemplos de payload

```json
POST /api/auth/authenticate
{
  "email": "demo.user@acme.com",
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

### Ejemplo de lista paginada

`GET /api/users?page=2&perPage=20`

```json
{
  "success": true,
  "data": [
    {
      "id": 21,
      "username": "juan",
      "firstName": "Juan",
      "lastName": "Perez",
      "email": "juan@acme.com",
      "roleId": 2,
      "role": "Administrador",
      "phone": 999111222
    }
  ],
  "meta": {
    "page": 2,
    "perPage": 20,
    "total": 142,
    "totalPages": 8
  }
}
```

## Swagger y OpenAPI

La API expone Swagger/OpenAPI en runtime:

- UI: `/swagger`
- JSON: `/swagger/v1/swagger.json`

Tambien se versiona un archivo YAML generado:

- [openapi.yaml](/D:/SOFTBRILLIANCE/MIGRACION/proyecto-nuevo/backend_api_base_netcore8/openapi.yaml)

Ese archivo refleja el contrato actual de la API y puede usarse para integraciones, importacion en herramientas o revision fuera de runtime.

## Esquema esperado de la tabla `users`

El repositorio trabaja con una tabla `users` cuyos campos principales encajan con `Domain/Entities/User.cs`: `id`, `role_id`, `name`, `first_name`, `email`, `password`, `degree_id`, `remember_token`, `phone`, `cip`. Adapta los nombres o los `SELECT` si tu base usa un esquema distinto.
