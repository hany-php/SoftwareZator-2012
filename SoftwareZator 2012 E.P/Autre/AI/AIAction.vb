''' *****************************************************************************
''' 
'''  © Veler Software 2012 - 2026. All rights reserved.
'''  AI Action structure for SoftwareZator
''' 
''' *****************************************************************************

Public Class AIAction
    Public Property ActionType As String ' e.g., "ADD_CONTROL", "SET_PROPERTY", "WRITE_CODE", "OPEN_FORM"
    Public Property Target As String     ' e.g., "Button1", "Form1"
    Public Property Value As String      ' e.g., "Hello World", "Blue"
    Public Property Parameters As New Dictionary(Of String, String)
End Class
