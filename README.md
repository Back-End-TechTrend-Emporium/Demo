# TechTrend Emporium

Plataforma e-commerce donde la tecnología se encuentra con la moda. Integra FakeStore API para poblar productos/categorías y ofrece autenticación dinámica, catálogo con filtros, reseñas, wishlist, carrito con cupones y un flujo CI/CD con estrategia Trunk-Based (todo a main via PR + aprobaciones).

## 🚀 Características Principales

### 🔐 Autenticación y Autorización
- **JWT Authentication** con roles dinámicos (SuperAdmin, Admin, Employee, Shopper)
- **ASP.NET Core Identity** para manejo de usuarios
- **Session tracking** para auditoría de usuarios
- **Políticas de autorización** basadas en roles

### 🛍️ Funcionalidades E-commerce
- **Catálogo de productos** con filtros avanzados (precio, categoría, búsqueda)
- **Carrito de compras** con cálculo de totales y descuentos
- **Sistema de cupones** con validaciones de fecha y monto mínimo
- **Wishlist** para productos favoritos
- **Sistema de reseñas** con aprobación de administradores
- **Integración FakeStore API** para importación automática de productos

### 🏗️ Arquitectura y Tecnología
- **Clean Architecture** con separación de capas (Core, Infrastructure, API, Tests)
- **Repository Pattern** con Unit of Work
- **Entity Framework Core** con soporte SQLServer e InMemory
- **Swagger/OpenAPI** para documentación automática
- **Docker** para containerización
- **CI/CD Pipeline** con GitHub Actions

## 📋 Requisitos del Sistema

- .NET 8.0 SDK
- SQL Server (opcional, usa InMemory por defecto en desarrollo)
- Docker (opcional, para containerización)

## 🔧 Instalación y Configuración

### 1. Clonar el Repositorio
```bash
git clone https://github.com/Back-End-TechTrend-Emporium/Demo.git
cd Demo
```

### 2. Restaurar Dependencias
```bash
dotnet restore
```

### 3. Ejecutar la Aplicación
```bash
cd TechTrendEmporium.API
dotnet run
```

La aplicación estará disponible en:
- **Swagger UI**: http://localhost:5172
- **API Base**: http://localhost:5172/api

### 4. Usuario Administrador por Defecto
- **Email**: admin@techtrendemporium.com
- **Password**: Admin123!
- **Rol**: SuperAdmin

## 🧪 Ejecutar Tests

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar tests con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## 🐳 Docker

### Construir Imagen
```bash
docker build -f TechTrendEmporium.API/Dockerfile -t techtrendemporium:latest .
```

### Ejecutar Container
```bash
docker run -p 8080:8080 techtrendemporium:latest
```

## 📚 Documentación API

### Swagger/OpenAPI
Accede a la documentación interactiva en: http://localhost:5172

### Postman Collection
Importa la colección `TechTrendEmporium.postman_collection.json` en Postman para probar todos los endpoints.

## 🔗 Endpoints Principales

### Autenticación
- `POST /api/auth/register` - Registro de usuarios
- `POST /api/auth/login` - Inicio de sesión
- `GET /api/auth/me` - Información del usuario actual
- `POST /api/auth/logout` - Cerrar sesión

### Productos
- `GET /api/products` - Listar productos con filtros
- `GET /api/products/{id}` - Obtener producto por ID
- `POST /api/products` - Crear producto (Employee+)
- `PUT /api/products/{id}` - Actualizar producto (Employee+)
- `DELETE /api/products/{id}` - Eliminar producto (Admin+)
- `POST /api/products/sync-fakestore` - Sincronizar con FakeStore API (Admin+)

### Categorías
- `GET /api/categories` - Listar categorías
- `GET /api/categories/{id}` - Obtener categoría por ID
- `POST /api/categories` - Crear categoría (Employee+)
- `PUT /api/categories/{id}` - Actualizar categoría (Employee+)
- `DELETE /api/categories/{id}` - Eliminar categoría (Admin+)

### Carrito
- `GET /api/cart` - Obtener carrito del usuario
- `POST /api/cart/items` - Agregar producto al carrito
- `PUT /api/cart/items/{productId}` - Actualizar cantidad
- `DELETE /api/cart/items/{productId}` - Remover producto
- `DELETE /api/cart` - Vaciar carrito
- `POST /api/cart/calculate-total` - Calcular total con cupón

## 🎭 Roles y Permisos

### SuperAdmin
- Acceso completo a todas las funcionalidades
- Gestión de usuarios y roles
- Configuración del sistema

### Admin
- Gestión de productos y categorías
- Revisión y aprobación de reseñas
- Gestión de cupones
- Reportes y analytics

