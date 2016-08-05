$server = "laedit.net"
$user = "zlaeditn12713ne"
$pass = $env:ftp_password
$rootDirectory = "httpdocs/addbook"

Copy-Item "Publish" "Packaged"
New-Item "Packaged/bin" -type directory
New-Item "Packaged/bin/app.config" -type file -value "AppVeyorApiKey:$env:config1\r\nSoleUser:$env:config2"

$source = "C:\projects\readinglist\Packaged"
$destination = "ftp://${user}:$pass@$server/$rootDirectory"

$webclient = New-Object -TypeName System.Net.WebClient

$files = Get-ChildItem -recurse $source

foreach ($file in $files)
{
    Write-Host "Uploading $file"
    $webclient.UploadFile("$destination/$file", $file.FullName)
} 

$webclient.Dispose()