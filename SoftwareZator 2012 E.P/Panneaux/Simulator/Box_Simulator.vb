''' *****************************************************************************
''' 
'''  © Veler Software 2012 - 2026. All rights reserved.
'''  AI Studio Simulator - Live Preview for SoftwareZator
''' 
''' *****************************************************************************

Public Class BoxSimulator

    ' UI Controls
    Private WithEvents ApplyButton As VelerSoftware.Design.Toolkit.KryptonButton
    Private WithEvents ApplyNewButton As VelerSoftware.Design.Toolkit.KryptonButton
    
    ' Remove LogLabel and PreviewContainer declarations as they are in Designer or we alias them
    Public LastGeneratedScript As String = ""

    ' Polling Timer for AI Response
    Private WithEvents AIPollTimer As New Timer()

    Private Sub BoxSimulator_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        InitializeCustomUI()
        
        AIPollTimer.Interval = 2000 ' Poll every 2 seconds
        AIPollTimer.Start()

        AddHandler VelerSoftware.Design.Toolkit.KryptonManager.GlobalPaletteChanged, AddressOf OnGlobalPaletteChanged
        OnGlobalPaletteChanged(Nothing, EventArgs.Empty)
    End Sub

    Private Sub InitializeCustomUI()
        ' Do NOT clear controls, respecting the Designer
        ' Me.Controls.Clear() 

        ' Input Area Panel (Bottom)
        Dim bottomPanel As New Panel()
        bottomPanel.Dock = DockStyle.Bottom
        bottomPanel.Height = 60
        bottomPanel.Padding = New Padding(5)
        
        ' Add to KryptonPanel1 which is the main container from Designer
        Me.KryptonPanel1.Controls.Add(bottomPanel)
        bottomPanel.BringToFront() ' Ensure it sits on top/bottom correctly

        ' Only Apply Button needed now
        ApplyButton = New VelerSoftware.Design.Toolkit.KryptonButton()
        ApplyButton.Text = "Apply (Current)"
        ApplyButton.Dock = DockStyle.Right
        ApplyButton.Width = 100
        ApplyButton.Enabled = False
        bottomPanel.Controls.Add(ApplyButton)

        ' Spacer
        Dim spacer As New Panel()
        spacer.Width = 5
        spacer.Dock = DockStyle.Right
        bottomPanel.Controls.Add(spacer)

        ' Apply New Form Button
        ApplyNewButton = New VelerSoftware.Design.Toolkit.KryptonButton()
        ApplyNewButton.Text = "Apply (New Form)"
        ApplyNewButton.Dock = DockStyle.Right
        ApplyNewButton.Width = 110
        ApplyNewButton.Enabled = False
        bottomPanel.Controls.Add(ApplyNewButton)

        ' Input Area (Left side) removed as per user request
        ' Keeping panels simple


        ' Redirect LogLabel to use lblStatus from Designer
        ' The designer "lblStatus" is a KryptonLabel docked bottom. 
        ' We will use it for status messages.
        
        ' PreviewContainer is already created by Designer
        ' Ensure it is visible/styled if needed
        If Me.PreviewContainer IsNot Nothing Then
             Me.PreviewContainer.BackColor = System.Drawing.Color.WhiteSmoke
             Me.PreviewContainer.AutoScroll = True
        End If
    End Sub

    Private Sub OnGlobalPaletteChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If VelerSoftware.Design.Toolkit.KryptonManager.CurrentGlobalPalette IsNot Nothing Then
             Dim palette As VelerSoftware.Design.Toolkit.IPalette = VelerSoftware.Design.Toolkit.KryptonManager.CurrentGlobalPalette
             Dim font As System.Drawing.Font = palette.GetContentShortTextFont(VelerSoftware.Design.Toolkit.PaletteContentStyle.LabelNormalControl, VelerSoftware.Design.Toolkit.PaletteState.Normal)
             If lblStatus IsNot Nothing Then lblStatus.StateCommon.ShortText.Font = font
        End If
    End Sub

    ' --- Interaction Logic ---

    Private Sub AIPollTimer_Tick(sender As Object, e As EventArgs) Handles AIPollTimer.Tick
        Dim response As Dictionary(Of String, String) = Global.SoftwareZator.AIControllerLogic.CheckPipeResponse()
        If response IsNot Nothing Then
             If response.ContainsKey("message") Then LogToConsole("AI: " & response("message"))
             If response.ContainsKey("script") Then 
                 SetScript(response("script"))
                 ' TRIGGER LIVE PREVIEW
                 Global.SoftwareZator.AIControllerLogic.RunScript(response("script"), Me)
                 LogToConsole("AI generated a plan. Check the preview above.")
             End If
        End If
    End Sub



    Private Sub ApplyButton_Click(sender As Object, e As EventArgs) Handles ApplyButton.Click
        If Not String.IsNullOrEmpty(LastGeneratedScript) Then
            Global.SoftwareZator.AIControllerLogic.ApplyScriptToProject(LastGeneratedScript, False)
            ResetSimulatorState()
        End If
    End Sub

    Private Sub ApplyNewButton_Click(sender As Object, e As EventArgs) Handles ApplyNewButton.Click
        If Not String.IsNullOrEmpty(LastGeneratedScript) Then
            Global.SoftwareZator.AIControllerLogic.ApplyScriptToProject(LastGeneratedScript, True)
            ResetSimulatorState()
        End If
    End Sub

    Private Sub ResetSimulatorState()
        LastGeneratedScript = ""
        ApplyButton.Enabled = False
        ApplyNewButton.Enabled = False
        ClearPreview()
        
        ' Force cleanup
        If Me.PreviewContainer IsNot Nothing Then
            Me.PreviewContainer.Controls.Clear() ' Double check
        End If
        GC.Collect() ' Aggressive cleanup as requested
        LogToConsole("Applied successfully. Memory cleared.")
    End Sub

    ' --- Public API for AIController ---

    Public Sub LogToConsole(ByVal message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() LogToConsole(message))
            Return
        End If
        ' Use Designer Control
        If Me.lblStatus IsNot Nothing Then
             Me.lblStatus.Text = "> " & message
             ' Ensure it's visible if hidden by other dock controls
             Me.lblStatus.BringToFront() 
        End If
    End Sub

    Public Sub SetScript(ByVal script As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() SetScript(script))
            Return
        End If
        LastGeneratedScript = script
        ApplyButton.Enabled = Not String.IsNullOrEmpty(script)
        ApplyNewButton.Enabled = Not String.IsNullOrEmpty(script)
    End Sub

    Public Sub ClearPreview()
        If Me.InvokeRequired Then
            Me.Invoke(Sub() ClearPreview())
            Return
        End If
        PreviewContainer.Controls.Clear()
    End Sub

    Public Sub RenderControl(ByVal type As String, ByVal text As String, ByVal location As System.Drawing.Point, ByVal size As System.Drawing.Size)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() RenderControl(type, text, location, size))
            Return
        End If

        Dim newCtrl As Control = Nothing

        Select Case type.ToLower()
            Case "button"
                newCtrl = New VelerSoftware.Design.Toolkit.KryptonButton() With {.Text = text}
            Case "label"
                newCtrl = New VelerSoftware.Design.Toolkit.KryptonLabel() With {.Text = text}
            Case "textbox"
                newCtrl = New VelerSoftware.Design.Toolkit.KryptonTextBox() With {.Text = text}
            Case "checkbox"
                newCtrl = New VelerSoftware.Design.Toolkit.KryptonCheckBox() With {.Text = text}
            Case "richtextbox"
                newCtrl = New VelerSoftware.Design.Toolkit.KryptonRichTextBox() With {.Text = text}
            Case "create_project"
                newCtrl = New Panel() With {.BorderStyle = BorderStyle.FixedSingle, .BackColor = System.Drawing.Color.LightYellow}
                Dim lbl As New Label() With {.Text = "NEW PROJECT: " & text, .Dock = DockStyle.Fill, .TextAlign = System.Drawing.ContentAlignment.MiddleCenter, .Font = New System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold)}
                newCtrl.Controls.Add(lbl)
            Case "create_form"
                newCtrl = New Panel() With {.BorderStyle = BorderStyle.Fixed3D, .BackColor = System.Drawing.Color.WhiteSmoke}
                Dim lbl As New Label() With {.Text = "NEW FORM: " & text, .Dock = DockStyle.Fill, .TextAlign = System.Drawing.ContentAlignment.MiddleCenter}
                newCtrl.Controls.Add(lbl)
            Case "datagrid"
                newCtrl = New VelerSoftware.Design.Toolkit.KryptonDataGridView()
            Case "chart"
                ' Fallback for chart if no specific library is handy, use a Panel placeholder
                newCtrl = New Panel() With {.BorderStyle = BorderStyle.FixedSingle, .BackColor = System.Drawing.Color.LightBlue}
                Dim lbl As New Label() With {.Text = "Chart: " & text, .Dock = DockStyle.Fill, .TextAlign = System.Drawing.ContentAlignment.MiddleCenter}
                newCtrl.Controls.Add(lbl)
            Case Else
                newCtrl = New Panel() With {.BorderStyle = BorderStyle.FixedSingle}
        End Select

        If newCtrl IsNot Nothing Then
            newCtrl.Location = location
            newCtrl.Size = size
            PreviewContainer.Controls.Add(newCtrl)
            newCtrl.BringToFront()
        End If
    End Sub

End Class
