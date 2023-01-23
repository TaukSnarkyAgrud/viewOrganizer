$requestedStreamers = @{}
$TagSiteKeys = [ordered]@{};
$upperLeftOrigin = @{
    Left=0;
    Top=0
}
$preferredStreamerTags = [ordered]@{
    poolshark=@{twitch="thepoolshark"; facebook="PoolsharkGaming"};
    lupo=@{youtube="DrLupo"};
    trip=@{twitch="triple_g"};
    fab=@{twitch="notfabtv"};
    paul=@{twitch="actionjaxon"};
    fudgexl=@{twitch="fudgexl"};
    jenn=@{twitch="jenntacles"};
    aims=@{twitch="aims"};
    tim=@{twitch="Darkness429"};
    pestily=@{twitch="pestily"};
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
}
$onlinePreferredStreamers = [ordered]@{};

function createTwitchStreamUrl {
    param (
        $streamerTag
    )
    $streamUrl = "https://player.twitch.tv/?channel=${streamerTag}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&volume=0"
    return $streamUrl
}
function createYoutubeStreamUrl {
    param (
        $streamerTag
    )
    $streamUrl = "https://www.youtube.com/${streamerTag}/live"
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
        $tagObject
    )
    $streamHash = getYoutubeStreamHash $tagObject
    $streamUrl = "https://www.youtube.com/live_chat?is_popout=1&v=${streamHash}"
    return $streamUrl
}

function getYoutubeStreamHash {
    param (
        $tagObject
    )
    $TagYt = $tagObject["youtube"]

    $streamUri = "https://www.youtube.com/${TagYt}/live"
    $streamHtml = Invoke-RestMethod -Uri $streamUri
    $match = Select-String "https:\/\/youtu\.be\/([A-Za-z0-9]+)`"" -inputobject $streamHtml
    $vId = $match.Matches.groups[1].value
    return $vId
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
                if ((Invoke-RestMethod -Uri "https://www.youtube.com/${tagObject[tagSite]}/") -like "*hqdefault_live.jpg*"){
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
    $chatW=360
    $chatH=900
    $screenWDivided3=[math]::ceiling($screenW/3)
    $screenHDivided2=[math]::ceiling($screenH/2)
    $selection = $tagSiteKeys.Keys | Out-GridView -OutputMode Multiple
    Write-Output("`nSelection:" + $d)
    foreach($d in $selection){
        $valueObject = $tagSiteKeys[$d]
        $streamUrl = getStreamUrl $valueObject["site"] $valueObject["tag"]
        $chatUrl = ""
        if ($valueObject["site"] -ne "facebook"){
            $chatUrl = getChatUrl $valueObject["site"] $valueObject["tag"]
            Write-Output @{$valueObject["site"]=$valueObject["tag"]; streamUrl=$streamUrl; chatUrl=$chatUrl}
            $requestedStreamers.Add($valueObject["name"], @{$valueObject["site"]=$valueObject["tag"]; streamUrl=$streamUrl; chatUrl=$chatUrl})
            
            $streamCliString = createCliChromeString $streamUrl $screenWDivided3 $screenHDivided2 $upperLeftOrigin
            Write-Output $streamCliString
            $chatCliString = createCliChromeString $chatUrl $chatW $chatH $upperLeftOrigin
            Write-Output $chatCliString

            spawnStream $streamCliString
            spawnStream $chatCliString

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

    $aRandom = Get-Random -Maximum 1000000
    $streamUrl = "--app=${streamUri} --window-position=`"${Left},${Top}`" --window-size=`"${streamW},${streamH}`""
    #Write-Output $streamUrl
    return $streamUrl
}

function spawnStream{
    param(
        $cliString
    )
    
    Write-Output("`nStarting stream: " + $cliString + "`n`n")
        Start-Sleep -s 1
        & 'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe' $cliString
}
userSelectStreamers