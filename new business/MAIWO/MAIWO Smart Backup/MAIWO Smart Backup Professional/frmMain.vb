﻿Imports System.Threading
Imports System.IO
Imports System.Globalization
Imports Microsoft.Win32
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.ComponentModel
Imports System.Windows.Forms

Public Class frmMain
    '读ini API函数
    Private Declare Function GetPrivateProfileString Lib "kernel32" Alias "GetPrivateProfileStringA" (ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal lpDefault As String, ByVal lpReturnedString As String, ByVal nSize As Int32, ByVal lpFileName As String) As Int32
    '写ini API函数
    Private Declare Function WritePrivateProfileString Lib "kernel32" Alias "WritePrivateProfileStringA" (ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal lpString As String, ByVal lpFileName As String) As Int32

    Private CopyThread As Thread '拷贝线程
    Private CountThread As Thread '统计线程
    Private _totalFiles As Long
    Private _totalFolders As Long
    Private _copiedFiles As Long

    Private _strFilter As New String("") '文件类型筛选字符串，用“|”分隔
    Private _strTimer As String '定时设定字符串
    Private _strTimerType As String '定时类型："byWeek"，"byDate"，"byIntarvel"
    Private _nowLanguag As String   '记录当前语言

    Private _minuteNextOnce As Long '当定时类型为间隔时，计算下次备份时间。每次备份后初始化为0


    Public Property FileFilter As String
        Get
            Return _strFilter
        End Get
        Set(ByVal value As String)
            _strFilter = value
        End Set
    End Property

    Public Property TimerSet As String
        Get
            Return _strTimer
        End Get
        Set(ByVal value As String)
            _strTimer = value
        End Set
    End Property

    Public Property TimerType As String
        Get
            Return _strTimerType
        End Get
        Set(ByVal value As String)
            _strTimerType = value
        End Set
    End Property

    Public Property NowLanguage As String
        Get
            Return _nowLanguag
        End Get
        Set(ByVal value As String)
            _nowLanguag = value
        End Set
    End Property

    Private Sub tsbAddTask_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbAddTask.Click
        Select Case _nowLanguag
            Case "zh-Hans"
                frmEditTask.Text = "添加任务"
            Case "zh-Hant"
                frmEditTask.Text = "添加任務"
            Case "en"
                frmEditTask.Text = "Add Task"
        End Select

        frmEditTask.myType = "Add"
        frmEditTask.ShowDialog()
    End Sub

    Private Sub tsbAlterTask_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbAlterTask.Click

        Select Case _nowLanguag
            Case "zh-Hans"
                If dGrid.CurrentCellAddress.Y < 0 Then MsgBox("当前没有任务需要修改！", vbCritical) : Exit Sub
                frmEditTask.Text = "修改任务"
            Case "zh-Hant"
                If dGrid.CurrentCellAddress.Y < 0 Then MsgBox("當前沒有任務需要修改！", vbCritical) : Exit Sub
                frmEditTask.Text = "修改任務"
            Case "en"
                If dGrid.CurrentCellAddress.Y < 0 Then MsgBox("There have no task now,！", vbCritical) : Exit Sub
                frmEditTask.Text = "Change Task"
        End Select

        frmEditTask.myType = "Alter"
        frmEditTask.txtFrom.Text = dGrid(1, dGrid.CurrentCellAddress.Y).Value
        frmEditTask.txtTo.Text = dGrid(2, dGrid.CurrentCellAddress.Y).Value
        frmEditTask.ShowDialog()
    End Sub

    Private Sub frmMain_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        For Each i In frmEditTask.Controls
            i.Dispose()
        Next
        For Each i In frmSetFilter.Controls
            i.Dispose()
        Next
        For Each i In frmSetFilter_Edit.Controls
            i.Dispose()
        Next
        For Each i In frmSetTime.Controls
            i.Dispose()
        Next
        For Each i In Me.Controls
            i.Dispose()
        Next
    End Sub

    Private Sub frmMain_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        '检测是否已有实例在运行，保证同一时间只有一个实例运行
        Dim createdNew As Boolean
        Dim mutex As System.Threading.Mutex = New System.Threading.Mutex(True, "MAIWO Smart Backup Professional", createdNew) ' 创建mutex 
        If createdNew = False Then
            Application.Run(Application.OpenForms.Item("MAIWO Smart Backup Professional"))
            Exit Sub
        End If

        mutex.ReleaseMutex() ' 释放mutex    

        Dim Key1, Key2, Key3 As Microsoft.Win32.RegistryKey
        '检测软件是否已注册
        Dim _isRegSuccess As Boolean = False
        Key1 = My.Computer.Registry.CurrentUser '返回当前用户键 
        Key2 = Key1.OpenSubKey("Software", True) '返回当前用户键下的Software键,如果想创建项，必须指定第二个参数为true  
        If Key2 IsNot Nothing Then
            Key3 = Key2.OpenSubKey("MAIWO Smart Backup Professional", True)
            If Key3 IsNot Nothing Then
                If Key3.GetValue("ID") IsNot Nothing Then
                    Dim strID As String = Key3.GetValue("ID")
                    If Mid(strID, Mid(strID, 1, 1) + 1, 1) = "3" And Mid(strID, Mid(strID, 2, 1) + 1, 1) = "0" Then
                        _isRegSuccess = True
                    End If
                End If
            End If
        End If
        If _isRegSuccess = False Then MsgBox("您尚未注册本软件。请安装本软件，并在安装过程中进行注册！") : Exit Sub

        '加载当前语言
        Key1 = My.Computer.Registry.CurrentUser '返回当前用户键 
        Key2 = Key1.OpenSubKey("Software", True) '返回当前用户键下的Software键,如果想创建项，必须指定第二个参数为true  
        If Key2 IsNot Nothing Then
            Key3 = Key2.OpenSubKey("MAIWO Smart Backup Professional", True)
            If Key3 IsNot Nothing Then
                If Key3.GetValue("Language") IsNot Nothing Then
                    _nowLanguag = Key3.GetValue("Language")
                Else
                    _nowLanguag = "zh-Hans"
                End If
            Else
                _nowLanguag = "zh-Hans"
            End If
        Else
            _nowLanguag = "zh-Hans"
        End If
        Me.setLanguage()    '設置語言
        Select Case _nowLanguag
            Case "zh-Hans"
                cobSetLanguage.Text = "简体中文"
            Case "zh-Hant"
                cobSetLanguage.Text = "繁體中文"
            Case "en"
                cobSetLanguage.Text = "ENGLISH"
        End Select

        '加载任务列表
        Dim arrTaskFrom() As String, arrTaskTo() As String, _taskFrom As String, _taskTo As String
        _taskFrom = GetINI("task", "FROM", "")
        _taskTo = GetINI("task", "TO", "")
        arrTaskFrom = Split(_taskFrom, "|")
        arrTaskTo = Split(_taskTo, "|")
        If arrTaskFrom.Length > 0 Then
            For i = 0 To arrTaskFrom.Length - 1
                If arrTaskFrom(i).ToString <> "" Then
                    dGrid.Rows.Add()
                    dGrid(1, dGrid.Rows.Count - 1).Value = arrTaskFrom(i).ToString
                    dGrid(2, dGrid.Rows.Count - 1).Value = arrTaskTo(i).ToString
                End If
            Next
        End If

        '加载备份时间设定
        Dim arrCell() As String, arrWeekTime() As String, arrHourMin() As String
        Dim _chkBox As myCheckBox, _numHour As NumericUpDown, _numMin As NumericUpDown

        If GetINI("time", "byWeek", "") <> "" Then
            arrCell = Split(GetINI("time", "byWeek", ""), "|")
            For i = 0 To arrCell.Length - 1
                If arrCell(i).ToString <> "" Then
                    arrWeekTime = Split(arrCell(i).ToString, ",")
                    arrHourMin = Split(arrWeekTime(1).ToString, ":")
                    _chkBox = frmSetTime.GroupBox1.Controls("CheckBox" & arrWeekTime(0).ToString)
                    _chkBox.Checked = True
                    _numHour = frmSetTime.GroupBox1.Controls("wHour" & arrWeekTime(0).ToString)
                    _numHour.Value = CDec(arrHourMin(0))

                    _numMin = frmSetTime.GroupBox1.Controls("wMin" & arrWeekTime(0).ToString)
                    _numMin.Value = CDec(arrHourMin(1))
                End If
            Next

            TimerSet = GetINI("time", "byWeek", "")
            TimerType = "byWeek"
            frmSetTime.optTypeWeek.Checked = True
            frmSetTime.optTypeDate.Checked = False
            frmSetTime.optTypeIntarvel.Checked = False
            frmSetTime.GroupBox1.Enabled = True : frmSetTime.rctShape1.Visible = True
            frmSetTime.GroupBox2.Enabled = False : frmSetTime.rctShape2.Visible = False
            frmSetTime.GroupBox3.Enabled = False : frmSetTime.rctShape3.Visible = False
        ElseIf GetINI("time", "byDate", "") <> "" Then
            arrCell = Split(GetINI("time", "byDate", ""), ",")
            arrHourMin = Split(arrCell(1).ToString, ":")
            frmSetTime.dDate.Value = arrCell(0).ToString
            frmSetTime.dHour.Value = arrHourMin(0).ToString
            frmSetTime.dMin.Value = arrHourMin(1).ToString

            TimerSet = GetINI("time", "byDate", "")
            TimerType = "byDate"
            frmSetTime.optTypeWeek.Checked = False
            frmSetTime.optTypeDate.Checked = True
            frmSetTime.optTypeIntarvel.Checked = False
            frmSetTime.GroupBox1.Enabled = False : frmSetTime.rctShape1.Visible = False
            frmSetTime.GroupBox2.Enabled = True : frmSetTime.rctShape2.Visible = True
            frmSetTime.GroupBox3.Enabled = False : frmSetTime.rctShape3.Visible = False
        ElseIf GetINI("time", "byIntarvel", "") <> "" Then
            arrHourMin = Split(GetINI("time", "byIntarvel", ""), ":")
            frmSetTime.inHour.Value = CInt(arrHourMin(0).ToString)
            frmSetTime.inMin.Value = CInt(arrHourMin(1).ToString)

            TimerSet = GetINI("time", "byIntarvel", "")
            TimerType = "byIntarvel"
            frmSetTime.optTypeWeek.Checked = False
            frmSetTime.optTypeDate.Checked = False
            frmSetTime.optTypeIntarvel.Checked = True
            frmSetTime.GroupBox1.Enabled = False : frmSetTime.rctShape1.Visible = False
            frmSetTime.GroupBox2.Enabled = False : frmSetTime.rctShape2.Visible = False
            frmSetTime.GroupBox3.Enabled = True : frmSetTime.rctShape3.Visible = True
        End If

        '加载文件类型
        For i = 1 To 7
            _strFilter &= GetINI("Filter", "Class" & i, "")
        Next

        '加载是否设置为开机启动
        If GetINI("AutoRun", "AutoRun", "") = "True" Then tcbStartWithPC.Checked = True


        Call frmMain_Resize(sender, e)
    End Sub

    Private Sub frmMain_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        If Me.WindowState = FormWindowState.Minimized Then
            '如果窗体是最小化操作
            Me.Visible = False
            NotifyIcon1.Visible = True
        Else
            picGg.Width = 156
            picGg.Left = Me.Width - picGg.Width - 10
            picGg.Height = 276
            picGg.Top = dGrid.Top
            dGrid.Width = Me.Width - picGg.Width - 13
            dGrid.Columns(1).Width = (dGrid.Width - 60) / 2
            dGrid.Columns(2).Width = dGrid.Columns(1).Width

            lstShow.Width = Me.Width - 10
            lstShow.Height = StatusStrip1.Location.Y - lstShow.Top
        End If

    End Sub

    Private Sub tsbDeleteTask_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbDeleteTask.Click
        Dim arrRows() As Integer
        Dim arrIndex As Integer

        If dGrid.Rows.Count = 0 Then
            Select Case _nowLanguag
                Case "zh-Hans"
                    MsgBox("当前还没有任务，无法完成删除操作！", vbCritical, "错误！") : Exit Sub
                Case "zh-Hant"
                    MsgBox("當前還沒有任務，無法完成刪除操作！", vbCritical, "错误！") : Exit Sub
                Case "en"
                    MsgBox("There have no task now！", vbCritical, "Error！") : Exit Sub
            End Select
        End If

        If dGrid.CurrentCellAddress.X = 0 Then dGrid.CurrentCell = dGrid(1, dGrid.CurrentCellAddress.Y)

        arrIndex = -1
        For i = 0 To dGrid.Rows.Count - 1
            If dGrid(0, i).Value = True Then
                arrIndex += 1
                ReDim Preserve arrRows(arrIndex)
                arrRows(arrIndex) = i
            End If
        Next

        If arrIndex = -1 Then
            Select Case _nowLanguag
                Case "zh-Hans"
                    MsgBox("当前没有选择任何任务。请先选择要删除的任务！", vbCritical, "错误！") : Exit Sub
                Case "zh-Hant"
                    MsgBox("當前沒有選擇任何任務，請先選擇要刪除的任務！", vbCritical, "错误！") : Exit Sub
                Case "en"
                    MsgBox("You have not choose any task now！", vbCritical, "Error！") : Exit Sub
            End Select
        End If

        Dim tempStr As New String("")
        Select Case _nowLanguag
            Case "zh-Hans"
                tempStr = "您当前选择了" & arrIndex + 1 & "条任务准备删除，您确定要删除这些任务吗？"
            Case "zh-Hant"
                tempStr = "您當前選擇了" & arrIndex + 1 & "條任務準備刪除，您確定要刪除這些任務嗎？"
            Case "en"
                tempStr = "You have choose " & arrIndex + 1 & " tasks.Do you really want to delete them？"
        End Select
        If MsgBox(tempStr, vbQuestion & vbYesNo) = vbNo Then Exit Sub

        For i = arrIndex To 0 Step -1
            dGrid.Rows.RemoveAt(arrRows(i))
        Next

        '将修改的任务信息保存到INI文件中
        Dim fileFrom As New String(""), fileTo As New String("")
        If dGrid.Rows.Count > 0 Then
            For i = 0 To dGrid.Rows.Count - 1
                fileFrom &= dGrid(1, i).Value & "|"
                fileTo &= dGrid(2, i).Value & "|"
            Next
        End If
        WriteINI("Task", "FROM", fileFrom)
        WriteINI("Task", "TO", fileTo)

        Select Case _nowLanguag
            Case "zh-Hans"
                MsgBox("任务删除成功！", vbInformation)
            Case "zh-Hant"
                MsgBox("任務刪除成功！", vbInformation)
            Case "en"
                MsgBox("Successful！", vbInformation)
        End Select
    End Sub

    Private Sub tsbBegin_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbBegin.Click
        If dGrid.RowCount = 0 Then
            Select Case _nowLanguag
                Case "zh-Hans"
                    MsgBox("当前没有任何备份任务！", vbCritical) : Exit Sub
                Case "zh-Hant"
                    MsgBox("當前沒有任何備份任務！", vbCritical) : Exit Sub
                Case "en"
                    MsgBox("There have no any tasks now！", vbCritical) : Exit Sub
            End Select
        End If

        tsbBegin.Enabled = False
        tsbEnd.Enabled = True

        Dim isNew As Boolean
        isNew = True

        If Not IsNothing(CopyThread) Then
            isNew = False
            If CopyThread.ThreadState = 68 Then CopyThread.Resume()
        End If

        If Not IsNothing(CountThread) Then
            isNew = False
            If CountThread.ThreadState = 68 Then CountThread.Resume()
        End If

        If isNew Then
            lstShow.Items.Clear()
            beginCopy()
        Else
            tsbPause.Enabled = True
        End If


    End Sub

    Public Sub beginCopy()
        Dim taskNum As Integer = -1
        For i = 0 To dGrid.RowCount - 1
            If CType(dGrid.Rows(i).Cells(0).EditedFormattedValue, Boolean) = True Then taskNum += 1
        Next

        If taskNum = -1 Then
            Select Case NowLanguage
                Case "zh-Hans"
                    MsgBox("您还没有选择任何任务！无法进行备份！", vbCritical)
                Case "zh-Hant"
                    MsgBox("您還沒有選擇任何任務！無法進行備份！", vbCritical)
                Case "en"
                    MsgBox("You have not choose any task in the Task List！", vbCritical)
            End Select
            tsbBegin.Enabled = True
            tsbEnd.Enabled = False
            Exit Sub
        End If

        Dim fromPath(taskNum) As String
        Dim toPath(taskNum) As String
        Dim arrID As Integer = 0

        For i = 0 To dGrid.RowCount - 1
            If CType(dGrid.Rows(i).Cells(0).EditedFormattedValue, Boolean) = True Then
                fromPath(arrID) = dGrid(1, i).Value
                toPath(arrID) = dGrid(2, i).Value
                arrID += 1
            End If
        Next

        '开始拷贝
        Dim FileCopy As New CopyClass(Me, _strFilter, fromPath, toPath)
        Dim FileFolderCount As New CopyClass(Me, _strFilter, fromPath, toPath)

        '暂停计时功能，避免正在进行复制时再次执行下次任务
        Timer1.Enabled = False

        '设置并运行拷贝进程
        If IsNothing(CopyThread) Then
            CopyThread = New Thread(AddressOf FileCopy.CopyFiles) '拷贝线程

            CopyThread.Name = "Copy"
            CopyThread.IsBackground = True
            CopyThread.Start()
        Else
            CopyThread.Resume()
        End If

        '设置并运行统计进程
        _totalFiles = 0
        _totalFolders = 0

        FileFolderCount = New CopyClass(Me, _strFilter, fromPath, toPath)
        CountThread = New Thread(AddressOf FileFolderCount.GetCountData) '计数线程

        CountThread.Name = "Count"
        CountThread.IsBackground = True
        CountThread.Start()

        Select Case NowLanguage
            Case "zh-Hans"
                tssLabel.Text = "正在统计总文件数。。。。"
            Case "zh-Hant"
                tssLabel.Text = "正在統計總文件數量。。。。"
            Case "en"
                tssLabel.Text = "Counting total files "
        End Select

        tssLabel.Visible = True
        ProgressBar1.Value = 0
        ProgressBar1.Maximum = 0
    End Sub

    Private Sub tsbEnd_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbEnd.Click
        If Not IsNothing(CopyThread) Then
            If CopyThread.ThreadState = 68 Then CopyThread.Resume()
            While CopyThread.ThreadState = System.Threading.ThreadState.Running
                Dim i As Integer = 1
            End While
            CopyThread.Abort()
        End If

        If Not IsNothing(CountThread) Then
            If CountThread.ThreadState = 68 Then CountThread.Resume()
            CountThread.Abort()
        End If

        tsbBegin.Enabled = True
        tsbPause.Enabled = False
        tsbEnd.Enabled = False

        tssLabel.Visible = False
        ProgressBar1.Value = 0
        ProgressBar1.Maximum = 0

        Timer1.Enabled = False
    End Sub

    Private Sub tsbSetTime_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbSetTime.Click
        frmSetTime.ShowDialog()
    End Sub

    Private Sub tsbSetFilter_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbSetFilter.Click
        frmSetFilter.ShowDialog()
    End Sub


    Private Sub tcbStartWithPC_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tcbStartWithPC.CheckedChanged
        '找到注册表中系统启动键RUN
        On Error GoTo toError
        Dim Key1 As Microsoft.Win32.RegistryKey
        Key1 = My.Computer.Registry.LocalMachine
        Key1 = Key1.OpenSubKey("Software", True)
        If Key1 Is Nothing Then GoTo toError
        Key1 = Key1.OpenSubKey("Microsoft", True)
        If Key1 Is Nothing Then GoTo toError
        Key1 = Key1.OpenSubKey("Windows", True)
        If Key1 Is Nothing Then GoTo toError
        Key1 = Key1.OpenSubKey("CurrentVersion", True)
        If Key1 Is Nothing Then GoTo toError
        Key1 = Key1.OpenSubKey("Run", True)
        If Key1 Is Nothing Then GoTo toError

        If tcbStartWithPC.Checked Then
            '如果设置为开机启动
            Key1.SetValue("MAIWO SmartBackup", Application.StartupPath & "\MAIWO Smart Backup Professional.exe")
            WriteINI("AutoRun", "AutoRun", "True")
        Else
            '如果设置为不开机启动
            Key1.DeleteValue("MAIWO SmartBackup", False)
            WriteINI("AutoRun", "AutoRun", "False")
        End If

        Exit Sub
