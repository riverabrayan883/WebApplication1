Imports System.IdentityModel.Tokens
Imports System.IdentityModel.Tokens.Jwt
Imports System.Security.Claims
Imports System.Text
Imports System.Web.Http
Imports Microsoft.IdentityModel.Tokens

<RoutePrefix("api/auth")>
Public Class AuthController
    Inherits ApiController

    ' Clase para recibir el login en JSON
    Public Class LoginRequest
        Public Property Username As String
        Public Property Password As String
    End Class

    <HttpPost>
    <Route("login")>
    Public Function Login(<FromBody> request As LoginRequest) As IHttpActionResult
        ' Simulación de validación (aquí validas contra tu BD)
        If request.Username = "admin" AndAlso request.Password = "123456" Then
            Dim token = GenerateJwtToken(request.Username)
            Return Ok(New With {
                Key .token = token
            })
        Else
            Return Unauthorized()
        End If
    End Function

    ' En tu AuthController.vb
    Public Shared Function GenerateJwtToken(username As String) As String
        Dim secretKey As String = ConfigurationManager.AppSettings("JwtSecret")
        Dim issuer As String = ConfigurationManager.AppSettings("JwtIssuer")
        Dim audience As String = ConfigurationManager.AppSettings("JwtAudience")
        Dim keyBytes As Byte() = Encoding.UTF8.GetBytes(secretKey)
        Dim tokenHandler As New JwtSecurityTokenHandler()

        Dim claims = New List(Of Claim) From {
        New Claim(ClaimTypes.Name, username),
        New Claim(ClaimTypes.Role, "Admin")
    }

        ' 👉 AGREGA Issuer y Audience
        Dim tokenDescriptor As New SecurityTokenDescriptor With {
        .Subject = New ClaimsIdentity(claims),
        .Issuer = issuer, ' ¡FALTABA ESTO!
        .Audience = audience, ' ¡FALTABA ESTO!
        .Expires = DateTime.UtcNow.AddDays(1),
        .SigningCredentials = New SigningCredentials(New SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    }

        Dim token = tokenHandler.CreateToken(tokenDescriptor)
        Return tokenHandler.WriteToken(token)
    End Function
End Class
