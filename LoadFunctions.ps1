Add-Type @"

    using System;

    using System.Runtime.InteropServices;

    public class Window {

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT wPlmt);

    }

    public struct RECT

    {

    public int Left;        // x position of upper-left corner

    public int Top;         // y position of upper-left corner

    public int Right;       // x position of lower-right corner

    public int Bottom;      // y position of lower-right corner

    }
    
    public struct POSI

    {

    public int Left;        // x position of upper-left corner

    public int Top;         // y position of upper-left corner

    }
    
    public struct SZ

    {

    public int Width;        // x position of lower-right corner

    public int Height;         // y position of lower-right corner

    }
    
    public struct POINT

    {

    public long x;

    public long y;

    }
    
    public struct WINDOWPLACEMENT

    {

    public uint length;

    public uint flags;

    public uint showCmd;

    public POINT ptMinPosition;

    public POINT ptMaxPosition;

    public RECT rcNormalPosition;
    
    public RECT rcDevice;

    }

"@

Add-Type -AssemblyName UIAutomationClient

function Get-Handle {
    param (
        $subString
    )

    $hwnd = (Get-Process | Where {$_.MainWindowTitle -like "*$subString*"} | Select -First 1)[0].MainWindowHandle
    if ($hwnd -eq 0 -or $hwnd -eq ""){
        Write-Host "Nope"
    }
    $hwnd
}

function Get-Window-Rekt {
    param (
        $hwnd
    )
    $Rectangle = New-Object RECT
    [Window]::GetWindowRect($hwnd, [ref] $Rectangle)
    $Rectangle
}

function Get-Window-Position {
    param (
        $hwnd
    )
    $rekt = (Get-Window-Rekt $hwnd)

    $Posi = New-Object POSI
    $Posi.Left = $rekt.Left
    $Posi.Top = $rekt.Top
    $Posi
}

function Get-Window-Size {
    param (
        $hwnd
    )
    $rekt = (Get-Window-Rekt $hwnd)

    $Sz = New-Object SZ
    $Sz.Width = $rekt.Right - $rekt.Left
    $Sz.Height = $rekt.Bottom - $rekt.Top
    $Sz
}

function Get-Window-Visible {
    param (
        $hwnd
    )

    $IsVisValue = [Window]::IsWindowVisible($hwnd)

    $WindowPlacement = New-Object WINDOWPLACEMENT
    $success = [Window]::GetWindowPlacement($hwnd, [ref] $WindowPlacement)
    $WindowPlacement.showCmd
}

function List-Windows {
    $procs = Get-Process | Where {$_.MainWindowHandle -ne 0} 
    ForEach ($proc in $procs){
        $proc | Add-Member -NotePropertyName ShowWindowState -NotePropertyValue (Get-Window-Visible $proc.MainWindowHandle)
        $proc | Add-Member -NotePropertyName Rect -NotePropertyValue (Get-Window-Rekt $proc.MainWindowHandle)
    }
    $procs | Select-Object -Property Name, MainWindowHandle, MainWindowTitle, IsVisible -ExpandProperty Rect
    $a = New-Object -com "Shell.Application"; $b = $a.windows(); $b
}

function List-Ignore-Windows {

    $procNames = @(
        "svchost",
        "TextInputHost",
        "ClearPassOnGuard"
        )

    Get-Process | Where {$_.MainWindowHandle -ne 0 -and $procNames -contains $_.Name} | Select-Object -Property Name, MainWindowHandle, MainWindowTitle
}

function Get-Window-ViewState {
    param(
        $hwnd
    )
    $automationElement = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
    $processPattern = $automationElement.GetCurrentPattern([System.Windows.Automation.WindowPatternIdentifiers]::Pattern)
    $processPattern.Current.WindowVisualState
}

