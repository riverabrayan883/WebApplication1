' Archivo: App_Start/Startup.vb

Imports System
Imports System.Configuration
Imports System.IdentityModel.Tokens.Jwt
Imports System.Security.Claims
Imports System.Text
Imports System.Web.Http
Imports Microsoft.IdentityModel.Tokens
Imports Microsoft.Owin
Imports Microsoft.Owin.Security
Imports Microsoft.Owin.Security.Jwt
Imports Owin
Imports WebApiThrottle

<Assembly: OwinStartup(GetType(Startup))>
Public Class Startup
    Public Sub Configuration(app As IAppBuilder)
        ConfigureJwtAuth(app)
        ' 👉 Configuración de Web API
        Dim config As New HttpConfiguration()
        WebApiConfig.Register(config)

        ' 👉 Configuración de Rate Limiting
        ConfigureRateLimiting(config)

        app.UseWebApi(config)
    End Sub

    Private Sub ConfigureJwtAuth(app As IAppBuilder)
        Dim secretKey As String = ConfigurationManager.AppSettings("JwtSecret")
        Dim issuer As String = ConfigurationManager.AppSettings("JwtIssuer")
        Dim audience As String = ConfigurationManager.AppSettings("JwtAudience")

        ' 👉 Validación básica
        If String.IsNullOrEmpty(secretKey) OrElse String.IsNullOrEmpty(issuer) OrElse String.IsNullOrEmpty(audience) Then
            Throw New Exception("Configuración JWT incompleta en web.config")
        End If

        ' 👉 Configuración de validación
        Dim validationParameters As New TokenValidationParameters() With {
            .ValidateIssuerSigningKey = True,
            .IssuerSigningKey = New SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            .ValidateIssuer = True,
            .ValidIssuer = issuer,
            .ValidateAudience = True,
            .ValidAudience = audience,
            .ClockSkew = TimeSpan.Zero,
            .RequireExpirationTime = True
        }

        ' 👉 CONFIGURACIÓN CORRECTA PARA .NET FRAMEWORK 4.7.2
        app.UseJwtBearerAuthentication(New JwtBearerAuthenticationOptions() With {
            .TokenValidationParameters = validationParameters,
            .AuthenticationMode = AuthenticationMode.Active
        })
    End Sub


    Private Sub ConfigureRateLimiting(config As HttpConfiguration)
        Dim policy As New ThrottlePolicy(perSecond:=1, perMinute:=20, perHour:=200, perDay:=1500) With {
            .IpThrottling = True,
            .ClientThrottling = True,
            .EndpointThrottling = True
        }

        policy.EndpointRules.Add("GET api/empleados", New RateLimits() With {.PerHour = 100})
        policy.EndpointRules.Add("POST api/empleados", New RateLimits() With {.PerHour = 50})
        policy.EndpointRules.Add("POST api/login", New RateLimits() With {.PerHour = 30})

        Dim repository As IThrottleRepository = New CacheRepository()

        Dim throttlingHandler As New ThrottlingHandler(
            policy,
            Nothing,
            repository,
            Nothing
        )

        config.MessageHandlers.Add(throttlingHandler)
    End Sub
End Class
