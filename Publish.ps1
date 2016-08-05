$server = "ftp://laedit.net"
$user = "zlaeditn12713ne"
$pass = $env:ftp_password
$rootDirectory = "/httpdocs/addbook"

Get-ChildItem -Path c:\projects\readinglist -Recurse -Directory -Filter "Publish" | ForEach-Object {
     Write-Host $_.FullName
}

Copy-Item "C:\projects\readinglist\Publish" "C:\projects\readinglist\Packaged"
New-Item "Packaged/bin" -type directory
New-Item "Packaged/bin/app.config" -type file -value "AppVeyorApiKey:$env:config1\r\nSoleUser:$env:config2"

$source = "C:\projects\readinglist\Packaged"

$webclient = New-Object -TypeName System.Net.WebClient
$webclient.Credentials = New-Object System.Net.NetworkCredential($user,$pass)

$files = Get-ChildItem -recurse $source

foreach ($file in $files)
{
    Write-Host "Uploading $file"
    $uri = New-Object System.Uri($server + $rootDirectory + $item.Name)
    Write-Host "Destination: $uri"
    $webclient.UploadFile($uri, $file.FullName)
} 

$webclient.Dispose()