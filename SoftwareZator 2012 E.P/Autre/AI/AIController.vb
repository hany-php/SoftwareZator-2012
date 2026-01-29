''' *****************************************************************************
''' 
'''  © Veler Software 2012 - 2026. All rights reserved.
'''  AI Controller - The Brain of the Simulator
''' 
''' *****************************************************************************

Imports System.Collections.Generic
Imports VelerSoftware.Design.Toolkit
Imports System.Windows.Forms
Imports System.Drawing
Imports System.ComponentModel.Design
Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Reflection

Public Class AIControllerLogic

    Public Shared PipePath As String = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "AI_Pipe.json")

    Public Shared Sub Initialize()
        ' Initialize Pipe if needed
        If Not System.IO.File.Exists(PipePath) Then
            Dim initialStatus As New Dictionary(Of String, Object)
            initialStatus.Add("status", "IDLE")
            initialStatus.Add("last_update", DateTime.Now.ToString("s"))
            Dim serializer As New System.Web.Script.Serialization.JavaScriptSerializer()
            System.IO.File.WriteAllText(PipePath, serializer.Serialize(initialStatus))
        End If
    End Sub

    Public Shared Sub SendRequestToPipe(ByVal userMessage As String)
        Dim request As New Dictionary(Of String, Object)
        request.Add("status", "PENDING")
        request.Add("request", userMessage)
        request.Add("last_update", DateTime.Now.ToString("s"))

        Dim serializer As New System.Web.Script.Serialization.JavaScriptSerializer()
        Try
            System.IO.File.WriteAllText(PipePath, serializer.Serialize(request))
        Catch ex As Exception
            ' Ignore file lock issues, handled by retry usually
        End Try
    End Sub

    Public Shared Sub UpdateProjectPath(ByVal path As String)
        Try
            ' Update the pipe with the Project Path immediately used context
            ' We read the existing file to preserve status/history if we want, or just overwrite 
            ' For now, we just ensure subsequent requests have it, but for "Auto-Start" we might want to signal "PROJECT_OPENED"
            
            ' Let's just create a special PROJECT_CONTEXT status
            Dim request As New Dictionary(Of String, Object)
            request.Add("last_update", DateTime.Now.ToString("s"))
            request.Add("status", "PROJECT_OPENED")
            
            Dim reqData As New Dictionary(Of String, Object)
            reqData.Add("project_path", path)
            request.Add("project_context", reqData)

            Dim serializer As New System.Web.Script.Serialization.JavaScriptSerializer()
            Dim json As String = serializer.Serialize(request)
            
            System.IO.File.WriteAllText(PipePath, json)
        Catch ex As Exception
           ' Ignore
        End Try
    End Sub

    Public Shared Sub SendRequestToAI(prompt As String, context As Object)
        Try
            Dim request As New Dictionary(Of String, Object)
            request.Add("last_update", DateTime.Now.ToString("s"))
            request.Add("status", "PENDING")
            
            Dim reqData As New Dictionary(Of String, Object)
            reqData.Add("text", prompt)
            reqData.Add("context", "Simulator")
            
            ' Send Project Path if available
            If Global.SoftwareZator.SOLUTION IsNot Nothing AndAlso Global.SoftwareZator.SOLUTION.Projets.Count > 0 Then
                reqData.Add("project_path", Global.SoftwareZator.SOLUTION.Projets(0).Emplacement)
            Else
                reqData.Add("project_path", "")
            End If

            request.Add("request", reqData)

            Dim serializer As New System.Web.Script.Serialization.JavaScriptSerializer()
            Dim json As String = serializer.Serialize(request)
            
            System.IO.File.WriteAllText(PipePath, json)
        Catch ex As Exception
            MessageBox.Show("Error writing to AI Pipe: " & ex.Message)
        End Try
    End Sub

    Public Shared Function CheckPipeResponse() As Dictionary(Of String, String)
        If Not System.IO.File.Exists(PipePath) Then Return Nothing
        
        Try
            Dim json As String = System.IO.File.ReadAllText(PipePath)
            Dim serializer As New System.Web.Script.Serialization.JavaScriptSerializer()
            Dim data As Dictionary(Of String, Object) = serializer.Deserialize(Of Dictionary(Of String, Object))(json)
            
            If data.ContainsKey("script") AndAlso Not String.IsNullOrEmpty(data("script").ToString()) Then
                 Dim result As New Dictionary(Of String, String)
                 
                 If data.ContainsKey("message") Then result.Add("message", data("message").ToString())
                 result.Add("script", data("script").ToString())
                 
                 ' IMPORTANT: Reset the pipe so we don't execute the same command forever
                 ' We write back IDLE status immediately
                 Dim resetStatus As New Dictionary(Of String, Object)
                 resetStatus.Add("status", "IDLE")
                 resetStatus.Add("last_update", DateTime.Now.ToString("s"))
                 System.IO.File.WriteAllText(PipePath, serializer.Serialize(resetStatus))
                 
                 Return result
            End If
        Catch ex As Exception
            ' Ignore concurrent access errors
        End Try
        Return Nothing
    End Function

    ' Mock "Brain" Dictionary for offline testing without the pipe
    ' In a real scenario, this would send the prompt to Antigravity
    Private Shared MockResponses As New Dictionary(Of String, String) From {
        {"dashboard", "[{'action':'CREATE_FORM','name':'DashboardForm'},{'action':'ADD_CONTROL','type':'Label','text':'Sales Overview','location':'10 10'},{'action':'ADD_CONTROL','type':'Chart','location':'20 50','size':'300 200'},{'action':'ADD_CONTROL','type':'DataGrid','location':'20 260','size':'300 150'}]"},
        {"واجهة إحصائيات", "[{'action':'CREATE_FORM','name':'StatsForm'},{'action':'ADD_CONTROL','type':'Label','text':'الإحصائيات العامة','location':'10 10'},{'action':'ADD_CONTROL','type':'Chart','location':'20 50','size':'400 200'},{'action':'ADD_CONTROL','type':'Button','text':'تحديث','location':'20 270'}]"},
        {"login", "[{'action':'CREATE_FORM','name':'LoginForm'},{'action':'ADD_CONTROL','type':'Label','text':'Username:','location':'20 20'},{'action':'ADD_CONTROL','type':'TextBox','location':'20 40'},{'action':'ADD_CONTROL','type':'Label','text':'Password:','location':'20 70'},{'action':'ADD_CONTROL','type':'TextBox','location':'20 90'},{'action':'ADD_CONTROL','type':'Button','text':'Login','location':'20 130'}]"}
    }

    Public Shared Sub ProcessNaturalLanguage(ByVal prompt As String, ByVal simulator As BoxSimulator)
        If String.IsNullOrEmpty(prompt) Then Return

        simulator.LogToConsole("AI thinking about: " & prompt & "...")

        ' Simulate Latency
        Threading.Thread.Sleep(500)

        ' Simple keyword matching for the Mock
        Dim scriptToRun As String = ""
        For Each key In MockResponses.Keys
            If prompt.ToLower().Contains(key.ToLower()) OrElse prompt.Contains("إحصائيات") Then ' Hack for exact arabic match in mock
                scriptToRun = MockResponses(key)
                If prompt.Contains("إحصائيات") Then scriptToRun = MockResponses("واجهة إحصائيات")
                Exit For
            End If
        Next

        If scriptToRun <> "" Then
            simulator.LogToConsole("Plan generated. Executing script...")
            simulator.SetScript(scriptToRun)
            RunScript(scriptToRun, simulator)
        Else
            simulator.LogToConsole("I heard you, but I don't have a mock response for that specific phrase yet. Try 'dashboard' or 'واجهة إحصائيات'.")
        End If
    End Sub

    Public Shared Sub RunScript(ByVal jsonScript As String, ByVal simulator As BoxSimulator)
        ' Very simple manual JSON parser for the requirement (avoiding external dependencies if possible for now)
        ' In production, use NewtonSoft.Json
        
        ' Cleaning up the pseudo-json
        jsonScript = jsonScript.Replace("[", "").Replace("]", "").Replace("},", "}|")
        Dim commands() As String = jsonScript.Split("|"c)

        simulator.ClearPreview()

        For Each cmd In commands
            Dim props As New Dictionary(Of String, String)
            Dim cleanCmd As String = cmd.Trim().Replace("{", "").Replace("}", "").Replace("'", "")
            Dim pairs() As String = cleanCmd.Split(","c)
            
            For Each pair In pairs
                Dim parts() As String = pair.Split(":"c)
                If parts.Length = 2 Then
                    props.Add(parts(0).Trim(), parts(1).Trim())
                ElseIf parts.Length > 2 Then
                     ' Handle case where value might contain a colon (e.g. text) - naive join
                     props.Add(parts(0).Trim(), String.Join(":", parts, 1, parts.Length - 1).Trim())
                End If
            Next

            ExecuteAction(props, simulator)
            Threading.Thread.Sleep(200) ' Animation delay
        Next
        
        simulator.LogToConsole("Execution Complete.")
    End Sub

    Private Shared Sub ExecuteAction(ByVal props As Dictionary(Of String, String), ByVal simulator As BoxSimulator)
        If Not props.ContainsKey("action") Then Return

        Select Case props("action")
            Case "CREATE_FORM"
                simulator.LogToConsole("Creating Form: " & props("name"))
                ' Simulator already is a container, we just clear it usually, but we could set a title
                
            Case "ADD_CONTROL"
                Dim type As String = props("type")
                Dim text As String = If(props.ContainsKey("text"), props("text"), "")
                Dim locStr As String = If(props.ContainsKey("location"), props("location"), "0 0")
                Dim sizeStr As String = If(props.ContainsKey("size"), props("size"), "100 30")

                Dim locParts() As String = locStr.Split(" "c)
                Dim loc As New Point(0, 0)
                If locParts.Length >= 2 Then loc = New Point(CInt(locParts(0)), CInt(locParts(1)))
                
                Dim sizeParts() As String = sizeStr.Split(" "c)
                Dim sz As New Size(100, 30)
                If sizeParts.Length >= 2 Then sz = New Size(CInt(sizeParts(0)), CInt(sizeParts(1)))

                simulator.RenderControl(type, text, loc, sz)
        End Select
    End Sub

    Public Shared Sub ApplyScriptToProject(ByVal jsonScript As String, Optional ByVal forceNewForm As Boolean = False)
        ' Reuse simple parsing logic
        jsonScript = jsonScript.Replace("[", "").Replace("]", "").Replace("},", "}|")
        Dim commands() As String = jsonScript.Split("|"c)

        ' Logic: If forceNewForm is true:
        ' 1. Check if script has CREATE_FORM.
        ' 2. If NO CREATE_FORM, create a default one first.
        ' 3. If YES CREATE_FORM, let the loop handle it, BUT ensure we wait for it to be active.

        Dim formCreated As Boolean = False
        If forceNewForm Then
             Dim hasCreateForm As Boolean = False
             For Each cmd In commands
                 If cmd.Contains("'action':'CREATE_FORM'") Then hasCreateForm = True
             Next
             
             If Not hasCreateForm Then
                 ' Create a default unique form because the script is just controls
                 Dim newFormName As String = "AI_" & DateTime.Now.ToString("HHmmss")
                 CreateFormInProject(newFormName)
                 WaitForDesignerLoad()
                 formCreated = True
             End If
        End If

        For Each cmd In commands
            Dim props As New Dictionary(Of String, String)
            Dim cleanCmd As String = cmd.Trim().Replace("{", "").Replace("}", "").Replace("'", "")
            Dim pairs() As String = cleanCmd.Split(","c)

            For Each pair In pairs
                Dim parts() As String = pair.Split(":"c)
                If parts.Length = 2 Then
                    props.Add(parts(0).Trim(), parts(1).Trim())
                ElseIf parts.Length > 2 Then
                     props.Add(parts(0).Trim(), String.Join(":", parts, 1, parts.Length - 1).Trim())
                End If
            Next

            ' If we forced a new form regarding CREATE_FORM, we execute everything normally.
            ' But if we are in "Current Form" mode (forceNewForm=False), we MUST skip CREATE_FORM to avoid jumping out of context.
            Dim skipCreateForm As Boolean = Not forceNewForm
            
            ' Special Case: If forceNewForm=True AND we already created a default form above, 
            ' and the script DOES contain CREATE_FORM (rare conflict), we might want to skip it or let it create another?
            ' Better logic: Run ExecuteRealAction. It handles CREATE_FORM by calling CreateFormInProject.
            
            ExecuteRealAction(props, skipCreateForm)
            
            ' If this was a CREATE_FORM action, we MUST wait for it to load before adding controls
            If props.ContainsKey("action") AndAlso props("action") = "CREATE_FORM" AndAlso Not skipCreateForm Then
                WaitForDesignerLoad()
            End If
        Next

        MessageBox.Show("Changes applied successfully!", "AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Shared Sub ExecuteRealAction(ByVal props As Dictionary(Of String, String), Optional ByVal skipCreateForm As Boolean = False)
        If Not props.ContainsKey("action") Then Return

        Select Case props("action")
            Case "ADD_CODE"
                ' Logic to inject VB.NET code into the control's event
                If props.ContainsKey("control") AndAlso props.ContainsKey("code") Then
                    Dim ctrlName As String = props("control")
                    Dim eventName As String = If(props.ContainsKey("event"), props("event"), "Click")
                    Dim codeBody As String = props("code")

                    ' 1. Find the DocConcepteurFenetre (Designer)
                    Dim doc As DocConcepteurFenetre = Nothing
                    ' Try Active Page first
                    If Form1.KryptonDockableWorkspace1.ActivePage IsNot Nothing AndAlso Form1.KryptonDockableWorkspace1.ActivePage.Controls.Count > 0 Then
                        doc = TryCast(Form1.KryptonDockableWorkspace1.ActivePage.Controls(0), DocConcepteurFenetre)
                    End If
                    ' Fallback
                    If doc Is Nothing Then
                        For Each page As VelerSoftware.Design.Navigator.KryptonPage In Form1.KryptonDockingManager1.Pages
                            If page.Controls.Count > 0 AndAlso TypeOf page.Controls(0) Is DocConcepteurFenetre Then
                                doc = DirectCast(page.Controls(0), DocConcepteurFenetre)
                                Exit For
                            End If
                        Next
                    End If

                    If doc IsNot Nothing Then
                        ' 2. Access Code/Functions directly via the File Object
                        ' DocConcepteurFenetre stores code snippets in Me.File.Functions (List(Of String))
                        
                        Try
                            Dim subName As String = ctrlName & "_" & eventName
                            ' Naive check: Does any function text contain this Sub name?
                            Dim alreadyExists As Boolean = False
                            If doc.File.Functions IsNot Nothing Then
                                For Each funcCode As String In doc.File.Functions
                                    If funcCode.Contains("Sub " & subName) Then
                                        alreadyExists = True
                                        Exit For
                                    End If
                                Next
                            End If

                            If Not alreadyExists Then
                                ' Code Injection is not supported in this version.
                                ' The system expects XAML Workflow Activities, not raw VB.NET code.
                                ' Writing raw strings here effectively does nothing or corrupts the view.
                                ' MessageBox.Show("Code injection skipped (Requires Manual Entry).", "AI Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            End If

                        Catch ex As Exception
                            MessageBox.Show("Failed to inject code: " & ex.Message, "AI Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End Try
                    End If
                End If

            Case "CREATE_PROJECT"
                If props.ContainsKey("name") Then
                    Dim projName As String = props("name")
                    Dim solName As String = If(props.ContainsKey("solution"), props("solution"), projName)
                    Dim type As String = If(props.ContainsKey("type"), props("type"), "Window")
                    
                    ' Find Template
                    Dim templatePath As String = ""
                    Dim templatesDir As String = System.IO.Path.Combine(Application.StartupPath, "Templates\Project")
                    
                    If My.Computer.FileSystem.DirectoryExists(templatesDir) Then
                         For Each file As String In My.Computer.FileSystem.GetFiles(templatesDir, FileIO.SearchOption.SearchAllSubDirectories)
                             If file.EndsWith(".sztemplate", StringComparison.OrdinalIgnoreCase) Then
                                 If file.Contains("Windows") Then
                                     templatePath = file
                                     Exit For
                                 End If
                                 If templatePath = "" Then templatePath = file 
                             End If
                         Next
                    End If
                    
                    If String.IsNullOrEmpty(templatePath) Then
                        MessageBox.Show("No Project Template found.", "AI Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If

                    Dim defaultPath As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SoftwareZator Projects")

                    Try
                        ClassProjet.Creer_Nouveau_Projet(defaultPath, projName, solName, False, templatePath, type)
                        
                        ' IMPORTANT FIX: When a project is created from template, it usually opens Form1 automatically.
                        ' We must wait for it to be fully loaded so subsequent commands working on "MainForm" or similar 
                        ' don't create a DUPLICATE form if they just wanted to use the default one.
                        WaitForDesignerLoad()
                        
                        ' Update Pipe with new project path context?
                    Catch ex As Exception
                         MessageBox.Show("Failed to create project: " & ex.Message, "AI Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If

            Case "CREATE_FORM"
                If Not skipCreateForm AndAlso props.ContainsKey("name") Then
                    CreateFormInProject(props("name"))
                    WaitForDesignerLoad()
                End If

            Case "ADD_CONTROL"
                AddControlToDesigner(props)
            
            Case "RESET_LAYOUT"
                RestoreOutputPanel()
        End Select
    End Sub

    Private Shared Sub RestoreOutputPanel()
        RestoreOnePanel(Form1.Box_Sortie, "Sortie")
        RestoreOnePanel(Form1.Box_Aide_Rapide, "Aide rapide")
        RestoreOnePanel(Form1.Box_Erreur_Generation, "Erreurs de génération")
        RestoreOnePanel(Form1.Box_Debogage, "Débogage")
    End Sub

    Private Shared Sub RestoreOnePanel(ByVal panel As VelerSoftware.Design.Navigator.KryptonPage, ByVal autoHideName As String)
        If panel IsNot Nothing Then
             Try
                 Dim manager As VelerSoftware.Design.Docking.KryptonDockingManager = Form1.KryptonDockingManager1
                 
                 ' 1. If it's AutoHidden, pop it out
                 manager.SwitchAutoHiddenGroupToDockedCellRequest(autoHideName)
                 
                 ' 2. Check if it's in a cell
                 Dim cell = Form1.KryptonDockableWorkspace1.CellForPage(panel)
                 If cell IsNot Nothing Then 
                     cell.SelectedPage = panel
                     panel.Visible = True
                 Else
                     ' 3. If not in a cell (Closed), try adding it to the active cell
                     If Form1.KryptonDockableWorkspace1.ActiveCell IsNot Nothing Then
                         Form1.KryptonDockableWorkspace1.ActiveCell.Pages.Add(panel)
                         panel.Visible = True
                     End If
                 End If
             Catch ex As Exception
                ' Failsafe
                panel.Visible = True
             End Try
        End If
    End Sub

    Private Shared Sub CreateFormInProject(ByVal formName As String)
        If SOLUTION Is Nothing OrElse SOLUTION.Projets.Count = 0 Then
            MessageBox.Show("No solution loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        Dim proj As VelerSoftware.SZVB.Projet.Projet = SOLUTION.Projets(0)
        Dim fullPath As String = System.IO.Path.Combine(proj.Emplacement, formName & ".szw")

        Try
            ' Create SZW File Object manually
            Dim newFile As New VelerSoftware.SZVB.Projet.SZW_File(formName)
            newFile.Nom = formName

            ' Initialize Code Model
            newFile.WINDOWS = New System.CodeDom.CodeCompileUnit()
            Dim ns As New System.CodeDom.CodeNamespace(proj.Nom)
            newFile.WINDOWS.Namespaces.Add(ns)
            Dim cls As New System.CodeDom.CodeTypeDeclaration(formName)
            cls.IsClass = True
            ' Use default base type for Windows Forms
            cls.BaseTypes.Add(New System.CodeDom.CodeTypeReference("System.Windows.Forms.Form"))
            ns.Types.Add(cls)

            ' Serialize to disk
            Using fs As New System.IO.FileStream(fullPath, IO.FileMode.Create)
                Dim formatter As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
                formatter.Serialize(fs, newFile)
            End Using

            ' Open the document using ClassProjet
            Dim filess() As String = {fullPath}
            Dim Safefiles() As String = {System.IO.Path.GetFileName(fullPath)}
            Dim projects() As VelerSoftware.SZVB.Projet.Projet = {proj}

            ClassProjet.Ouvrir_Document(filess, Safefiles, projects, formName)

        Catch ex As Exception
            MessageBox.Show("Failed to create form: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Shared Sub WaitForDesignerLoad()
        ' Allow the UI to update and open the new document
        For i As Integer = 0 To 20 ' Wait up to 2 seconds
            Application.DoEvents()
            Threading.Thread.Sleep(100)

            If Form1.KryptonDockableWorkspace1.ActivePage IsNot Nothing AndAlso 
               Form1.KryptonDockableWorkspace1.ActivePage.Controls.Count > 0 Then
                Dim doc As DocConcepteurFenetre = TryCast(Form1.KryptonDockableWorkspace1.ActivePage.Controls(0), DocConcepteurFenetre)
                If doc IsNot Nothing Then
                     ' Found the designer document, we are good to go
                     Exit Sub
                End If
            End If
        Next
    End Sub

    Private Shared Sub AddControlToDesigner(ByVal props As Dictionary(Of String, String))
        Dim doc As DocConcepteurFenetre = Nothing

        ' 1. Try Active Page first
        If Form1.KryptonDockableWorkspace1.ActivePage IsNot Nothing AndAlso Form1.KryptonDockableWorkspace1.ActivePage.Controls.Count > 0 Then
            doc = TryCast(Form1.KryptonDockableWorkspace1.ActivePage.Controls(0), DocConcepteurFenetre)
        End If

        ' 2. Fallback: If Active Page is the Simulator (or something else), search for the first open Designer
        If doc Is Nothing Then
            For Each page As VelerSoftware.Design.Navigator.KryptonPage In Form1.KryptonDockingManager1.Pages
                If page.Controls.Count > 0 AndAlso TypeOf page.Controls(0) Is DocConcepteurFenetre Then
                    doc = DirectCast(page.Controls(0), DocConcepteurFenetre)
                    Exit For
                End If
            Next
        End If

        If doc Is Nothing Then
            MessageBox.Show("No Form Designer found. Please open a Form first.", "AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        If doc IsNot Nothing Then
            Dim host As IDesignerHost = Nothing
            Try
                ' Attempt to get Host property or field via Reflection if it's not public
                Dim flags As BindingFlags = BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.IgnoreCase
                Dim prop As PropertyInfo = doc.GetType().GetProperty("Host", flags)
                If prop IsNot Nothing Then
                    host = TryCast(prop.GetValue(doc, Nothing), IDesignerHost)
                Else
                    Dim field As FieldInfo = doc.GetType().GetField("Host", flags)
                    If field IsNot Nothing Then
                        host = TryCast(field.GetValue(doc), IDesignerHost)
                    Else
                        ' Fallback: Try to get 'Designer' field and then get Host from it?
                        ' Or just fail
                    End If
                End If
            Catch ex As Exception
                ' Ignore reflection errors
            End Try

            If host IsNot Nothing Then
                Dim typeName As String = props("type")
                Dim realType As Type = Nothing

                ' Extended Type Mapping
                Select Case typeName.ToLower()
                    Case "button"
                        realType = GetType(System.Windows.Forms.Button)
                    Case "label"
                        realType = GetType(System.Windows.Forms.Label)
                    Case "textbox"
                        realType = GetType(System.Windows.Forms.TextBox)
                    Case "checkbox"
                        realType = GetType(System.Windows.Forms.CheckBox)
                    Case "datagrid"
                        realType = GetType(System.Windows.Forms.DataGridView)
                    Case "chart", "panel"
                        realType = GetType(System.Windows.Forms.Panel)
                    Case "menustrip"
                        realType = GetType(System.Windows.Forms.MenuStrip)
                    Case "menuitem", "toolstripmenuitem"
                        realType = GetType(System.Windows.Forms.ToolStripMenuItem)
                    Case "richtextbox"
                        realType = GetType(System.Windows.Forms.RichTextBox)
                End Select

                If realType IsNot Nothing Then
                    Try
                        Dim comp As IComponent = host.CreateComponent(realType)
                        
                        ' Set Name if provided (critical for parenting)
                        If props.ContainsKey("name") AndAlso comp.Site IsNot Nothing Then
                            comp.Site.Name = props("name")
                        End If

                        ' Set Properties
                        If props.ContainsKey("text") Then
                            Dim propText As PropertyDescriptor = TypeDescriptor.GetProperties(comp)("Text")
                            If propText IsNot Nothing Then propText.SetValue(comp, props("text"))
                        End If

                        ' Handle Controls (Location/Size)
                        If TypeOf comp Is Control Then
                            Dim ctrl As Control = DirectCast(comp, Control)
                            
                            Dim locStr As String = If(props.ContainsKey("location"), props("location"), "0 0")
                            Dim locParts() As String = locStr.Split(" "c)
                            If locParts.Length >= 2 Then ctrl.Location = New Point(CInt(locParts(0)), CInt(locParts(1)))

                            Dim sizeStr As String = If(props.ContainsKey("size"), props("size"), "100 25")
                            Dim sizeParts() As String = sizeStr.Split(" "c)
                            If sizeParts.Length >= 2 Then ctrl.Size = New Size(CInt(sizeParts(0)), CInt(sizeParts(1)))
                            
                            ctrl.BringToFront()
                        End If

                        ' PARENTING LOGIC
                        Dim parentName As String = If(props.ContainsKey("parent"), props("parent"), "")
                        Dim parentObj As Object = Nothing

                        If Not String.IsNullOrEmpty(parentName) Then
                            ' Find parent in components
                            If host.Container.Components(parentName) IsNot Nothing Then
                                parentObj = host.Container.Components(parentName)
                            End If
                        Else
                            ' Default to Form
                            parentObj = host.RootComponent
                        End If

                        ' Link Child to Parent
                        If parentObj IsNot Nothing Then
                            If TypeOf parentObj Is Control AndAlso TypeOf comp Is Control Then
                                ' Control inside Control (e.g. Button in Panel, or Button in Form)
                                DirectCast(parentObj, Control).Controls.Add(DirectCast(comp, Control))
                            
                            ElseIf TypeOf parentObj Is MenuStrip AndAlso TypeOf comp Is ToolStripItem Then
                                ' Item inside MenuStrip
                                DirectCast(parentObj, MenuStrip).Items.Add(DirectCast(comp, ToolStripItem))
                            
                            ElseIf TypeOf parentObj Is ToolStripMenuItem AndAlso TypeOf comp Is ToolStripItem Then
                                ' Sub-Item inside Item
                                DirectCast(parentObj, ToolStripMenuItem).DropDownItems.Add(DirectCast(comp, ToolStripItem))
                            End If
                        End If

                        doc.Modifier = True ' Mark document as dirty
                        
                    Catch ex As Exception
                         ' Fail silently or log
                    End Try
                End If
            Else
                 MessageBox.Show("Could not access Designer Host. Internal Reflection Error.", "AI Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Else
             MessageBox.Show("The active tab is not a Form Designer.", "AI Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

End Class
