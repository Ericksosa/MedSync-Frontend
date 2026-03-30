# MedSync Frontend

Sistema de gestión médica desarrollado como aplicación web MVC, que actúa como cliente del backend REST de MedSync.

## Objetivo

MedSync es una plataforma integral de gestión hospitalaria que permite coordinar la atención médica entre pacientes, médicos y administradores. El sistema facilita la reserva de citas, el seguimiento de expedientes clínicos, la gestión de disponibilidad de médicos y el control de pagos, todo a través de una interfaz web moderna e intuitiva.

## Funcionalidades principales

- **Autenticación** basada en JWT con roles diferenciados (Paciente, Doctor, Administrador)
- **Portal del paciente**: reserva de citas en 3 pasos, historial de consultas y dashboard personalizado
- **Portal del médico**: agenda semanal (FullCalendar), cola de pacientes del día y acceso a expedientes clínicos
- **Portal del administrador**: gestión de hospitales, médicos, pacientes, citas, pagos y reportes de ingresos

## Tecnologías utilizadas

- **ASP.NET Core 9.0 MVC** — framework principal
- **Tailwind CSS** (CDN) — estilos y diseño responsivo
- **Lucide Icons** (CDN) — iconografía
- **FullCalendar v6** (CDN) — vista de agenda semanal
- **jQuery** — interacciones DOM y AJAX
- **JWT** almacenado en sesión de servidor

## Arquitectura

```
Browser → MVC Controller → ApiClient (HttpClient) → MedSync-API
                        ↓
                   Razor View (Tailwind CSS + FullCalendar)
```

El frontend es un cliente delgado (thin client): no tiene base de datos propia ni ORM. Toda la lógica de negocio y persistencia reside en el backend **MedSync-API**.

## Configuración y ejecución

### Requisitos previos

- .NET 9.0 SDK
- Backend MedSync-API corriendo en `http://localhost:5080`

### Pasos

```bash
# Clonar el repositorio
git clone https://github.com/Ericksosa/MedSync-Frontend.git
cd MedSync-Frontend

# Ejecutar
dotnet run

# O con hot reload
dotnet watch run
```

La aplicación queda disponible en `http://localhost:5121` y redirige a `/Auth/Login` por defecto.


### Configuración del backend

En `appsettings.json`, ajustar la URL base de la API si es necesario:

```json
{
  "ApiBaseUrl": "http://localhost:5080"
}
```

## Estructura del proyecto

```
MedSync-Frontend/
├── Controllers/          # AdminController, MedicoController, PacienteController, AuthController
├── Models/ViewModels/    # DTOs que reflejan los shapes del API
├── Services/
│   ├── ApiClient.cs      # Wrapper HTTP generico (GET, POST, PATCH, PUT, DELETE)
│   └── AuthTokenService.cs  # Lectura/escritura del JWT y claims en sesion
├── Views/
│   ├── Auth/             # Login (pagina independiente, sin layout)
│   ├── Admin/            # Dashboard, Hospitales, Medicos, Pacientes, Citas, Pagos, Reportes
│   ├── Medico/           # Dashboard, Agenda, Historial, Paciente (expediente)
│   ├── Paciente/         # Dashboard, ReservarCita, Historial
│   └── Shared/           # _Layout.cshtml (sidebar Tailwind con navegacion por rol)
└── wwwroot/              # Assets estaticos
```

## Elaborado por

| Estudiante | Matricula |
|---|---|
| Erick Daniel Sosa Rodriguez | A00115078 |
| Jerlyn Rodriguez | A00113235 |
| Eorys Pina | A00115249 |
