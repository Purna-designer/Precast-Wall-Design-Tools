<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class formLoader
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        Panel1 = New Panel()
        Label1 = New Label()
        PBLoading1 = New ProgressBar()
        LblStatusText = New Label()
        LblReportProgress = New Label()
        Timer1 = New Timer(components)
        PictureBox1 = New PictureBox()
        Label2 = New Label()
        CType(PictureBox1, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' Panel1
        ' 
        Panel1.BackColor = SystemColors.ActiveCaption
        Panel1.Dock = DockStyle.Top
        Panel1.ForeColor = SystemColors.ActiveCaptionText
        Panel1.Location = New Point(0, 0)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(800, 43)
        Panel1.TabIndex = 0
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Font = New Font("Showcard Gothic", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label1.Location = New Point(332, 89)
        Label1.Name = "Label1"
        Label1.Size = New Size(142, 26)
        Label1.TabIndex = 1
        Label1.Text = "WELCOME TO"
        ' 
        ' PBLoading1
        ' 
        PBLoading1.Location = New Point(63, 326)
        PBLoading1.Name = "PBLoading1"
        PBLoading1.Size = New Size(682, 29)
        PBLoading1.TabIndex = 2
        ' 
        ' LblStatusText
        ' 
        LblStatusText.AutoSize = True
        LblStatusText.Font = New Font("Courier New", 10.2F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LblStatusText.Location = New Point(63, 358)
        LblStatusText.Name = "LblStatusText"
        LblStatusText.Size = New Size(269, 20)
        LblStatusText.TabIndex = 3
        LblStatusText.Text = "Loading The Application..."
        ' 
        ' LblReportProgress
        ' 
        LblReportProgress.AutoSize = True
        LblReportProgress.Font = New Font("Bodoni MT", 10.2F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LblReportProgress.Location = New Point(695, 358)
        LblReportProgress.Name = "LblReportProgress"
        LblReportProgress.Size = New Size(50, 21)
        LblReportProgress.TabIndex = 4
        LblReportProgress.Text = "000%"
        LblReportProgress.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' Timer1
        ' 
        Timer1.Enabled = True
        Timer1.Interval = 50
        ' 
        ' PictureBox1
        ' 
        PictureBox1.Image = Global.PM_INTERACTION_CURVE.Resources.POSSIBUILDLOGO1
        PictureBox1.Location = New Point(123, 165)
        PictureBox1.Name = "PictureBox1"
        PictureBox1.Size = New Size(100, 100)
        PictureBox1.SizeMode = PictureBoxSizeMode.Zoom
        PictureBox1.TabIndex = 5
        PictureBox1.TabStop = False
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Font = New Font("Showcard Gothic", 16.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label2.Location = New Point(240, 193)
        Label2.Name = "Label2"
        Label2.Size = New Size(343, 35)
        Label2.TabIndex = 6
        Label2.Text = "PM INTERACTION CURVE"
        ' 
        ' formLoader
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(Label2)
        Controls.Add(PictureBox1)
        Controls.Add(LblReportProgress)
        Controls.Add(LblStatusText)
        Controls.Add(PBLoading1)
        Controls.Add(Label1)
        Controls.Add(Panel1)
        Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        FormBorderStyle = FormBorderStyle.None
        Name = "formLoader"
        StartPosition = FormStartPosition.CenterScreen
        Text = "formLoader"
        CType(PictureBox1, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Panel1 As Panel
    Friend WithEvents Label1 As Label
    Friend WithEvents PBLoading1 As ProgressBar
    Friend WithEvents LblStatusText As Label
    Friend WithEvents LblReportProgress As Label
    Friend WithEvents Timer1 As Timer
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents Label2 As Label
End Class
