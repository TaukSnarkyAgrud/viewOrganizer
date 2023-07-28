$requestedStreamers = @{}
$TagSiteKeys = [ordered]@{};
$upperLeftOrigin = @{
    Left=0;
    Top=0
}
$preferredStreamerTags = [ordered]@{
    poolshark=@{twitch="thepoolshark"; facebook="PoolsharkGaming"; youtube="thepoolshark"};
    lupo=@{youtube="DrLupo"};
    trip=@{twitch="triple_g"};
    pestily=@{twitch="pestily"};
    fab=@{twitch="notfabtv"; youtube="NotFabTV"};
    paul=@{twitch="actionjaxon"};
    jenn=@{twitch="jenntacles"};
    aims=@{twitch="aims"};
    fudgexl=@{twitch="fudgexl"};
    tim=@{facebook="Darkness429"};
    tweety=@{twitch="tweetyexpert"};
    hodsy=@{twitch="hodsy"};
    bearkiLauren=@{twitch="bearki"};
    AnneMunition=@{twitch="AnneMunition"};
    mr___meme=@{twitch="mr___meme"};
    cali=@{twitch="caliverse"};
    clintus=@{twitch="clintus"};
    Bull1060=@{facebook="Bull1060"};
    ElliottAsAlways=@{facebook="ElliottAsAlways"};
    MugsTV=@{facebook="MugsTV"};
    callOfCrafter=@{facebook="Callofcrafters0"};
    theJosh=@{youtube="theJOSHfeed"};
}
$onlinePreferredStreamers = [ordered]@{};
$youtubeHashs = @{};

function createTwitchStreamUrl {
    param (
        $streamerTag
    )
    $streamUrl = "https://player.twitch.tv/?channel=${streamerTag}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&volume=0"
    return $streamUrl
}
function createYoutubeStreamUrl {
    param (
        $tag
    )
    $streamHash = getYoutubeStreamHash $tag
    # TODO: time start in url
    $streamUrl = "https://www.youtube.com/embed/${streamHash}?popout=1&autoplay=1&loop=0&controls=1&modestbranding=0"
    #$streamUrl = "https://www.youtube.com/${streamerTag}/live"
    return $streamUrl
}
function createFacebookStreamUrl {
    param (
        $streamerTag
    )
    $streamUrl = "https://www.facebook.com/${streamerTag}/live"
    return $streamUrl
}

function createTwitchChatUrl {
    param (
        $streamerTag
    )
    $streamUrl = "https://www.twitch.tv/popout/${streamerTag}/chat?popout="
    return $streamUrl
}
function createYoutubeChatUrl {
    param (
        $tag
    )

    $streamHash = getYoutubeStreamHash $tag
    $streamUrl = "https://www.youtube.com/live_chat?is_popout=1&v=${streamHash}"
    return $streamUrl
}

function getYoutubeStreamHash {
    param (
        $tag
    )

    if($youtubeHashs.containsKey($tag)){
        return $youtubeHashs[$tag]
    } else {
        $streamUri = "https://www.youtube.com/${tag}/live"
        $streamHtml = Invoke-RestMethod -Uri $streamUri
        #$streamHtml | Out-File -File "C:\Users\andrewta\Development\viewOrganizer\viewManager\Source\StreamingTools\Powershell\${tag}_tmp.html"
        $match = Select-String "https:\/\/youtu\.be\/([A-Za-z0-9-]+)`"" -inputobject $streamHtml
        $vId = $match.Matches.groups[1].value
        $youtubeHashs[$tag] = $vId
        return $vId
    }
}

function getStreamUrl{
    param(
        $site,
        $tag
    )
    switch($site){
        "twitch" {
            return createTwitchStreamUrl $tag
        }
        "facebook"{
            return createFacebookStreamUrl $tag
        }
        "youtube" {
            return createYoutubeStreamUrl $tag
        }
    }
}

function getChatUrl{
    param(
        $site,
        $tag
    )
    switch($site){
        "twitch" {
            return createTwitchChatUrl $tag
        }
        "youtube" {
            return createYoutubeChatUrl $tag
        }
    }
}

function htmlHasTag {
    param(
        $tag,
        $html
    )
    if($html -like "*$tag*"){
        return $true
    }
    return $false
}

function IsStreamerOnline{
    param(
        $streamerName,
        $tagObject
    )

    $streamerOnline = $false
    $streamerOnlineObject = @{};

    foreach($tagSite in $tagObject.Keys){
        switch($tagSite){
            "twitch" {
                Write-Host -NoNewline "twitch site for ${streamerName} "
                $tag = $tagObject[$tagSite]
                if ((Invoke-RestMethod -Uri "decapi.me/twitch/uptime/${tag}") -like "*offline*"){
                    Write-Output "Offline"
                } else {
                    Write-Output "Online"
                    $streamerOnline = $true
                    $streamerOnlineObject.Add($tagSite, $tag)
                }
            }
            "facebook"{
                Write-Host -NoNewline "facebook site for ${streamerName} "
                $tag = $tagObject[$tagSite]
                $OnlineCheck = Invoke-RestMethod -Uri "www.facebook.com/${tag}/live"
                if ($OnlineCheck -like "*LiveVideoOverlaySticker*"){
                    Write-Output "Online"
                    $streamerOnline = $true
                    $streamerOnlineObject.Add($tagSite, $tag)
                } else {
                    Write-Output "Offline"
                }
            }
            "youtube" {
                Write-Host -NoNewline "youtube site for ${streamerName} "
                $htmlReturn = (Invoke-RestMethod -Uri "https://www.youtube.com/${tagObject[tagSite]}/live")
                #$htmlReturn | Out-File -File "C:\Users\andrewta\Development\viewOrganizer\viewManager\Source\StreamingTools\Powershell\${streamerName}_tmp.html"
                $tag = $tagObject[$tagSite]
                
                #if ((htmlHasTag $tag $htmlReturn) -and ($htmlReturn -like "*hqdefault_live.jpg*" -or $htmlReturn -like "*hq720_live.jpg*")){    
                if ($htmlReturn -like "*hqdefault_live.jpg*" -or $htmlReturn -like "*hq720_live.jpg*"){
                    Write-Output "Online"
                    $streamerOnline = $true
                    $streamerOnlineObject.Add($tagSite, $tag)
                } else {
                    Write-Output "Offline"
                }
            }
        }
    }
    if($streamerOnline){
        $onlinePreferredStreamers.Add($streamerName, $streamerOnlineObject)
    }
    return $streamerOnline
}

