Imports Csu.Modsim.ModsimIO
Imports Csu.Modsim.ModsimModel
Imports System.Runtime.InteropServices
Imports System

Module CoupledGSFLOW
    <DllImport("C:\Users\trianae\MyLocalCode\USGS\gsflow.git\msds\gsflow_develop_f\x64\Debug\CPPStaticLIBWrapper.dll", EntryPoint:="call_modules2", ExactSpelling:=False, CallingConvention:=CallingConvention.Cdecl)>
    Public Function call_modules(ByVal a As Integer, ByVal b As Integer) As Integer
    End Function

    Dim myModel As New Model
    Sub Main(ByVal CmdArgs() As String)
        Dim val1 As Int32
        Dim val2 As Int32
        Dim answer As Int32

        val1 = 3
        val2 = 4

        answer = call_modules(val1, val2)

        MsgBox(answer)

        Dim Num As Integer = 8
        Dim Message As String = "Harun"
        call_modules(Message, 1)
        'ReturnInParam(Num, Message)
        Console.Write(Message)

        Dim FileName As String = CmdArgs(0)
        AddHandler myModel.Init, AddressOf OnInitialize
        AddHandler myModel.IterBottom, AddressOf OnIterationBottom
        AddHandler myModel.IterTop, AddressOf OnIterationTop
        AddHandler myModel.Converged, AddressOf OnIterationConverge
        AddHandler myModel.End, AddressOf OnFinished
        AddHandler myModel.OnMessage, AddressOf OnMessage
        AddHandler myModel.OnModsimError, AddressOf OnMessage

        XYFileReader.Read(myModel, FileName)
        Modsim.RunSolver(myModel)
        Console.ReadLine()
    End Sub

    Private Sub OnInitialize()

    End Sub

    Private Sub OnIterationTop()

    End Sub

    Private Sub OnMessage(ByVal message As String)
        Console.WriteLine(message)
    End Sub

    Private Sub OnIterationBottom()

    End Sub

    Private Sub OnIterationConverge()

    End Sub

    Private Sub OnFinished()

    End Sub
    <DllImport("mmf.dll", CallingConvention:=CallingConvention.Cdecl)>
    Private Sub ReturnInParam(ByRef Stan As Integer, ByRef message As String)

    End Sub
End Module