toError:
        Select Case _nowLanguag
            Case "zh-Hans"
                MsgBox("设置失败", vbCritical)
            Case "zh-Hant"
                MsgBox("設置失敗", vbCritical)
            Case "en"
                MsgBox("Error", vbCritical)
        End Select
    End Sub

    Private Sub NotifyIcon1_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles NotifyIcon1.Click
        Me.Visible = True
        NotifyIcon1.Visible = False
        Me.WindowState = FormWindowState.Normal
    End Sub

    Private Sub tsbAbout_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbAbout.Click
        AboutBox1.ShowDialog()
    End Sub

    '读取ini文件内容
    Public Function GetINI(ByVal Section As String, ByVal AppName As String, ByVal lpDefault As String) As String
        Dim Str As String = LSet(Str, 256)
        GetPrivateProfileString(Section, AppName, lpDefault, Str, Len(Str), Application.StartupPath & "\MYINFO.INI")
        Return Microsoft.VisualBasic.Left(Str, InStr(Str, Chr(0)) - 1)
    End Function

    '写ini文件操作
    Public Function WriteINI(ByVal Section As String, ByVal AppName As String, ByVal lpDefault As String) As Long
        WriteINI = WritePrivateProfileString(Section, AppName, lpDefault, Application.StartupPath & "\MYINFO.INI")
    End Function

    '复制进程的消息
    Public Sub CopyThreadMessage(ByVal ThreadName As String, ByVal CopiedFiles As Long, ByVal Message As String)

        ' 如果复制结束
        If Message = "END" Then
            ProgressBar1.Value = ProgressBar1.Maximum

            '寻找最近的下次备份时间点
            Dim arrCell() As String, arrWeekTime() As String, arrHourMin() As String
            Dim nextTime As New String("")

            Select Case TimerType
                Case "byWeek"
                    arrCell = Split(_strTimer, "|")
                    Dim maxWeek, minWeek, minID, maxID As Integer
                    arrWeekTime = Split(arrCell(0).ToString, ",")
                    maxWeek = CInt(arrWeekTime(0)) : maxID = 0
                    minWeek = CInt(arrWeekTime(0)) : minID = 0
                    For i = 1 To arrCell.Length - 1
                        If arrCell(i).ToString <> "" Then
                            arrWeekTime = Split(arrCell(i).ToString, ",")
                            If CInt(arrWeekTime(0)) > maxWeek Then maxWeek = CInt(arrWeekTime(0)) : maxID = i
                            If CInt(arrWeekTime(0)) < minWeek Then minWeek = CInt(arrWeekTime(0)) : minID = i
                        End If
                    Next

                    Select Case NowLanguage
                        Case "zh-Hans"
                            Dim arrZh As String() = New String() {"星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六"}
                            If CInt(Date.Today.DayOfWeek) < maxWeek Then
                                arrWeekTime = Split(arrCell(maxID).ToString, ",")
                                nextTime = "上次备份于" & Date.Now & "成功完成，下次备份将于：" & arrZh(maxWeek) & " " & arrWeekTime(1) & " 进行"
                            Else
                                arrWeekTime = Split(arrCell(minID).ToString, ",")
                                nextTime = "上次备份于" & Date.Now & "成功完成，下次备份将于：" & arrZh(minWeek) & " " & arrWeekTime(1) & " 进行"
                            End If
                        Case "zh-Hant"
                            Dim arrZh As String() = New String() {"星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六"}
                            If CInt(Date.Today.DayOfWeek) < maxWeek Then
                                arrWeekTime = Split(arrCell(maxID).ToString, ",")
                                nextTime = "上次備份與" & Date.Now & "成功完成，下次備份將于：" & arrZh(maxWeek) & " " & arrWeekTime(1) & " 進行"
                            Else
                                arrWeekTime = Split(arrCell(minID).ToString, ",")
                                nextTime = "上次備份與" & Date.Now & "成功完成，下次備份將于：" & arrZh(minWeek) & " " & arrWeekTime(1) & " 進行"
                            End If
                        Case "en"
                            Dim arrEn As String() = New String() {"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"}
                            If CInt(Date.Today.DayOfWeek) < maxWeek Then
                                arrWeekTime = Split(arrCell(maxID).ToString, ",")
                                nextTime = "Last backup completed at " & Date.Now & ",next once will start at " & arrEn(maxWeek) & " " & arrWeekTime(1)
                            Else
                                arrWeekTime = Split(arrCell(minID).ToString, ",")
                                nextTime = "Last backup completed at " & Date.Now & ",next once will start at " & arrEn(minWeek) & " " & arrWeekTime(1)
                            End If
                    End Select


                    tssLabel.Text = nextTime
                    tsbPause.Enabled = False
                    Timer1.Enabled = True
                Case "byDate"
                    '指定日期备份，只运行一次，不再运行
                    Timer1.Enabled = False
                    Select Case NowLanguage
                        Case "zh-Hans"
                            tssLabel.Text = "上次备份于" & Date.Now & "成功完成"
                        Case "zh-Hant"
                            tssLabel.Text = "上次備份與" & Date.Now & "成功完成"
                        Case "en"
                            tssLabel.Text = "Last backup completed at " & Date.Now
                    End Select

                    tsbBegin.Enabled = True
                    tsbPause.Enabled = False
                    tsbEnd.Enabled = False
                    Timer1.Enabled = True
                Case "byIntarvel"
                    arrHourMin = Split(_strTimer, ":")
                    Select Case NowLanguage
                        Case "zh-Hans"
                            tssLabel.Text = "上次备份于" & Date.Now & "成功完成，下次备份将于" & arrHourMin(0) * 60 + arrHourMin(1) & "分钟后进行"
                        Case "zh-Hant"
                            tssLabel.Text = "上次備份與" & Date.Now & "成功完成，下次備份將于" & arrHourMin(0) * 60 + arrHourMin(1) & "分鐘后進行"
                        Case "en"
                            tssLabel.Text = "Last backup completed at " & Date.Now & ",next once will start after " & arrHourMin(0) * 60 + arrHourMin(1) & " minutes"
                    End Select

                    '记录本次备份完成时间，当定时功能设定为时间间隔时，便于计算下次备份时间
                    _minuteNextOnce = 0
                    Timer1.Enabled = True
            End Select

            CopyThread.Abort()
            CopyThread = Nothing

            Exit Sub
        Else
            ' 显示当前复制文件
            lstShow.Items.Add(Message)

            If ProgressBar1.Maximum <> 0 Then
                ProgressBar1.Value = _totalFiles - (_totalFiles - CopiedFiles)
                Select Case NowLanguage
                    Case "zh-Hans"
                        tssLabel.Text = "共有" & _totalFiles & "个文件需要复制，当前复制第" & _copiedFiles & "个文件"
                    Case "zh-Hant"
                        tssLabel.Text = "共有" & _totalFiles & "個文件需要復制，當前復制第" & _copiedFiles & "個文件"
                    Case "en"
                        tssLabel.Text = "There are " & _totalFiles & " files need backup, now have " & _copiedFiles & " files completed"
                End Select
            End If

            _copiedFiles = CopiedFiles
        End If
    End Sub

    '统计进程的消息
    Public Sub CountThreadMessage(ByVal ThreadName As String, ByVal Files As Long, ByVal TotalFiles As Long, ByVal Folders As Long, ByVal Message As String)

        ' 当统计结束时为CopyThreadMessage()保存总文件数
        If Message = "END" Then
            _totalFiles += TotalFiles
            _totalFolders += Folders

            '如果已将所有任务统计完成
            CountThread.Abort()
            CountThread = Nothing

            ProgressBar1.Maximum = _totalFiles
            Select Case NowLanguage
                Case "zh-Hans"
                    tssLabel.Text = "共有" & _totalFiles & "个文件需要复制，当前复制第" & _copiedFiles & "个文件"
                Case "zh-Hant"
                    tssLabel.Text = "共有" & _totalFiles & "個文件需要復制，當前復制第" & _copiedFiles & "個文件"
                Case "en"
                    tssLabel.Text = "There are " & _totalFiles & " files need backup, now have " & _copiedFiles & " files completed"
            End Select
        End If

    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        Dim arrCell() As String, arrWeekTime() As String, arrHourMin() As String

        Select Case TimerType
            Case "byWeek"
                Dim isBackupNow As Boolean = False
                arrCell = Split(_strTimer, "|")
                For i = 0 To arrCell.Length - 1
                    If arrCell(i).ToString <> "" Then
                        arrWeekTime = Split(arrCell(i).ToString, ",")
                        arrHourMin = Split(arrWeekTime(1).ToString, ":")
                        If Date.Today.DayOfWeek = arrWeekTime(0) And Date.Now.Hour.ToString = arrHourMin(0) And Date.Now.Minute.ToString = arrHourMin(1) Then
                            isBackupNow = True
                            Exit For
                        End If
                    End If
                Next
                If isBackupNow = False Then Exit Sub
            Case "byDate"
                arrCell = Split(_strTimer, ",")
                arrHourMin = Split(arrCell(1).ToString, ":")
                If Date.Today.Date <> arrCell(0) Then Exit Sub
                If Date.Now.Hour.ToString <> arrHourMin(0) Then Exit Sub
                If Date.Now.Minute.ToString <> arrHourMin(1) Then Exit Sub
            Case "byIntarvel"
                _minuteNextOnce += 1
                arrHourMin = Split(_strTimer, ":")

                Dim minuteInt As Long
                minuteInt = CInt(arrHourMin(0)) * 60 + CInt(arrHourMin(1))
                If _minuteNextOnce <> minuteInt Then
                    Exit Sub
                Else
                    _minuteNextOnce = 0
                End If
        End Select

        tsbPause.Enabled = True

        '调用备份程序
        beginCopy()
    End Sub

    Private Sub cobSetLanguage_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cobSetLanguage.SelectedIndexChanged
        Dim setLng As New SetLanguageClass

        Select Case cobSetLanguage.Text
            Case "简体中文"
                _nowLanguag = "zh-Hans"
            Case ("繁體中文")
                _nowLanguag = "zh-Hant"
            Case "ENGLISH"
                _nowLanguag = "en"
        End Select

        '將所有窗口更新語言
        Me.setLanguage()

        '寫入註冊表
        Dim Key1, Key2, Key3 As Microsoft.Win32.RegistryKey
        Key1 = My.Computer.Registry.CurrentUser '返回当前用户键 
        Key2 = Key1.OpenSubKey("Software", True) '返回当前用户键下的Software键,如果想创建项，必须指定第二个参数为true  
        If Key2 Is Nothing Then Key2 = Key1.CreateSubKey("Software") '如果键不存在就创建它
        Key3 = Key2.OpenSubKey("MAIWO Smart Backup Professional", True)
        If Key3 Is Nothing Then Key3 = Key2.CreateSubKey("MAIWO Smart Backup Professional") '如果键不存在就创建它
        Key3.SetValue("Language", _nowLanguag)

    End Sub

    Public Sub setLanguage()
        Dim setLng As New SetLanguageClass

        Select Case _nowLanguag
            Case "zh-Hans"
                tsbBegin.Text = "启动"
                tsbPause.Text = "暂停"
                tsbEnd.Text = "停止"
                tsbAddTask.Text = "添加任务"
                tsbAlterTask.Text = "修改任务"
                tsbDeleteTask.Text = "删除任务"
                tsbSetTime.Text = "设定时间"
                tsbSetFilter.Text = "文件类型"
                tsbAbout.Text = "关于"
                tcbStartWithPC.Text = "开机启动"

                dGrid.Columns(1).HeaderText = "源路径"
                dGrid.Columns(2).HeaderText = "目标路径"
            Case ("zh-Hant")
                tsbBegin.Text = "啟動"
                tsbPause.Text = "暫停"
                tsbEnd.Text = "停止"
                tsbAddTask.Text = "添加任務"
                tsbAlterTask.Text = "修改任務"
                tsbDeleteTask.Text = "刪除任務"
                tsbSetTime.Text = "設定時間"
                tsbSetFilter.Text = "文件類型"
                tsbAbout.Text = "關于"
                tcbStartWithPC.Text = "開機啟動"

                dGrid.Columns(1).HeaderText = "源路徑"
                dGrid.Columns(2).HeaderText = "目標路徑"
            Case "en"
                tsbBegin.Text = "Start"
                tsbPause.Text = "Pause"
                tsbEnd.Text = "Stop"
                tsbAddTask.Text = "Add Task"
                tsbAlterTask.Text = "Change Task"
                tsbDeleteTask.Text = "Delete Task"
                tsbSetTime.Text = "Interval"
                tsbSetFilter.Text = "Extensions"
                tsbAbout.Text = "About"
                tcbStartWithPC.Text = "Run With Boot"

                dGrid.Columns(1).HeaderText = "From Path"
                dGrid.Columns(2).HeaderText = "To Path"
        End Select


        frmEditTask.setLanguage()
        frmSetFilter.setLanguage()
        frmSetFilter_Edit.setLanguage()
        frmSetTime.setLanguage()
    End Sub

    Private Sub tsbPause_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbPause.Click

        If Not IsNothing(CopyThread) Then
            If CopyThread.IsAlive Then CopyThread.Suspend()
        End If

        If Not IsNothing(CountThread) Then
            If CountThread.IsAlive Then CountThread.Suspend()
        End If

        tsbBegin.Enabled = True
        tsbPause.Enabled = False
        tsbEnd.Enabled = True

        tssLabel.Visible = True
        tssLabel.Text = "(暂停)"

        Select Case NowLanguage
            Case "zh-Hans"
                tssLabel.Text = "(暂停)"
            Case "zh-Hant"
                tssLabel.Text = "(暫停)"
            Case "en"
                tssLabel.Text = "(Pause)"
        End Select

        Timer1.Enabled = False
    End Sub

    Private Sub tsbToBBS_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbToBBS.Click
        System.Diagnostics.Process.Start("http://kintec.cn/bbs")
    End Sub

    Private Sub picGg_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles picGg.Click
        System.Diagnostics.Process.Start("http://www.maiwo.hk/html/20140711/89.html")
    End Sub

End Class