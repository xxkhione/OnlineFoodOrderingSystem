# Online Food Ordering System
An Online Food Ordering System built as a microservices-based SEN300 project. The solution demonstrates service decomposition, API gateway routing, and basic service discovery for a simple food ordering domain.

## Features
- **API gateway**: Central Ocelot-based gateway that fronts all backend services and handles routing, aggregation, and cross-cutting concerns.

- **Service decomposition**: Separate services for catalog, basket, orders, authentication/customer, and messaging, each with its own Dockerfile and runtime stack.

 - **Containerization**: All services are containerized and orchestrated via docker-compose.yml at the solution root.

## Architecture
This project follows a microservices architecture with a gateway pattern.

### Services
- `catalogservice` (Python) – Manages menu items and exposes REST endpoints for querying and managing the food catalog.

- `basketservice` (Python) – Handles user baskets/carts and temporary order state before checkout.

- `orderservice` (.NET) – Persists and manages completed orders, coordinating with other services as needed.

- `authservice` & `customerservice` (.NET) – Authentication and basic customer profile handling (sign-up, login, identity).

- `messageservice` (.NET) – Handles out-of-band notifications or messaging functionality (for example, order confirmation messages).

- `ocelotgateway` (.NET) – API gateway exposing a single entry point for clients and routing requests to downstream services using ocelot.json.

## Technology Stack
- Python (Flask/FastAPI-style service) for `catalogservice` & `basketservice`.

- .NET 10 for `ocelotgateway` and other C#-based services.

- Infrastructure

  - Docker & Docker Compose for containerization and local orchestration.

  - Eureka-style service registration.

## Project Structure
```
OnlineFoodOrderingSystem/
├─ authservice_customerservice/
├─ basketservice/
├─ catalogservice/
│  ├─ app.py
│  ├─ eureka_registration.py
│  ├─ menu_item.py
│  ├─ menu_item_repository.py
│  ├─ Dockerfile
│  └─ requirements.txt
├─ messageservice/
├─ orderservice/
├─ ocelotgateway/
│  ├─ Program.cs
│  ├─ ocelot.json
│  ├─ appsettings.json
│  └─ Dockerfile
├─ docker-compose.yml
└─ README.md
```
## Getting Started
These instructions help you run the system locally using Docker.

### Prerequisites
- Docker and Docker Compose installed and running.

- .NET SDK 6+ (only required if you want to run or develop services outside Docker).

- Python 3.10+ (only required if you want to run or develop services outside Docker).

### Clone the repository
```bash
git clone https://github.com/xxkhione/OnlineFoodOrderingSystem.git
cd OnlineFoodOrderingSystem
```
### Running with Docker Compose (Recommended)
From the repository root:
```bash
docker compose up --build
```
- This command builds and starts all services defined in the root `docker-compose.yml` file.

- Once all containers are healthy, you can access the API gateway and individual services at the ports defined in the compose and service configs (see the Configuration section).

To stop everything:

```bash
docker compose down
```

## Configuration
### API Gateway (Ocelot)
The gateway routing is defined in `ocelotgateway/ocelot.json`.

Common configuration elements:

- Downstream service URLs and ports for `catalogservice`, `basketservice`, `orderservice`, etc.

- Upstream paths exposed to clients (for example, `/catlogservice/api`, `/basketservice/api`).

To change routes:

1. Edit `ocelotgateway/ocelot.json`.

2. Restart the `ocelotgateway` service or re-run Docker Compose.

### Docker and Compose
- Each service has its own `Dockerfile` describing how to build that container.

- The root `docker-compose.yml` wires the services together, including networks, ports, and environment variables.

To adjust ports or environment variables, edit the root `docker-compose.yml` and rebuild.

## Development Workflow
A typical development loop:

1. Make code changes in the service of interest.

2. Run that service locally (outside Docker) on a different port, or rebuild the relevant container.

3. Update `ocelotgateway/ocelot.json` to point to your local instance if you want to test via the gateway.

4. Use tools like Postman or curl to hit gateway routes and verify behavior.

For Python services, ensure dependencies are updated in `requirements.txt` and re-run `pip install -r requirements.txt` as needed.
For .NET services, update the `.csproj` file and run `dotnet restore` when new packages are added.

## Testing
At a minimum, you can perform manual API testing via HTTP requests:

- Catalog endpoints (list menu items, get by id, create/update/delete).

- Basket operations (add item, remove item, view basket).

- Order submission (create order from a basket).

- Authentication endpoints (register/login) in `authservice_customerservice`.

If you add automated tests:

- Place Python tests under something like `catalogservice/tests`.

Place .NET tests in a separate test project referencing the services.

You can then wire test runs into a CI workflow using GitHub Actions for .NET or Python.

## Extending the System
Ideas for extending this project:

- Add a frontend client (React/Angular/Blazor) consuming the gateway APIs.

- Introduce a persistent database layer (PostgreSQL, MongoDB, etc.) for catalog, orders, and baskets.

- Implement proper authentication and authorization (JWT, OAuth2) in the gateway and services.

- Add observability tools (logging, metrics, tracing) to each service and the gateway.

These changes would further demonstrate microservice patterns and production-readiness in a real-world online food ordering scenario.
