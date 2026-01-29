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
                 
                 Dim scriptObj As Object = data("script")
                 If TypeOf scriptObj Is String Then
                     result.Add("script", scriptObj.ToString())
                 Else
                     result.Add("script", serializer.Serialize(scriptObj))
                 End If
                 
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
        {"login", "[{'action':'CREATE_FORM','name':'LoginForm'},{'action':'ADD_CONTROL','type':'Label','text':'Username:','location':'20 20'},{'action':'ADD_CONTROL','type':'TextBox','location':'20 40'},{'action':'ADD_CONTROL','type':'Label','text':'Password:','location':'20 70'},{'action':'ADD_CONTROL','type':'TextBox','location':'20 90'},{'action':'ADD_CONTROL','type':'Button','text':'Login','location':'20 130'}]"},
        {"calculator", "[{'action':'CREATE_FORM','name':'CalcForm'},{'action':'ADD_CONTROL','type':'TextBox','name':'txtDisplay','location':'10 10','size':'220 30'},{'action':'ADD_CONTROL','type':'Button','name':'btn1','text':'1','location':'10 50','size':'50 50'},{'action':'ADD_CODE','control':'btn1','event':'Click','code':'txtDisplay.Text &= ""1""'},{'action':'ADD_CONTROL','type':'Button','name':'btn2','text':'2','location':'70 50','size':'50 50'},{'action':'ADD_CODE','control':'btn2','event':'Click','code':'txtDisplay.Text &= ""2""'},{'action':'ADD_CONTROL','type':'Button','name':'btnClear','text':'Clear','location':'130 50','size':'50 50'},{'action':'ADD_CODE','control':'btnClear','event':'Click','code':'txtDisplay.Text = """"'},{'action':'ADD_CONTROL','type':'Button','name':'btnExit','text':'Exit','location':'10 110','size':'100 30'},{'action':'ADD_CODE','control':'btnExit','event':'Click','code':'Me.Close()'}]"}
    }

    Public Shared Sub ProcessNaturalLanguage(ByVal prompt As String, ByVal simulator As BoxSimulator)
        If String.IsNullOrEmpty(prompt) Then Return

        simulator.LogToConsole("AI thinking about: " & prompt & "...")

        ' Simulate Latency
        Threading.Thread.Sleep(500)

        ' Simple keyword matching for the Mock
        Dim scriptToRun As String = ""
        For Each key In MockResponses.Keys
            If prompt.ToLower().Contains(key.ToLower()) OrElse prompt.Contains("إحصائيات") OrElse prompt.Contains("آلة حاسبة") Then
                scriptToRun = MockResponses(key)
                If prompt.Contains("إحصائيات") Then scriptToRun = MockResponses("واجهة إحصائيات")
                If prompt.Contains("آلة حاسبة") Then scriptToRun = MockResponses("calculator")
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
        
        ' Use JavaScriptSerializer for robust JSON parsing
        Try
            Dim serializer As New System.Web.Script.Serialization.JavaScriptSerializer()
            Dim commands As New List(Of Dictionary(Of String, Object))
            commands = serializer.Deserialize(Of List(Of Dictionary(Of String, Object)))(jsonScript)

            simulator.ClearPreview()

            For Each dict As Dictionary(Of String, Object) In commands
                Dim props As New Dictionary(Of String, String)
                For Each kvp In dict
                    props.Add(kvp.Key, kvp.Value.ToString())
                Next

                ExecuteAction(props, simulator)
                Threading.Thread.Sleep(200) ' Animation delay
            Next

        Catch ex As Exception
             simulator.LogToConsole("Error parsing/executing script: " & ex.Message)
             ' Fallback to manual parser if needed (Old Mock format compatible?)
             ' No, relying on standard JSON from now on.
        End Try
        
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
        ' Use JavaScriptSerializer for robust JSON parsing
        Dim commands As New List(Of Dictionary(Of String, Object))
            Dim serializer As New System.Web.Script.Serialization.JavaScriptSerializer()
            Try
                ' Try deserializing as a List directly first
                commands = serializer.Deserialize(Of List(Of Dictionary(Of String, Object)))(jsonScript)
            Catch ex As Exception
                ' If that fails, try deserializing as a Dictionary (Response Wrapper)
                Try
                    Dim responseObj As Dictionary(Of String, Object) = serializer.Deserialize(Of Dictionary(Of String, Object))(jsonScript)
                    If responseObj.ContainsKey("script") Then
                        ' The script is likely an ArrayList of Dictionaries
                        Dim rawScript As Object = responseObj("script")
                        If TypeOf rawScript Is System.Collections.ArrayList Then
                            For Each item As Dictionary(Of String, Object) In DirectCast(rawScript, System.Collections.ArrayList)
                                commands.Add(item)
                            Next
                        ElseIf TypeOf rawScript Is Object() Then
                             For Each item As Dictionary(Of String, Object) In DirectCast(rawScript, Object())
                                commands.Add(item)
                            Next
                        End If
                    End If
                Catch ex2 As Exception
                    MessageBox.Show("JSON Parse Error: " & ex.Message & vbCrLf & "Wrapper Error: " & ex2.Message)
                    Return
                End Try
            End Try

        ' Logic: If forceNewForm is true:
        ' 1. Check if script has CREATE_FORM.
        ' 2. If NO CREATE_FORM, create a default one first.
        ' 3. If YES CREATE_FORM, let the loop handle it, BUT ensure we wait for it to be active.

        If forceNewForm Then
             Dim hasCreateForm As Boolean = False
             For Each dict In commands
                 If dict.ContainsKey("action") AndAlso dict("action").ToString() = "CREATE_FORM" Then
                     hasCreateForm = True
                     Exit For
                 End If
             Next
             
             If Not hasCreateForm Then
                 ' Create a default unique form because the script is just controls
                 Dim newFormName As String = "AI_" & DateTime.Now.ToString("HHmmss")
                 CreateFormInProject(newFormName)
                 WaitForDesignerLoad(newFormName)
             End If
        End If

        For Each dict As Dictionary(Of String, Object) In commands
            Dim props As New Dictionary(Of String, String)
            For Each kvp In dict
                props.Add(kvp.Key, kvp.Value.ToString())
            Next

            ' If we forced a new form regarding CREATE_FORM, we execute everything normally.
            ' But if we are in "Current Form" mode (forceNewForm=False), we MUST skip CREATE_FORM to avoid jumping out of context.
            Dim skipCreateForm As Boolean = Not forceNewForm
            
            ExecuteRealAction(props, skipCreateForm)
            System.Threading.Thread.Sleep(50)
            
            ' If this was a CREATE_FORM action, we MUST wait for it to load before adding controls
            If props.ContainsKey("action") AndAlso props("action") = "CREATE_FORM" AndAlso Not skipCreateForm Then
                If props.ContainsKey("name") Then
                    WaitForDesignerLoad(props("name"))
                End If
            End If
        Next

        ' FORCE SAVE: Ensure changes are written to disk so MSBuild/Simulator can see them
        Dim doc As DocConcepteurFenetre = Nothing
        Try
            ' 1. Try Active Page first
            If Form1.KryptonDockableWorkspace1.ActivePage IsNot Nothing AndAlso Form1.KryptonDockableWorkspace1.ActivePage.Controls.Count > 0 Then
                doc = TryCast(Form1.KryptonDockableWorkspace1.ActivePage.Controls(0), DocConcepteurFenetre)
            End If

            ' 2. Fallback: Search all pages if Active Page is not a Designer (e.g. User is looking at Simulator)
            If doc Is Nothing Then
                For Each page As VelerSoftware.Design.Navigator.KryptonPage In Form1.KryptonDockingManager1.Pages
                    If page.Controls.Count > 0 AndAlso TypeOf page.Controls(0) Is DocConcepteurFenetre Then
                        doc = DirectCast(page.Controls(0), DocConcepteurFenetre)
                        Exit For
                    End If
                Next
            End If
            
            If doc IsNot Nothing Then
                doc.Enregistrer(False)
                ' Force a repaint of the designer surface
                 doc.Designer.View.Refresh()
            End If
        Catch ex As Exception
            ' Ignore save errors during automation
        End Try

        Dim targetName As String = "Unknown"
        If doc IsNot Nothing Then targetName = doc.NomFichier
        MessageBox.Show("Changes applied successfully! Project Saved. Target: " & targetName, "AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Information)
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
                        
                        ' STABILITY: Wait for control to be registered in Designer (Robust Check)
                        Dim host As IDesignerHost = Nothing
                        Try
                            Dim flags As BindingFlags = BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.IgnoreCase
                            Dim prop As PropertyInfo = doc.GetType().GetProperty("Host", flags)
                            If prop IsNot Nothing Then host = TryCast(prop.GetValue(doc, Nothing), IDesignerHost)
                        Catch
                        End Try

                        If host IsNot Nothing Then
                            For i As Integer = 0 To 50 ' Wait up to 5 seconds
                                If host.Container.Components(ctrlName) IsNot Nothing Then Exit For
                                System.Windows.Forms.Application.DoEvents()
                                System.Threading.Thread.Sleep(100)
                            Next
                        Else
                             System.Threading.Thread.Sleep(2000) ' Fallback delay
                        End If

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
                                ' Generate XAML for the new function
                                Dim xaml As new System.Text.StringBuilder()
                                xaml.AppendLine("<vgp:Fonction xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities"" xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"" xmlns:mva=""clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities"" xmlns:sap=""http://schemas.microsoft.com/netfx/2009/xaml/activities/presentation"" xmlns:sap2010=""http://schemas.microsoft.com/netfx/2010/xaml/activities/presentation"" xmlns:scg=""clr-namespace:System.Collections.Generic;assembly=mscorlib"" xmlns:sco=""clr-namespace:System.Collections.ObjectModel;assembly=mscorlib"" xmlns:sd=""clr-namespace:System.Drawing;assembly=System.Drawing"" xmlns:sl=""clr-namespace:System.Linq;assembly=System.Core"" xmlns:st=""clr-namespace:System.Text;assembly=mscorlib"" xmlns:sys=""clr-namespace:System;assembly=mscorlib"" xmlns:system=""clr-namespace:System;assembly=mscorlib"" xmlns:vgp=""clr-namespace:VelerSoftware.GeneralPlugin;assembly=VelerSoftware.GeneralPlugin"" xmlns:vp=""clr-namespace:VelerSoftware.Plugins3;assembly=VelerSoftware.Plugins3"" xmlns:vsz=""clr-namespace:VelerSoftware.SZVB;assembly=VelerSoftware.SZVB"" xmlns:vszp=""clr-namespace:VelerSoftware.SZVB.Projet;assembly=VelerSoftware.SZVB"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">")
                                xaml.AppendLine("  <vgp:Fonction.Param1>" & subName & "</vgp:Fonction.Param1>")
                                xaml.AppendLine("  <vgp:Fonction.DisplayName>" & subName & "</vgp:Fonction.DisplayName>")
                                
                                ' Param2 = ControlName.EventName (Critical for Event Binding)
                                xaml.AppendLine("  <vgp:Fonction.Param2>" & ctrlName & "." & eventName & "</vgp:Fonction.Param2>")

                                ' Param3 = Event Signature (Critical for Event Binding)
                                Dim eventSig As String = "ByVal sender As System.Object, ByVal e As System.EventArgs"
                                xaml.AppendLine("  <vgp:Fonction.Param3>" & eventSig & "</vgp:Fonction.Param3>")

                                ' Param4 = Is it a function (returns value)? False for Sub/Event
                                xaml.AppendLine("  <vgp:Fonction.Param4>False</vgp:Fonction.Param4>")

                                xaml.AppendLine("  <vp:ActionWithChildren.Children_Actions>")
                                
                                ' Escape XML special characters in code
                                Dim escapedCode As String = codeBody.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("""", "&quot;").Replace("'", "&apos;")
                                
                                xaml.AppendLine("    <vgp:Commandes_VBNet>")
                                xaml.AppendLine("      <vgp:Commandes_VBNet.Param1>" & escapedCode & "</vgp:Commandes_VBNet.Param1>")
                                xaml.AppendLine("      <vgp:Commandes_VBNet.DisplayName>AI Code</vgp:Commandes_VBNet.DisplayName>")
                                xaml.AppendLine("    </vgp:Commandes_VBNet>")
                                
                                xaml.AppendLine("  </vp:ActionWithChildren.Children_Actions>")
                                xaml.AppendLine("</vgp:Fonction>")

                                ' 1. Add to the document file model (Backup)
                                If doc.File.Functions Is Nothing Then doc.File.Functions = New List(Of String)()
                                doc.File.Functions.Add(xaml.ToString())

                                ' 2. DYNAMIC UI UPDATE: Create the visual tab for the designer
                                ' This MUST be done for the compiler and user to see it immediately.
                                Try
                                    Dim editor As New DocEditeurFonctionsUserControl()
                                    editor.Dock = System.Windows.Forms.DockStyle.Fill
                                    editor.IsWindow = True
                                    editor.WorkflowDesigne = New Global.SoftwareZator.WorkflowDesigner() 
                                    editor.DebuggerService = editor.WorkflowDesigne.DebugManagerView
                                    
                                    For i = 0 To 50 - 1
                                       editor.TempXAMLFileName &= Mid("abcdefghijklmnopqrstuvwxyz1234567890", Int(Rnd() * Len("abcdefghijklmnopqrstuvwxyz1234567890")) + 1, 1)
                                    Next
                                    editor.TempXAMLFileName &= ".xaml"
                                    
                                    Dim tempPath As String = System.Windows.Forms.Application.StartupPath & "\Temp\Functions\" & editor.TempXAMLFileName
                                    My.Computer.FileSystem.WriteAllText(tempPath, xaml.ToString(), False)
                                    
                                    editor.WorkflowDesigne.Load(tempPath)
                                    
                                    Dim CloseButton As New VelerSoftware.Design.Toolkit.ButtonSpecAny
                                    CloseButton.Type = VelerSoftware.Design.Toolkit.PaletteButtonSpecStyle.Close
                                    AddHandler CloseButton.Click, AddressOf doc.CloseButton_Click
                                    
                                    Dim Tab2 As New VelerSoftware.Design.Navigator.KryptonPage
                                    DirectCast(Tab2, System.ComponentModel.ISupportInitialize).BeginInit()
                                    Tab2.AutoHiddenSlideSize = New System.Drawing.Size(200, 200)
                                    Tab2.ButtonSpecs.Add(CloseButton)
                                    Tab2.Cursor = System.Windows.Forms.Cursors.Default
                                    Tab2.Flags = 65534
                                    Tab2.LastVisibleSet = True
                                    Tab2.MinimumSize = New System.Drawing.Size(50, 50)
                                    Tab2.Name = subName
                                    Tab2.UniqueName = subName
                                    Tab2.Text = subName
                                    Tab2.Controls.Add(editor)
                                    DirectCast(Tab2, System.ComponentModel.ISupportInitialize).EndInit()
                                    
                                    doc.KryptonNavigator2.Pages.Add(Tab2)
                                    AddHandler editor.SelectedActionChanger, AddressOf doc.SelectedActionChanged
                                    
                                Catch ex As Exception
                                    System.Windows.Forms.MessageBox.Show("Error adding AI Function to UI: " & ex.Message)
                                End Try

                                ' Mark as modified
                                doc.Modifier = True
                                
                            Else
                                ' Append to existing function? Too complex for now, just skip or notify.
                                ' MessageBox.Show("Function " & subName & " already exists. Skipping.", "AI Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
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

    Private Shared Sub WaitForDesignerLoad(Optional ByVal formName As String = Nothing)
        ' Wait until the form is loaded/created
        For i As Integer = 0 To 50 ' Wait up to 10 seconds
            Application.DoEvents()
            Threading.Thread.Sleep(200)

            If String.IsNullOrEmpty(formName) Then
                ' Old Logic: Wait for Active Page
                If Form1.KryptonDockableWorkspace1.ActivePage IsNot Nothing AndAlso 
                   Form1.KryptonDockableWorkspace1.ActivePage.Controls.Count > 0 Then
                    Dim doc As DocConcepteurFenetre = TryCast(Form1.KryptonDockableWorkspace1.ActivePage.Controls(0), DocConcepteurFenetre)
                    If doc IsNot Nothing Then
                         Exit Sub
                    End If
                End If
            Else
                ' New Logic: Search for specific page
                For Each page As VelerSoftware.Design.Navigator.KryptonPage In Form1.KryptonDockingManager1.Pages
                     If page.Text = formName OrElse page.Text = formName & ".szw" Then
                          If page.Controls.Count > 0 AndAlso TypeOf page.Controls(0) Is DocConcepteurFenetre Then
                              ' Found it!
                              Exit Sub
                          End If
                     End If
                Next
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
                        
                        ' Name is already set by CreateComponent if provided

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
