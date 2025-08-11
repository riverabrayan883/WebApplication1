Imports System.Data.SqlClient
Imports System.Net
Imports System.Net.Http
Imports System.Web.Http
Imports System.Web.Http.Description
Imports WebGrease

Public Class ValuesController
    Inherits ApiController

    ' GET api/values
    <Authorize>
    <HttpGet>
    <ResponseType(GetType(Empleado))>
    Public Function GetEmpleados() As IEnumerable(Of Empleado)
        Dim connectionString As String = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString
        Dim conexion As SqlConnection = Nothing
        Dim sqlCommand As SqlCommand = Nothing
        Dim rst As SqlDataReader = Nothing
        Dim listaEmpleados As New List(Of Empleado)()
        Try
            conexion = New SqlConnection(connectionString)
            conexion.Open()

            sqlCommand = New SqlCommand()
            sqlCommand.Connection = conexion
            sqlCommand.CommandType = CommandType.Text
            sqlCommand.CommandText = "SELECT * FROM empleados"

            rst = sqlCommand.ExecuteReader()
            If rst.HasRows Then
                While rst.Read()
                    Dim empleado As New Empleado()
                    empleado.id = Convert.ToInt32(rst("id"))
                    empleado.nombre = Convert.ToString(rst("nombre"))
                    empleado.apellido = Convert.ToString(rst("apellido"))
                    empleado.edad = If(rst("edad") Is DBNull.Value, Nothing, Convert.ToInt32(rst("edad")))
                    empleado.salario = If(rst("salario") Is DBNull.Value, Nothing, Convert.ToDecimal(rst("salario")))
                    empleado.fecha_contratacion = If(rst("fecha_contratacion") Is DBNull.Value, Nothing, Convert.ToDateTime(rst("fecha_contratacion")))
                    empleado.departamento = Convert.ToString(rst("departamento"))
                    empleado.telefono = Convert.ToString(rst("telefono"))
                    listaEmpleados.Add(empleado)
                End While
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine("Error: " & ex.Message)
            Throw New HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error interno"))
        Finally
            If rst IsNot Nothing Then
                Try : rst.Close() : Catch : End Try
            End If
            If conexion IsNot Nothing AndAlso conexion.State = ConnectionState.Open Then
                Try : conexion.Close() : Catch : End Try
            End If
        End Try
        Return listaEmpleados
    End Function


    ' GET api/values/5
    <Authorize>
    <HttpGet>
    <ResponseType(GetType(Empleado))>
    Public Function GetEmpleado(ByVal id As Integer) As IHttpActionResult
        Dim connectionString As String = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString
        Dim conexion As SqlConnection = Nothing
        Dim sqlCommand As SqlCommand = Nothing
        Dim rst As SqlDataReader = Nothing
        Dim empleado As Empleado = Nothing

        Try
            conexion = New SqlConnection(connectionString)
            conexion.Open()

            ' 👉 Consulta SEGURA con parámetros (evita SQL Injection)
            sqlCommand = New SqlCommand("SELECT * FROM empleados WHERE id = @id", conexion)
            sqlCommand.Parameters.AddWithValue("@id", id)
            rst = sqlCommand.ExecuteReader()

            If rst.HasRows Then
                rst.Read()
                empleado = New Empleado() With {
                .id = Convert.ToInt32(rst("id")),
                .nombre = Convert.ToString(rst("nombre")),
                .apellido = Convert.ToString(rst("apellido")),
                .edad = If(rst("edad") Is DBNull.Value, Nothing, Convert.ToInt32(rst("edad"))),
                .salario = If(rst("salario") Is DBNull.Value, Nothing, Convert.ToDecimal(rst("salario"))),
                .fecha_contratacion = If(rst("fecha_contratacion") Is DBNull.Value, Nothing, Convert.ToDateTime(rst("fecha_contratacion"))),
                .departamento = Convert.ToString(rst("departamento")),
                .telefono = Convert.ToString(rst("telefono"))
            }
            End If

            ' 👉 404 si no existe
            If empleado Is Nothing Then
                Return NotFound()
            End If

            Return Ok(empleado)

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine("Error: " & ex.Message)
            Return InternalServerError()
        Finally
            ' 👉 Cierre seguro de recursos
            If rst IsNot Nothing Then Try : rst.Close() : Catch : End Try
            If conexion IsNot Nothing AndAlso conexion.State = ConnectionState.Open Then Try : conexion.Close() : Catch : End Try
        End Try
    End Function

    Protected Function GetValidationErrorResponse() As IHttpActionResult
        ' 👉 Formato profesional RFC 7807 (usado por Microsoft, AWS, etc.)
        Dim errorResponse = New With {
        .type = "https://tu-api.com/errors/validation",
        .title = "Errores de validación",
        .status = HttpStatusCode.BadRequest,
        .detail = "Revise los errores en los campos específicos",
        .errors = ModelState _
            .Where(Function(kvp) kvp.Value.Errors.Any()) _
            .Select(Function(kvp) New With {
                .field = kvp.Key,
                .messages = kvp.Value.Errors.Select(Function(e) e.ErrorMessage)
            })
    }
        Return Content(HttpStatusCode.BadRequest, errorResponse)
    End Function
    ' POST api/values
    <Authorize>
    <HttpPost>
    <ResponseType(GetType(Empleado))>
    Public Function PostEmpleado(<FromBody()> ByVal nuevoEmpleado As Empleado) As IHttpActionResult
        ' 👉 VALIDACIÓN AUTOMÁTICA
        If Not ModelState.IsValid Then
            Return GetValidationErrorResponse()
        End If

        Dim connectionString As String = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString
        Dim nuevoId As Integer = 0

        Using conexion As New SqlConnection(connectionString)
            conexion.Open()
            Using transaccion As SqlTransaction = conexion.BeginTransaction()
                Try
                    ' 👉 Insert con parámetros
                    Dim query As String = "
                    INSERT INTO empleados (nombre, apellido, edad, departamento, salario, telefono, fecha_contratacion)
                    VALUES (@nombre, @apellido, @edad, @departamento, @salario, @telefono, @fecha_contratacion);
                    SELECT SCOPE_IDENTITY();"

                    Using sqlCommand As New SqlCommand(query, conexion, transaccion)
                        ' Parámetros
                        sqlCommand.Parameters.AddWithValue("@nombre", nuevoEmpleado.nombre)
                        sqlCommand.Parameters.AddWithValue("@apellido", nuevoEmpleado.apellido)
                        sqlCommand.Parameters.AddWithValue("@edad", If(nuevoEmpleado.edad.HasValue, CObj(nuevoEmpleado.edad), DBNull.Value))
                        sqlCommand.Parameters.AddWithValue("@departamento", If(String.IsNullOrEmpty(nuevoEmpleado.departamento), CObj(DBNull.Value), nuevoEmpleado.departamento))
                        sqlCommand.Parameters.AddWithValue("@salario", If(nuevoEmpleado.salario.HasValue, CObj(nuevoEmpleado.salario), DBNull.Value))
                        sqlCommand.Parameters.AddWithValue("@telefono", If(String.IsNullOrEmpty(nuevoEmpleado.telefono), CObj(DBNull.Value), nuevoEmpleado.telefono))
                        sqlCommand.Parameters.AddWithValue("@fecha_contratacion", If(nuevoEmpleado.fecha_contratacion.HasValue, CObj(nuevoEmpleado.fecha_contratacion), DBNull.Value))

                        ' 👉 Obtener ID generado
                        nuevoId = Convert.ToInt32(sqlCommand.ExecuteScalar())
                    End Using

                    transaccion.Commit()

                    ' 👉 ASIGNAR EL ID AL OBJETO
                    nuevoEmpleado.id = nuevoId

                    ' 👉 ¡USO CORRECTO DE CREATED! (sin errores de concatenación)
                    Dim ubicacion As Uri = New Uri(Request.RequestUri, nuevoId.ToString())
                    Return Created(ubicacion, nuevoEmpleado) ' 👈 201 Created + recurso + Location header

                Catch ex As SqlException When ex.Number = 2627 ' Violación de clave única
                    transaccion.Rollback()
                    ModelState.Clear() ' 👉 Limpia errores previos
                    ModelState.AddModelError("id", "Empleado ya existe")
                    Return GetValidationErrorResponse()
                Catch ex As Exception
                    transaccion.Rollback()
                    System.Diagnostics.Debug.WriteLine("Error: " & ex.Message)
                    Return InternalServerError()
                End Try
            End Using
        End Using
    End Function
    ' PUT api/values/5
    <Authorize>
    <HttpPut>
    <ResponseType(GetType(Empleado))>
    Public Function PutEmpleado(ByVal id As Integer, <FromBody()> ByVal empleadoActualizado As Empleado) As IHttpActionResult
        ' 👉 1. Validación de coherencia de ID
        If id <> empleadoActualizado.id Then
            ModelState.Clear()
            ModelState.AddModelError("id", "El ID en la URL no coincide con el del cuerpo")
            Return GetValidationErrorResponse()
        End If

        ' 👉 2. Verificar existencia
        If Not ExisteEmpleado(id) Then
            Return NotFound()
        End If

        ' 👉 3. VALIDACIÓN AUTOMÁTICA
        If Not ModelState.IsValid Then
            Return GetValidationErrorResponse()
        End If

        Dim connectionString As String = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString

        Using conexion As New SqlConnection(connectionString)
            conexion.Open()
            Using transaccion As SqlTransaction = conexion.BeginTransaction()
                Try
                    ' 👉 Actualización SEGURA
                    Dim query As String = "
                    UPDATE empleados SET 
                        nombre = @nombre, 
                        apellido = @apellido,
                        edad = @edad,
                        departamento = @departamento,
                        salario = @salario,
                        telefono = @telefono,
                        fecha_contratacion = @fecha_contratacion
                    WHERE id = @id"

                    Using sqlCommand As New SqlCommand(query, conexion, transaccion)
                        ' Parámetros
                        sqlCommand.Parameters.AddWithValue("@id", id)
                        sqlCommand.Parameters.AddWithValue("@nombre", empleadoActualizado.nombre)
                        sqlCommand.Parameters.AddWithValue("@apellido", empleadoActualizado.apellido)
                        sqlCommand.Parameters.AddWithValue("@edad", If(empleadoActualizado.edad.HasValue, CObj(empleadoActualizado.edad), DBNull.Value))
                        sqlCommand.Parameters.AddWithValue("@departamento", If(String.IsNullOrEmpty(empleadoActualizado.departamento), CObj(DBNull.Value), empleadoActualizado.departamento))
                        sqlCommand.Parameters.AddWithValue("@salario", If(empleadoActualizado.salario.HasValue, CObj(empleadoActualizado.salario), DBNull.Value))
                        sqlCommand.Parameters.AddWithValue("@telefono", If(String.IsNullOrEmpty(empleadoActualizado.telefono), CObj(DBNull.Value), empleadoActualizado.telefono))
                        sqlCommand.Parameters.AddWithValue("@fecha_contratacion", If(empleadoActualizado.fecha_contratacion.HasValue, CObj(empleadoActualizado.fecha_contratacion), DBNull.Value))

                        sqlCommand.ExecuteNonQuery()
                    End Using

                    transaccion.Commit()

                    ' 👉 DEVOLVER EL OBJETO ACTUALIZADO (200 OK)
                    Return Ok(empleadoActualizado)

                Catch ex As Exception
                    transaccion.Rollback()
                    System.Diagnostics.Debug.WriteLine("Error: " & ex.Message)
                    Return InternalServerError()
                End Try
            End Using
        End Using
    End Function
    ' 👉 Método auxiliar para verificar existencia
    Private Function ExisteEmpleado(ByVal id As Integer) As Boolean
        Dim connectionString As String = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString
        Using conexion As New SqlConnection(connectionString)
            conexion.Open()
            Dim cmd As New SqlCommand("SELECT COUNT(1) FROM empleados WHERE id = @id", conexion)
            cmd.Parameters.AddWithValue("@id", id)
            Return Convert.ToInt32(cmd.ExecuteScalar()) > 0
        End Using
    End Function

    ' DELETE api/values/5
    <Authorize>
    <HttpDelete>
    Public Function DeleteEmpleado(ByVal id As Integer) As IHttpActionResult
        Dim connectionString As String = ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString
        Dim conexion As SqlConnection = Nothing
        Dim sqlCommand As SqlCommand = Nothing
        Dim filasAfectadas As Integer = 0

        Try
            conexion = New SqlConnection(connectionString)
            conexion.Open()

            ' 👉 Primero verificar existencia
            Dim existeCmd As New SqlCommand("SELECT COUNT(1) FROM empleados WHERE id = @id", conexion)
            existeCmd.Parameters.AddWithValue("@id", id)
            If Convert.ToInt32(existeCmd.ExecuteScalar()) = 0 Then
                Return NotFound()
            End If

            ' 👉 Eliminación SEGURA
            sqlCommand = New SqlCommand("DELETE FROM empleados WHERE id = @id", conexion)
            sqlCommand.Parameters.AddWithValue("@id", id)
            filasAfectadas = sqlCommand.ExecuteNonQuery()

            ' 👉 Confirmar eliminación
            If filasAfectadas > 0 Then
                'Return Ok("Empleado eliminado correctamente")
                Return StatusCode(HttpStatusCode.NoContent) ' 👈 204 No Content                
            Else
                Return InternalServerError()
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine("Error: " & ex.Message)
            Return InternalServerError()
        Finally
            If conexion IsNot Nothing AndAlso conexion.State = ConnectionState.Open Then Try : conexion.Close() : Catch : End Try
        End Try
    End Function
End Class
