Imports System.Windows.Forms.DataVisualization.Charting

Public Class Form2
    Private chart As Chart
    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        chart = New Chart()
        chart.Dock = DockStyle.Fill

        Dim chartArea As New ChartArea()
        chart.ChartAreas.Add(chartArea)

        Dim series As New Series()
        series.Name = "Pu vs Mu"
        series.ChartType = SeriesChartType.Line

        chart.Series.Add(series)
        Me.Controls.Add(chart)

        series.Color = Color.FromName("Blue")
        series.BorderWidth = 2

        chart.Titles.Add("PM INTERACTION CURVE")
        chart.ChartAreas(0).AxisX.Title = "Mu / Fck B D^2"
        chart.ChartAreas(0).AxisY.Title = "Pu / Fck B D"
        chart.ChartAreas(0).AxisX.LabelStyle.Format = "0.00"

    End Sub
    Public Sub PlotGraph(Pu_values As List(Of Double), Mu_values As List(Of Double))
        If chart Is Nothing Then Return

        Dim series As Series = chart.Series("Pu vs Mu")

        series.Points.Clear()

        For i As Integer = 0 To Pu_values.Count - 1
            series.Points.AddXY(Mu_values(i), Pu_values(i))
        Next

    End Sub

End Class