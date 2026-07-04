Public Class Form1

    Private Pu_values As List(Of Double)
    Private Mu_values As List(Of Double)
    Private x As Double, x_Values As List(Of Double), y_Values As List(Of Double)

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

        Pu_values = New List(Of Double)()
        Mu_values = New List(Of Double)()
        x_Values = New List(Of Double)()
        y_Values = New List(Of Double)()

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click, Button2.Click
        Dim B As Double, D As Double, d0 As Double, d1 As Double
        Dim Dia_Ties, Dia_Mainbar, No_minface, No_majface As Double
        Dim Fck, Fy, n, Ast_prov, P_Fck As Double
        Dim Pi = Math.PI

        B = TextBox1.Text
        D = TextBox2.Text
        d0 = TextBox3.Text
        Dia_Ties = TextBox4.Text
        Dia_Mainbar = TextBox5.Text
        No_minface = TextBox6.Text
        No_majface = TextBox7.Text
        Fck = TextBox8.Text
        Fy = TextBox9.Text

        n = (2 * (No_majface + No_minface)) - 4 'Total No.of bars
        Ast_prov = n * (Pi / 4) * (Dia_Mainbar ^ 2)
        d1 = d0 + Dia_Ties + Dia_Mainbar / 2  'Effective Cover
        P_Fck = (Ast_prov * 100 / (B * D * Fck))
        TextBox10.Text = Math.Round(P_Fck, 2)
        TextBox11.Text = Math.Round((d1 / D), 2)

        ' Step 1: Neutral axis depth (Xu) will vary from 0.01*D and 20*D

        Dim Initial_Value As Double, Final_Value As Double, Step_Value As Double

        Initial_Value = -0.05 * D
        Final_Value = 10 * D
        Step_Value = (Final_Value - Initial_Value) / 199


        ' Step 2: Strain in Concrete

        Dim Asi_Maj() As Double ' An Array for Asi of Rebar layers on major face
        Dim Yi_Maj() As Double ' An Array for Yi of Rebar layers on major face
        Dim Csi() As Double
        Dim Msi() As Double
        Dim esi() As Double
        Dim fsi() As Double
        Dim fci As Double, Xu As Double, Xbar As Double

        ReDim Asi_Maj(0 To No_majface - 1)
        ReDim Yi_Maj(0 To No_majface - 1)
        ReDim Csi(0 To No_majface - 1)
        ReDim Msi(0 To No_majface - 1)
        ReDim esi(0 To No_majface - 1)
        ReDim fsi(0 To No_majface - 1)

        Dim strain(0 To 7) As Double 'For Fe415 Grade Steel
        Dim stress(0 To 7) As Double

        Dim a, g As Double
        Dim Cc As Double
        Dim Mc As Double

        Pu_values.Clear()
        Mu_values.Clear()

        'Assigning Strain values in an Array
        strain(0) = 0
        strain(1) = 0.00144
        strain(2) = 0.00163
        strain(3) = 0.00192
        strain(4) = 0.00241
        strain(5) = 0.00276
        strain(6) = 0.0038
        strain(7) = 0.1

        'Assigning stress values in an Array
        stress(0) = 0
        stress(1) = 288.7
        stress(2) = 306.7
        stress(3) = 324.8
        stress(4) = 342.8
        stress(5) = 351.8
        stress(6) = 360.9
        stress(7) = 360.9

        Asi_Maj(0) = No_minface * (Pi * Dia_Mainbar ^ 2 / 4)
        Yi_Maj(0) = D / 2 - d1

        Asi_Maj(No_majface - 1) = No_minface * (Pi * Dia_Mainbar ^ 2 / 4)
        Yi_Maj(No_majface - 1) = D / 2 - (d1 + (No_majface - 1) * (D - 2 * d1) / (No_majface - 1))

        For i = 1 To No_majface - 2
            Asi_Maj(i) = 2 * (Pi * Dia_Mainbar ^ 2 / 4)
            Yi_Maj(i) = D / 2 - (d1 + (i - 1) * (D - 2 * d1) / (No_majface - 1))
        Next i

        For i = 1 To 199     ' The design strength components will change for each value of Xu
            Xu = Initial_Value + i * Step_Value
            ' Sheets("Sheet1").Cells(i + 1, 35).Value = Xu / D


            For j = 0 To No_majface - 1    ' For each layer of steel on major side (D)

                ' Calculating the strain values in steel esi
                If Xu <= D Then
                    esi(j) = 0.0035 * (Xu - D / 2 + Yi_Maj(j)) / Xu
                ElseIf Xu > D Then
                    esi(j) = 0.002 * (1 + (Yi_Maj(j) - D / 14) / (Xu - 3 * D / 7))

                End If

                ' Calculating the stress in concrete fci
                If esi(j) <= 0 Then
                    fci = 0
                ElseIf esi(j) >= 0.002 Then
                    fci = 0.447 * Fck
                ElseIf esi(j) > 0 And esi(j) < 0.002 Then
                    fci = 0.447 * Fck * (2 * esi(j) / 0.002 - (esi(j) / 0.002) ^ 2)
                End If

                '' Calculating the Design Strength Components

                ' Calculating  the Cs & Ms values
                For k = 0 To 6
                    If Math.Abs(esi(j)) = strain(k) Then
                        fsi(j) = stress(k)
                    ElseIf Math.Abs(esi(j)) > strain(k) And Math.Abs(esi(j)) < strain(k + 1) Then
                        fsi(j) = (stress(k) + (stress(k + 1) - stress(k)) * (Math.Abs(esi(j)) - strain(k)) / (strain(k + 1) - strain(k))) * (esi(j) / Math.Abs(esi(j)))
                    ElseIf esi(j) < -0.0035 Then
                        fsi(j) = -0.87 * Fy
                    End If
                Next k

                Csi(j) = (fsi(j) - fci) * Asi_Maj(j)
                Msi(j) = Csi(j) * Yi_Maj(j)
            Next j

            Dim sum_Cs As Double
            Dim sum_Ms As Double
            sum_Cs = 0
            sum_Ms = 0

            For j = 0 To No_majface - 1
                sum_Cs = sum_Cs + Csi(j)
                sum_Ms = sum_Ms + Msi(j)
            Next j

            ' Calculating Cc & Mc values
            g = 16 / (7 * (Xu / D) - 3) ^ 2

            If Xu <= D Then
                a = 0.362 * Xu / D
                Xbar = 0.416 * Xu
            ElseIf Xu > D Then
                a = 0.447 * (1 - 4 * g / 21)
                Xbar = (0.5 - 8 * g / 49) * (D / (1 - 4 * g / 21))
            End If

            Cc = a * Fck * B * D
            Mc = Cc * (D / 2 - Xbar)


            Pu_values.Add((Cc + sum_Cs) / (Fck * B * D))
            Mu_values.Add((Mc + sum_Ms) / (Fck * B * D ^ 2))
        Next i
        Dim form2 As New Form2()
        form2.Show()
        form2.PlotGraph(Pu_values, Mu_values)

    End Sub

    Private Function LinearInterpolation(x As Double, x_Values As List(Of Double), y_Values As List(Of Double))
        Dim n As Integer
        n = x_Values.Count

        ' Ensure the lists have the same length and are not empty
        If x_Values.Count <> y_Values.Count OrElse x_Values.Count = 0 Then
            Return Double.NaN
        End If

        ' Find the interval [xValues(i), xValues(i+1)] containing x
        For i = 0 To n - 2
            If x >= x_Values(i) AndAlso x <= x_Values(i + 1) Then
                ' Linear interpolation
                Return y_Values(i) + ((x - x_Values(i)) / (x_Values(i + 1) - x_Values(i)) * (y_Values(i + 1) - y_Values(i)))
            End If
        Next

        ' If x is out of bounds, return an error value (or handle appropriately)
        Return Double.NaN
    End Function

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Dim x, Mu As Double

        x = Convert.ToDouble(TextBox12.Text)

        x_Values.Clear()
        y_Values.Clear()

        x_Values.AddRange(Pu_values)
        y_Values.AddRange(Mu_values)

        Mu = LinearInterpolation(x, x_Values, y_Values)
        TextBox15.Text = Math.Round(Mu, 2)
    End Sub

End Class
