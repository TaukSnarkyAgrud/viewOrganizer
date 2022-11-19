Add-Type -AssemblyName System.Windows.Forms
$as = [System.Windows.Forms.Screen]::AllScreens

$displays = $as | ForEach-Object {
    [PSCustomObject]@{
    GraphicPort = $_.DeviceName.substring($_.DeviceName.IndexOf('Y')+1, 1)
    Primary = $_.Primary
    Resolution = @{
        Width  = $_.Bounds.Width
        Height = $_.Bounds.Height
    }
    Position = $_.WorkingArea.Location
}}

$displays