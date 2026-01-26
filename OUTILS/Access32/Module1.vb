''' *****************************************************************************
''' 
'''  © Veler Software 2012. All rights reserved.
'''  The current code and the associated software are the proprietary 
'''  information of Etienne Baudoux from Veler Software and are
'''  supplied subject to licence terms.
''' 
'''  www.velersoftware.com
''' *****************************************************************************

Imports System.Windows.Forms

Module Module1

    Enum Action
        Create_DataBase
        Create_Table
        Delete_Table
        Get_Table_List
        Get_Data_Table
        Update_Data_Table
        TestConnect
        RenameTable
    End Enum

    Sub Main()
        Process.GetCurrentProcess.PriorityClass = ProcessPriorityClass.High

        Dim command As String = Nothing
        Dim value As String = Nothing
        Dim db As String = Nothing
        Dim pswd As String = Nothing
        Dim table_name As String = Nothing
        Dim file As String = Nothing
        Dim columns As New Generic.List(Of String)
        Dim types As New Generic.List(Of String)

        Dim commands As String() = System.Environment.GetCommandLineArgs
        Dim action As Action = 0

        If commands.Count = 1 Then
            Console.WriteLine(My.Application.Info.CompanyName & " - " & My.Application.Info.Title & " (" & My.Application.Info.Version.ToString & ") " & My.Application.Info.Copyright)
            Console.ReadKey()
            End
        End If

        For i As Integer = 1 To commands.Count - 1
            command = commands(i)
            If command.Contains("=") Then
                value = command.Split("=")(1)
                Select Case command.Split("=")(0)
                    Case "db"
                        db = value

                    Case "pswd"
                        pswd = value

                    Case "createdb"
                        db = value
                        action = Module1.Action.Create_DataBase

                    Case "createtable"
                        If value.Contains(";") Then
                            table_name = value.Split(";")(0)
                            If value.Contains(",") Then
                                For Each strr As String In value.Split(";")(1).Split(",")
                                    columns.Add(strr.Split("|")(0))
                                    types.Add(strr.Split("|")(1))
                                Next
                            End If
                            action = Module1.Action.Create_Table
                        End If

                    Case "deletetable"
                        table_name = value
                        action = Module1.Action.Delete_Table

                    Case "renametable"
                        table_name = value
                        action = Module1.Action.RenameTable

                    Case "getdatatable"
                        table_name = value
                        action = Module1.Action.Get_Data_Table

                    Case "updatedatatable"
                        table_name = value
                        action = Module1.Action.Update_Data_Table

                    Case "file"
                        file = value
                End Select
            Else
                Select Case command

                    Case "gettablelist"
                        action = Module1.Action.Get_Table_List

                    Case "testconnect"
                        action = Module1.Action.TestConnect
                End Select
            End If
        Next

        Select Case action

            Case Module1.Action.TestConnect
                Console.WriteLine(Connect(db, pswd))
                Disconnect()

            Case Module1.Action.Create_DataBase
                Console.WriteLine(CreateNewDataBase(db, pswd))

            Case Module1.Action.Create_Table
                Connect(db, pswd)
                Console.WriteLine(CreateNewTable(table_name, columns, types))
                Disconnect()

            Case Module1.Action.Delete_Table
                Connect(db, pswd)
                Console.WriteLine(DeleteTable(table_name))
                Disconnect()

            Case Module1.Action.RenameTable
                Connect(db, pswd)
                Console.WriteLine(RenameTable(table_name.Split("|")(0), table_name.Split("|")(1)))
                Disconnect()

            Case Module1.Action.Get_Table_List
                Connect(db, pswd)
                Dim result As String = ""
                For Each strr In GetTableList()
                    result &= strr & "|"
                Next
                Console.WriteLine(result.TrimEnd("|"))
                Disconnect()

            Case Module1.Action.Get_Data_Table
                If Not My.Computer.FileSystem.DirectoryExists(My.Application.Info.DirectoryPath & "\Temp\") Then
                    My.Computer.FileSystem.CreateDirectory(My.Application.Info.DirectoryPath & "\Temp")
                End If
                If My.Computer.FileSystem.FileExists(My.Application.Info.DirectoryPath & "\Temp\datatable") Then
                    My.Computer.FileSystem.DeleteFile(My.Application.Info.DirectoryPath & "\Temp\datatable", FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.DeletePermanently)
                End If
                If Not My.Computer.FileSystem.FileExists(My.Application.Info.DirectoryPath & "\Temp\datatable") Then
                    Connect(db, pswd)
                    Dim dat As System.Data.DataTable = GetDataTable(table_name)
                    Dim myFileStream As IO.Stream = IO.File.Create(My.Application.Info.DirectoryPath & "\Temp\datatable")
                    Dim serializer As New Runtime.Serialization.Formatters.Binary.BinaryFormatter
                    serializer.Serialize(myFileStream, dat)
                    myFileStream.Close()
                    Console.WriteLine(My.Application.Info.DirectoryPath & "\Temp\datatable")
                    Disconnect()
                End If

            Case Module1.Action.Update_Data_Table
                If My.Computer.FileSystem.FileExists(file) Then
                    Connect(db, pswd)
                    Dim myFileStream As IO.Stream = IO.File.OpenRead(file)
                    Dim deserializer As Runtime.Serialization.Formatters.Binary.BinaryFormatter = New Runtime.Serialization.Formatters.Binary.BinaryFormatter()
                    Console.WriteLine(UpdateDataTable(CType(deserializer.Deserialize(myFileStream), System.Data.DataTable), table_name))
                    myFileStream.Close()
                    Disconnect()
                    My.Computer.FileSystem.DeleteFile(file, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.DeletePermanently)
                End If
        End Select
    End Sub













    'Declare the connection
    Private Cnx As System.Data.OleDb.OleDbConnection

#Region "Connexion"

    ' Connect to a database 
    ' Parameters :
    ' NomFichierMDB = name of the .mdb file to read
    ' Return value :
    ' -1 = unknown error
    ' 0 = database does not exist
    ' 1 = connected to database
    ' 2 = database found but unable to connect to it
    Private Function Connect(ByVal NomFichierMDB As String, ByVal Password As String) As Integer
        Try
            If My.Computer.FileSystem.FileExists(NomFichierMDB) Then
                Disconnect() ' disconnect
                Cnx = New System.Data.OleDb.OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & NomFichierMDB & ";Jet OLEDB:Database Password=" & Password)
                Cnx.Open()
                If Cnx.State = ConnectionState.Open Then
                    Return 1
                Else
                    Return 2
                End If
            Else
                Return 0
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error opening the database : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Disconnect from a database
    ' Return value :
    ' -1 = unknown error
    ' 0 = unable to disconnect for an unknown reason
    ' 1 = disconnected from database
    Private Function Disconnect() As Integer
        Try
            If Cnx IsNot Nothing AndAlso Cnx.State = ConnectionState.Open Then
                Cnx.Close()
                Cnx.Dispose()
            End If
            If Cnx IsNot Nothing Then
                If Cnx.State = ConnectionState.Closed Then
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return 1
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error closing the database : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Determines if connected to a database or not.
    ' Return value :
    ' -1 = unknown error
    ' 0 = disconnected
    ' 1 = connected
    ' 2 = Broken
    ' 3 = Connecting
    ' 4 = Executing
    ' 5 = Fetching
    Private Function GetConnectStatus() As Integer
        Try
            If Cnx IsNot Nothing Then
                Select Case Cnx.State
                    Case ConnectionState.Broken
                        Return 2
                    Case ConnectionState.Closed
                        Return 0
                    Case ConnectionState.Connecting
                        Return 3
                    Case ConnectionState.Executing
                        Return 4
                    Case ConnectionState.Fetching
                        Return 5
                    Case ConnectionState.Open
                        Return 1
                    Case Else
                        Return 0
                End Select
            Else
                Return 0
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error determine database's status : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

#End Region

#Region "General"

    ' Create a new database
    ' Parameters :
    ' NomFichierMDB = name of the .mdb file to create
    ' Return value :
    ' -1 = unknown error
    ' 0 = database already exists
    ' 1 = database created
    Private Function CreateNewDataBase(ByVal NomFichierMDB As String, ByVal Password As String) As Integer
        Try
            If Not My.Computer.FileSystem.FileExists(NomFichierMDB) Then
                Dim cat As New ADOX.Catalog
                cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & NomFichierMDB & ";Jet OLEDB:Database Password=" & Password)
                cat.ActiveConnection.Close()
                cat = Nothing
                Return 1
            Else
                Return 0
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error creating the database : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Execute a request 
    ' Parameters :
    ' request = request to execute
    ' Return value :
    ' list of objects
    Private Function ExecuteRequest(ByVal request As String) As System.Collections.Generic.List(Of Object)
        Dim resultat As New System.Collections.Generic.List(Of Object)
        Try
            If GetConnectStatus() = 1 Then
                Using cmd As New System.Data.OleDb.OleDbCommand(request, Cnx)
                    Dim reader As System.Data.OleDb.OleDbDataReader = cmd.ExecuteReader()
                    While (reader.Read())
                        resultat.Add(reader.GetValue(0))
                    End While
                    reader.Close()
                End Using
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error executing request : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Return resultat
    End Function

#End Region

#Region "Table"

    ' Gets the name of all Tables in the database.
    ' Return value :
    ' list of text values
    Private Function GetTableList() As Generic.List(Of String)
        Dim resultat As New Generic.List(Of String)
        Try
            If GetConnectStatus() = 1 Then
                For Each row As System.Data.DataRow In Cnx.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, New [Object]() {Nothing, Nothing, Nothing, "TABLE"}).Rows
                    resultat.Add(row.ItemArray(2).ToString)
                Next
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error getting database's tables : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Return resultat
    End Function

    ' Create a new Table.
    ' Return value :
    ' -1 = unknown error
    ' 0 = table already exists
    ' 1 = table created
    Private Function CreateNewTable(ByVal table_name As String, ByVal columns As Generic.List(Of String), ByVal types As Generic.List(Of String)) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If Not GetTableList.Contains(table_name) Then
                    Dim param As New System.Text.StringBuilder
                    For i As Integer = 0 To columns.Count - 1
                        param.Append(columns(i) & " " & types(i) & ", ")
                    Next

                    If Not param.ToString = Nothing Then
                        Using cmd As New System.Data.OleDb.OleDbCommand("CREATE TABLE " & table_name & "(Id integer PRIMARY KEY, " & param.ToString.TrimEnd(" ").TrimEnd(",") & ");", Cnx)
                            cmd.ExecuteReader().Close() ' execute query
                        End Using
                    Else
                        Using cmd As New System.Data.OleDb.OleDbCommand("CREATE TABLE " & table_name & "(Id integer PRIMARY KEY);", Cnx)
                            cmd.ExecuteReader().Close() ' execute query
                        End Using
                    End If
                    param.Clear()
                    param = Nothing
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return -1
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error creating table : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Delete a Table.
    ' Return value :
    ' -1 = unknown error
    ' 0 = table does not exist
    ' 1 = table deleted
    Private Function DeleteTable(ByVal table_name As String) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If GetTableList.Contains(table_name) Then
                    Using cmd As New System.Data.OleDb.OleDbCommand("DROP TABLE " & table_name & ";", Cnx)
                        cmd.ExecuteReader().Close() ' exécution de la requête
                    End Using
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return -1
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error deleting table : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Rename a Table.
    ' Return value :
    ' -1 = unknown error
    ' 0 = table does not exist
    ' 1 = table renamed
    ' 2 = new name is already used by another table
    Private Function RenameTable(ByVal table_name As String, ByVal new_name As String) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If GetTableList.Contains(table_name) Then
                    If Not GetTableList.Contains(new_name) Then
                        Using cmd As New System.Data.OleDb.OleDbCommand("SELECT * INTO " & new_name & " FROM " & table_name & ";", Cnx)
                            cmd.ExecuteReader().Close() ' execute query
                        End Using
                        Using cmd As New System.Data.OleDb.OleDbCommand("DROP TABLE " & table_name & ";", Cnx)
                            cmd.ExecuteReader().Close() ' execute query
                        End Using
                        Return 1
                    Else
                        Return 2
                    End If
                Else
                    Return 0
                End If
                Else
                    Return -1
                End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error renaming table : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

#End Region

#Region "Valeurs"

    ' Get the number of rows in a Table.
    ' Return value :
    ' -1 = unknown error
    ' -2 = table does not exist
    ' other = number of rows in the table
    Private Function GetRowsCountTable(ByVal table_name As String) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If GetTableList.Contains(table_name) Then
                    Using cmd As New System.Data.OleDb.OleDbCommand("SELECT COUNT(Id) FROM " & table_name & ";", Cnx)
                        Dim reader As System.Data.OleDb.OleDbDataReader = cmd.ExecuteReader()
                        While (reader.Read())
                            Return reader.GetValue(0)
                            Exit While
                        End While
                        reader.Close()
                    End Using
                    Return 0
                Else
                    Return -2
                End If
            Else
                Return -1
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error getting number of rows in the table : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Add a row to a Table.
    ' Parameters :
    ' table_name = name of the table
    ' index = index at which the row will be added
    ' values = values to put in the row
    ' Return value :
    ' -1 = unknown error
    ' 0 = table does not exist
    ' 1 = row successfully added
    Private Function AddRowTable(ByVal table_name As String, ByVal index As Integer, ByVal values As Generic.List(Of Object)) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If GetTableList.Contains(table_name) Then
                    Dim param As New System.Text.StringBuilder
                    For Each Val As Object In values
                        If TypeOf Val Is String Then
                            param.Append("'" & CStr(Val) & "', ")
                        Else
                            param.Append(Val & ", ")
                        End If
                    Next

                    Using cmd As New System.Data.OleDb.OleDbCommand("INSERT INTO " & table_name & " VALUES (" & index & ", " & param.ToString.TrimEnd(" ").TrimEnd(",") & ");", Cnx)
                        cmd.ExecuteReader().Close()
                    End Using

                    param.Clear()
                    param = Nothing
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return -1
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Unable to add a row to the table " & table_name & " : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Change a row in a Table.
    ' Parameters :
    ' table_name = name of the table
    ' index = index at which the row will be modified
    ' column_name = name of the column to modify
    ' value = values to put in the column
    ' Return value :
    ' -1 = unknown error
    ' 0 = table does not exist
    ' 1 = row successfully modified
    Private Function ChangeRowTable(ByVal table_name As String, ByVal index As Integer, ByVal column_name As String, ByVal value As Object) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If GetTableList.Contains(table_name) Then
                    If TypeOf value Is String Then
                        Using cmd As New System.Data.OleDb.OleDbCommand("UPDATE " & table_name & " SET [" & column_name & "] = '" & value & "' WHERE Id = " & index & ";", Cnx)
                            cmd.ExecuteReader().Close()
                        End Using
                    Else
                        Using cmd As New System.Data.OleDb.OleDbCommand("UPDATE " & table_name & " SET [" & column_name & "] = " & value & " WHERE Id = " & index & ";", Cnx)
                            cmd.ExecuteReader().Close()
                        End Using
                    End If
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return -1
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Unable to change a row to the table " & table_name & " : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Delete a row in a Table.
    ' Parameters :
    ' table_name = name of the table
    ' index = index at which the row will be deleted
    ' Return value :
    ' -1 = unknown error
    ' 0 = table does not exist
    ' 1 = row successfully deleted
    Private Function DeleteRowTable(ByVal table_name As String, ByVal index As Integer) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If GetTableList.Contains(table_name) Then
                    Using cmd As New System.Data.OleDb.OleDbCommand("DELETE FROM " & table_name & " WHERE Id = " & index & ";", Cnx)
                        cmd.ExecuteReader().Close()
                    End Using
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return -1
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Unable to delete a row to the table " & table_name & " : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Get a single value in a Row.
    ' Parameters :
    ' table_name = name of the table concerned
    ' column_name = name of the column where the value is located
    ' index (optional) = index of the value in the table
    ' condition (optional) = if index is unknown, can specify e.g. "the row where Phone field = 0671665378"
    ' Return value :
    ' object
    Private Function GetSingleValueOfRow(ByVal table_name As String, ByVal column_name As String, Optional ByVal index As Integer = -1, Optional ByVal condition As String = Nothing) As Object
        Try
            If GetConnectStatus() = 1 AndAlso GetTableList.Contains(table_name) Then
                If Not index = -1 Then
                    Using cmd As New System.Data.OleDb.OleDbCommand("SELECT [" & column_name & "] FROM " & table_name & " WHERE Id = " & index & ";", Cnx)
                        Dim reader As System.Data.OleDb.OleDbDataReader = cmd.ExecuteReader()
                        While (reader.Read())
                            Return reader.GetValue(0)
                            Exit While
                        End While
                        reader.Close()
                    End Using
                ElseIf Not condition = Nothing Then
                    Using cmd As New System.Data.OleDb.OleDbCommand("SELECT [" & column_name & "] FROM " & table_name & " WHERE " & condition & ";", Cnx)
                        Dim reader As System.Data.OleDb.OleDbDataReader = cmd.ExecuteReader()
                        While (reader.Read())
                            Return reader.GetValue(0)
                            Exit While
                        End While
                        reader.Close()
                    End Using
                End If
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error getting database's tables : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Return Nothing
    End Function

    ' Get values in a Row.
    ' Parameters :
    ' table_name = name of the table concerned
    ' column_name = name of the column where the value is located
    ' index (optional) = index of the value in the table
    ' condition (optional) = if index is unknown, can specify e.g. "the row where Phone field = 0671665378"
    ' Return value :
    ' list of objects
    Private Function GetValuesOfRows(ByVal table_name As String, ByVal column_name As String, ByVal condition As String) As System.Collections.Generic.List(Of Object)
        Dim resultat As New System.Collections.Generic.List(Of Object)
        Try
            If GetConnectStatus() = 1 AndAlso GetTableList.Contains(table_name) Then
                If condition = Nothing Then
                    Using cmd As New System.Data.OleDb.OleDbCommand("SELECT [" & column_name & "] FROM " & table_name & ";", Cnx)
                        Dim reader As System.Data.OleDb.OleDbDataReader = cmd.ExecuteReader()
                        While (reader.Read())
                            resultat.Add(reader.GetValue(0))
                        End While
                        reader.Close()
                    End Using
                Else
                    Using cmd As New System.Data.OleDb.OleDbCommand("SELECT [" & column_name & "] FROM " & table_name & " WHERE " & condition & ";", Cnx)
                        Dim reader As System.Data.OleDb.OleDbDataReader = cmd.ExecuteReader()
                        While (reader.Read())
                            resultat.Add(reader.GetValue(0))
                        End While
                        reader.Close()
                    End Using
                End If
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error getting database's tables : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Return resultat
    End Function

#End Region

#Region "Colonnes"

    ' Get the name of all columns in a Table.
    ' Parameters :
    ' table_name = name of the table concerned
    ' Return value :
    ' list of text values
    Private Function GetColumnsList(ByVal table_name As String) As Generic.List(Of String)
        Dim resultat As New Generic.List(Of String)
        Try
            If GetConnectStatus() = 1 AndAlso GetTableList.Contains(table_name) Then
                For Each col As System.Data.DataColumn In GetDataTable(table_name).Columns
                    resultat.Add(col.ColumnName)
                Next
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error getting database's tables : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Return resultat
    End Function

    ' Get the number of columns in a Table.
    ' Parameters :
    ' table_name = name of the table concerned
    ' Return value :
    ' -1 = unknown error
    ' -2 = table does not exist
    ' other = number of columns in the table
    Private Function GetColumnsCountTable(ByVal table_name As String) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If GetTableList.Contains(table_name) Then
                    Return GetDataTable(table_name).Columns.Count
                Else
                    Return -2
                End If
            Else
                Return -1
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Error getting number of columns in the table : " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

    ' Add a column to a Table.
    ' Parameters :
    ' table_name = name of the table
    ' column_name = name of the column to add
    ' typed = type of value for this column
    ' Return value :
    ' -1 = unknown error
    ' 0 = table does not exist
    ' 1 = column successfully added
    Private Function AddColumnTable(ByVal table_name As String, ByVal column_name As String, ByVal typed As String) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If GetTableList.Contains(table_name) Then
                    Using cmd As New System.Data.OleDb.OleDbCommand("ALTER TABLE " & table_name & " ADD COLUMN [" & column_name & "] " & typed & ";", Cnx)
                        cmd.ExecuteReader().Close()
                    End Using
                    Return 1
                Else
                    Return 0
                End If
            Else
                Return -1
            End If
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Unable to add a row to the table " & table_name & " : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

#End Region

#Region "DataTable"

    ' Get the DataTable from a table.
    ' Parameters :
    ' table_name = name of the table
    ' Return value :
    ' value of type System.Data.DataTable
    Private Function GetDataTable(ByVal table_name As String) As System.Data.DataTable
        Dim resultat As New System.Data.DataTable
        Try
            If GetConnectStatus() = 1 AndAlso GetTableList.Contains(table_name) Then
                Using da As New System.Data.OleDb.OleDbDataAdapter("SELECT * FROM " & table_name, Cnx)
                    Using cb As New System.Data.OleDb.OleDbCommandBuilder(da)
                        Using dst As New System.Data.DataSet
                            da.Fill(resultat)
                        End Using
                    End Using
                End Using
            End If
        Catch ex As System.Data.OleDb.OleDbException
            System.Windows.Forms.MessageBox.Show("Unable to get the DataTable from the table " & table_name & " : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Unable to get the DataTable from the table " & table_name & " : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Return resultat
    End Function

    ' Get the DataTable from a table.
    ' Parameters :
    ' table_name = name of the table
    ' Return value :
    ' -1 = unknown error
    ' 0 = Table update not performed because no table was found
    ' 1 = update successfully performed
    ' 2 = there is nothing to update
    Private Function UpdateDataTable(ByVal dat As System.Data.DataTable, ByVal old_table_name As String) As Integer
        Try
            If GetConnectStatus() = 1 Then
                If Not dat Is Nothing AndAlso GetTableList.Contains(old_table_name) Then
                    Dim changes As System.Data.DataTable = dat.GetChanges()
                    If Not changes Is Nothing Then
                        Using da As New System.Data.OleDb.OleDbDataAdapter("SELECT * FROM " + old_table_name, Cnx)
                            Using cb As New System.Data.OleDb.OleDbCommandBuilder(da)
                                da.Update(changes)
                                dat.AcceptChanges()
                            End Using
                        End Using
                        Return 1
                    Else
                        Return 2
                    End If
                Else
                    Return 0
                End If
            Else
                Return -1
            End If
        Catch ex As System.Data.OleDb.OleDbException
            System.Windows.Forms.MessageBox.Show("Unable to update the table " & dat.TableName & " : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        Catch ex As System.Exception
            System.Windows.Forms.MessageBox.Show("Unable to update the table " & dat.TableName & " : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return -1
        End Try
    End Function

#End Region

End Module