### Employee
- CRUD de productos y categorías
- Gestión de inventario
- Atención al cliente

### Shopper (Usuario estándar)
- Navegación del catálogo
- Gestión de carrito y wishlist
- Realizar pedidos
- Escribir reseñas

## 🛠️ Estructura del Proyecto

```
TechTrendEmporium/
├── TechTrendEmporium.API/          # Capa de presentación (Web API)
│   ├── Controllers/                # Controladores REST
│   ├── Program.cs                  # Configuración de la aplicación
│   └── Dockerfile                  # Configuración Docker
├── TechTrendEmporium.Core/         # Capa de dominio
│   ├── Entities/                   # Entidades del dominio
│   ├── Interfaces/                 # Contratos e interfaces
│   └── DTOs/                       # Objetos de transferencia de datos
├── TechTrendEmporium.Infrastructure/ # Capa de infraestructura
│   ├── Data/                       # DbContext y configuraciones EF
│   ├── Repositories/               # Implementación de repositorios
│   └── Services/                   # Servicios externos (FakeStore API)
├── TechTrendEmporium.Tests/        # Pruebas unitarias e integración
│   ├── Controllers/                # Tests de controladores
│   ├── Repositories/               # Tests de repositorios
│   ├── Services/                   # Tests de servicios
│   └── Integration/                # Tests de integración
└── .github/workflows/              # Pipelines CI/CD
```

## 🚀 CI/CD Pipeline

El proyecto implementa una estrategia **Trunk-Based Development** con:

### Flujo de Trabajo
1. **Feature branches** → Pull Request → **main**
2. **Revisión de código** obligatoria
3. **Tests automatizados** en cada PR
4. **Build y deploy** automático en main

### Pipelines
- **CI**: Tests, build, y análisis de código en cada PR
- **CD**: Deploy automático a staging/production desde main
- **Versionado**: Tags automáticos basados en commits
- **Docker**: Build y push automático de imágenes

## 🔄 Integración FakeStore API

El sistema se integra con [FakeStore API](https://fakestoreapi.com/) para:

- **Importar productos** automáticamente
- **Sincronizar categorías** 
- **Mantener catálogo actualizado**
- **Demo data** para desarrollo y testing

### Sincronización
```bash
# Como administrador, puedes sincronizar datos:
POST /api/products/sync-fakestore
POST /api/categories/sync-fakestore
```

## 🧪 Testing Strategy

### Cobertura de Tests
- **Target**: 80% de cobertura de código
- **Unit Tests**: Lógica de negocio y repositorios
- **Integration Tests**: APIs y flujos completos
- **Controller Tests**: Endpoints y autorización

### Tecnologías de Testing
- **xUnit** para framework de testing
- **Moq** para mocking
- **Microsoft.AspNetCore.Mvc.Testing** para integration tests
- **InMemory Database** para tests aislados

## 📈 Funcionalidades Avanzadas

### Sistema de Cupones
```json
{
  "code": "WELCOME10",
  "discountPercentage": 10,
  "maxDiscountAmount": 50,
  "minimumOrderAmount": 100,
  "validFrom": "2024-01-01",
  "validTo": "2024-12-31",
  "usageLimit": 1000
}
```

### Filtros de Productos
- **Por categoría**: `?categoryId=1`
- **Por precio**: `?minPrice=10&maxPrice=100`
- **Búsqueda**: `?search=smartphone`
- **Paginación**: `?page=1&pageSize=10`

### Session Management
- **JWT tokens** con expiración configurable
- **Refresh tokens** para renovación automática
- **Session tracking** para auditoría
- **Logout** con invalidación de tokens

## 🔒 Seguridad

### Autenticación JWT
- **Tokens firmados** con clave secreta
- **Claims personalizados** (roles, usuario info)
- **Expiración configurable**
- **Refresh token** para renovación

### Autorización por Roles
```csharp
[Authorize(Policy = "AdminOnly")]
[Authorize(Policy = "EmployeeOnly")]
[Authorize(Policy = "SuperAdminOnly")]
```

### Validación de Datos
- **Data Annotations** en DTOs
- **Fluent Validation** para reglas complejas
- **Sanitización** de inputs
- **Rate limiting** para APIs

## 📄 Licencia

Este proyecto está bajo la licencia MIT. Ver archivo `LICENSE` para más detalles.

## 👥 Contribución

1. Fork el proyecto
2. Crea una rama feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📞 Soporte

Para soporte y preguntas:
- **Issues**: GitHub Issues para bugs y feature requests
- **Discussions**: GitHub Discussions para preguntas generales
- **Email**: support@techtrendemporium.com

---

**TechTrend Emporium** - Donde la tecnología se encuentra con la moda 🛍️✨
