# A view is what a user sees infront of them. A collection of devices that present fedback and within those devices(usually displays)
# a collection of windows.
# Windows should open to the same size and location; everytime and even when that size and location is different due to haveing more or less of other windows.
# A display view is a framing of how the user sees a collection of displays(monitors); configured for resolution and physical location.
# A Window view is a collection of windows, with configured size and location, within a display view.

# Manage display views
    # collect data on the view
        # Display Primary, Resolution, Position
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
    # create a view objects
    # store and recall known views

# Manage window views
    # collect data on the view
        # Window Unique ID of type, position, size
    # create a view objects
    # store and recall known views

# When to store a view and when to recall a view
    # Watch and prevent thrashing
    # Add feature to lauch all items needed for a view
        # note that this is for view with apps that can be launched without anything else
