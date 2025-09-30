# Tech Trend Emporium

**Autores:**
- Andrés Poveda
- Carlos Peña  
- Nicolás Quintana

---

## Descripción

Plataforma e-commerce donde la tecnología se encuentra con la moda. Integra FakeStore API para poblar productos/categorías y ofrece autenticación dinámica, catálogo con filtros, reseñas, wishlist, carrito con cupones y un flujo CI/CD con estrategia Trunk-Based (todo a main via PR + aprobaciones).

## Arquitectura

**Stack Tecnológico:**
- **.NET 8** - Backend APIs y servicios
- **Entity Framework Core** - ORM con MySQL
- **Docker** - Containerización
- **AWS ECS Fargate** - Deployment
- **GitHub Actions** - CI/CD

**Características principales:**
- Microservicios con separación lógica de esquemas
- Autenticación JWT con sesiones
- Sistema de aprobaciones para productos/categorías
- Integración con FakeStore API para seeding
- Carrito de compras con sistema de cupones
- Reseñas y wishlist de productos

## Estructura del Proyecto

```
├── src/TechTrendEmporium.Api/     # API principal
├── Data/                          # Entidades y DbContext
├── Logica/                        # Servicios y repositorios
├── docs/                          # Documentación del proyecto
└── Dockerfile                     # Containerización
```

## Inicio Rápido

### Prerequisitos
- .NET 8 SDK
- Docker Desktop
- SQL Server/MySQL (local o remoto)

### Configuración Local

1. **Clonar el repositorio:**
   ```bash
   git clone <repository-url>
   cd TechTrend-Emporium.Backend
   ```

2. **Configurar la cadena de conexión:**
   ```bash
   # En appsettings.Development.json o variables de entorno
   "ConnectionStrings": {
     "DefaultConnection": "tu-connection-string-aqui"
   }
   ```

3. **Ejecutar migraciones:**
   ```bash
   dotnet ef database update --project Data
   ```

4. **Ejecutar la aplicación:**
   ```bash
   dotnet run --project src/TechTrendEmporium.Api
   ```

5. **Acceder a Swagger UI:**
   - Desarrollo: `https://localhost:7089/swagger`
   - Health check: `https://localhost:7089/health`

### Docker

```bash
# Construir imagen
docker build -t techtrend-emporium .

# Ejecutar contenedor
docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="tu-connection-string" techtrend-emporium
```

## Documentación

La documentación completa del proyecto se encuentra en la carpeta [`docs/`](./docs/):

### 📋 Documentación Principal
- **[Arquitectura](./docs/ARQUITECTURE.md)** - Diseño del sistema, componentes y modelo de datos
- **[Estrategia de Ramas](./docs/BRANCH.md)** - Trunk-Based Development y workflow de PRs
- **[CI/CD](./docs/CI-CD.md)** - Pipelines de integración y despliegue continuo

### 📁 Documentación Especializada
- **[Decisiones de Arquitectura (ADRs)](./docs/decisions/)** - Registro de decisiones técnicas importantes
- **[Especificaciones de Features](./docs/features/)** - Documentación detallada de funcionalidades

### 🎯 Enlaces Rápidos
- [Sistema Overview](./docs/ARQUITECTURE.md#system-overview) - Diagrama general del sistema
- [Modelo de Datos](./docs/ARQUITECTURE.md#data-model-logical) - Entidades y relaciones
- [Workflow de Desarrollo](./docs/BRANCH.md#pull-request-workflow) - Proceso de contribución
- [Pipelines CI/CD](./docs/CI-CD.md#continuous-integration-on-feature-bugfix-hotfix-branches-f02) - Detalles de automatización

## Desarrollo

### Estrategia de Ramas

Este proyecto sigue **Trunk-Based Development**:

- ✅ Rama principal: `main` (protegida)
- ✅ Ramas de trabajo: `feature/*`, `bugfix/*`, `hotfix/*`
- ✅ Todo cambio via Pull Request con 2+ aprobaciones
- ✅ CI automático en cada push
- ✅ CD automático en merge a `main`

Ver detalles completos en [BRANCH.md](./docs/BRANCH.md).

### Contribuir

1. **Crear rama desde main:**
   ```bash
   git checkout main && git pull origin main
   git checkout -b feature/mi-nueva-funcionalidad
   ```

2. **Desarrollar y commitear cambios**

3. **Abrir Pull Request hacia `main`:**
   - Incluir descripción clara del cambio
   - Referenciar issues relacionados
   - Actualizar documentación si es necesario

4. **Review process:** Requiere 2+ aprobaciones y checks en verde

Ver proceso completo en [docs/README.md](./docs/README.md#contributions).

## API Endpoints

### Principales Endpoints
- `GET /health` - Health check
- `POST /api/users` - Crear usuario
- `GET /api/users` - Listar usuarios
- `GET /api/users/{id}` - Obtener usuario por ID

> 📖 **Documentación completa:** Ver Swagger UI en desarrollo o revisar [ARQUITECTURE.md](./docs/ARQUITECTURE.md#services--repos) para detalles de servicios.

## Base de Datos

### Entidades Principales
- **User** - Usuarios del sistema (Admin, Employee, Shopper)
- **Product** - Productos con sistema de aprobaciones
- **Category** - Categorías con estados de aprobación
- **Cart/CartItem** - Carrito de compras con cupones
- **Review** - Reseñas de productos
- **Wishlist** - Lista de deseos

### Migraciones
```bash
# Agregar nueva migración
dotnet ef migrations add NombreMigracion --project Data

# Aplicar migraciones
dotnet ef database update --project Data
```

Ver modelo completo en [ARQUITECTURE.md](./docs/ARQUITECTURE.md#data-model-logical).

## Deployment

### CI/CD Pipeline

El proyecto usa **GitHub Actions** para automatización:

1. **CI en ramas de feature:** Build → Test → Docker Image → Push to GHCR
2. **CI en PRs:** Validación antes de merge
3. **CD en main:** Deploy automático a AWS ECS Fargate

Cada imagen Docker se etiqueta con el **commit SHA** para trazabilidad completa.

Ver detalles en [CI-CD.md](./docs/CI-CD.md).

### Ambientes

- **Desarrollo:** Local con Swagger habilitado
- **Producción:** AWS ECS Fargate con health checks

## Arquitectura de Decisiones

Las decisiones técnicas importantes se documentan como ADRs:

- [**Containerización y Database Choice**](./docs/decisions/ADR.md) - Por qué Docker + MySQL + commit SHA tagging

## Soporte

- **Issues:** Usar GitHub Issues para reportar bugs o solicitar features
- **Documentación:** Revisar la carpeta [`docs/`](./docs/) para información detallada
- **Contribuciones:** Seguir el [workflow de PRs](./docs/BRANCH.md#pull-request-workflow)

---

> 📚 **Más información:** Revisar [`docs/README.md`](./docs/README.md) para el índice completo de documentación del proyecto.