function userSelectStreamers{
    foreach($streamer in $preferredStreamerTags.Keys){
        IsStreamerOnline $streamer $preferredStreamerTags[$streamer]
    }
    $streamerNames = $onlinePreferredStreamers.Keys
    foreach($streamerName in $streamerNames){
        $streamerTagObject = $onlinePreferredStreamers[$streamerName]
        foreach($siteKey in $streamerTagObject.Keys){
            $tagSiteKeys.Add("${streamerName}-${siteKey}", @{name=${streamerName}; site=${siteKey}; tag=$streamerTagObject[$siteKey]})
        }

        Write-Output "`n"
    }
    getScreenDimensions
    $screenW=1904
    $screenH=1000
    $screenWDivided3=[math]::ceiling($screenW/3)
    $screenHDivided2=[math]::ceiling($screenH/2)
    $selection = $tagSiteKeys.Keys | Out-GridView -OutputMode Multiple
    Write-Output("`nSelection:" + $d)
    foreach($d in $selection){
        $valueObject = $tagSiteKeys[$d]
        $streamUrl = getStreamUrl $valueObject["site"] $valueObject["tag"]
        if ($valueObject["site"] -ne "facebook"){
            Write-Output @{$valueObject["site"]=$valueObject["tag"]; streamUrl=$streamUrl}
            $requestedStreamers.Add($valueObject["name"], @{$valueObject["site"]=$valueObject["tag"]; streamUrl=$streamUrl})
            
            $streamCliString = createCliChromeString $streamUrl $screenWDivided3 $screenHDivided2 $upperLeftOrigin
            Write-Output $streamCliString

            spawnStream $streamCliString

        }else {
            Write-Output @{$valueObject["site"]=$valueObject["tag"]; streamUrl=$streamUrl}
            $requestedStreamers.Add($valueObject["name"], @{$valueObject["site"]=$valueObject["tag"]; streamUrl=$streamUrl})
            $streamCliString = createCliChromeString $streamUrl $screenWDivided3 $screenHDivided2 $upperLeftOrigin
            Write-Output $streamCliString
            spawnStream $streamCliString
        }
    }

    if ($requestedStreamers.Count -gt 15) {
        Write-Output("ERROR too many streams requested")
        exit
    }
}

function userSelectChats{
    $selection = $tagSiteKeys.Keys | Out-GridView -OutputMode Multiple
    Write-Output("`nSelection:" + $d)
    $chatUrl = ""
    $chatW=360
    $chatH=900
    foreach($d in $selection){
        $valueObject = $tagSiteKeys[$d]
        if ($valueObject["site"] -ne "facebook"){
            $chatUrl = getChatUrl $valueObject["site"] $valueObject["tag"]
            $chatCliString = createCliChromeString $chatUrl $chatW $chatH $upperLeftOrigin
            Write-Output $chatCliString
            spawnStream $chatCliString
        }
    }
}

function getScreenDimensions{
    # Write-Output("Adding Type")
    # Add-Type -AssemblyName System.Windows.Forms
    # [int]$screenW=[System.Windows.Forms.Screen]::AllScreens.WorkingArea.Width
    # [int]$screenH=[System.Windows.Forms.Screen]::AllScreens.WorkingArea.Height

    $screenW=1904
    $screenH=1000
    $chatW=360
    $chatH=900
    $screenWDivided3=[math]::ceiling($screenW/3)
    $screenHDivided2=[math]::ceiling($screenH/2)
}

function createCliChromeString {
    param (
        $streamUri,
        $streamW,
        $streamH,
        $displayViewAddress
    )
    $Left = $displayViewAddress.Left
    $Top = $displayViewAddress.Top

    # $tmpDir = $env:tmp
    # $aRandom = Get-Random -Maximum 1000000
    $streamUrl = " --app=`"data:text/html,<html><body><script>window.moveTo(${Left},${Top});window.resizeTo(${streamW},${streamH});window.location='${streamUri}';</script></body></html>`""
    #$streamUrl = "--app=${streamUri} --window-position=${Left},${Top} --window-size=${streamW},${streamH}"
    return $streamUrl
}

function spawnStream{
    param(
        $cliString
    )
    
    Write-Output("`nStarting stream: " + $cliString + "`n`n")
        Start-Sleep -s 1
        & 'C:\Program Files\Google\Chrome\Application\chrome.exe' $cliString
}