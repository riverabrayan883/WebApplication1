Imports System.ComponentModel.DataAnnotations

Public Class Empleado
    Implements IValidatableObject ' 👉 ¡CRUCIAL para proyectos grandes!

    Public Property id As Integer

    <Required(ErrorMessage:="El nombre es obligatorio")>
    <StringLength(50, MinimumLength:=2, ErrorMessage:="El nombre debe tener entre 2 y 50 caracteres")>
    Public Property nombre As String

    <Required(ErrorMessage:="El apellido es obligatorio")>
    <StringLength(50, MinimumLength:=2, ErrorMessage:="El apellido debe tener entre 2 y 50 caracteres")>
    Public Property apellido As String

    <Range(18, 100, ErrorMessage:="La edad debe estar entre 18 y 100")>
    Public Property edad As Integer?

    <StringLength(50, ErrorMessage:="El departamento no puede exceder 50 caracteres")>
    Public Property departamento As String

    <Range(0, 1000000, ErrorMessage:="El salario debe ser positivo y menor a 1M")>
    Public Property salario As Decimal?

    <Phone(ErrorMessage:="Teléfono no válido")>
    <StringLength(20, ErrorMessage:="El teléfono no puede exceder 20 caracteres")>
    Public Property telefono As String

    <DataType(DataType.Date, ErrorMessage:="Fecha no válida")>
    Public Property fecha_contratacion As DateTime?

    ' 👉 VALIDACIÓN CRUZADA (ejemplo profesional)
    Public Function Validate(validationContext As ValidationContext) As IEnumerable(Of ValidationResult) Implements IValidatableObject.Validate
        Dim results As New List(Of ValidationResult)()

        ' 1. Fecha de contratación no puede ser futura
        If fecha_contratacion.HasValue AndAlso fecha_contratacion > DateTime.Now Then
            results.Add(New ValidationResult(
                "La fecha de contratación no puede ser futura",
                New String() {"fecha_contratacion"} ' 👉 Enlaza al campo específico
            ))
        End If

        ' 2. Salario razonable para la edad (regla de negocio)
        If edad.HasValue AndAlso salario.HasValue Then
            If edad < 25 AndAlso salario > 3000 Then
                results.Add(New ValidationResult(
                    $"Salario demasiado alto para edad {edad} (máx recomendado: {edad * 100})",
                    New String() {"salario"}
                ))
            End If
        End If

        ' 3. Departamento obligatorio para salarios altos
        If salario.HasValue AndAlso salario > 5000 AndAlso String.IsNullOrEmpty(departamento) Then
            results.Add(New ValidationResult(
                "Departamento obligatorio para salarios superiores a $5,000",
                New String() {"departamento"}
            ))
        End If

        Return results
    End Function
End Class