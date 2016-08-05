$server = "ftp://laedit.net"
$user = "zlaeditn12713ne"
$pass = $env:ftp_password
$rootDirectory = "/httpdocs/addbook"

Copy-Item "Publish" "Packaged" -Recurse
New-Item "Packaged/bin" -type directory
New-Item "Packaged/bin/app.config" -type file -value "AppVeyorApiKey:$env:config1\r\nSoleUser:$env:config2"

$source = "C:\projects\readinglist\Packaged"

$webclient = New-Object -TypeName System.Net.WebClient
$webclient.Credentials = New-Object System.Net.NetworkCredential($user,$pass)

$files = Get-ChildItem $source -Recurse

foreach ($file in $files)
{
    Write-Host "Uploading $file"
    $destination = New-Object System.Uri($server + $rootDirectory + $file.Name)
    Write-Host "Destination: $destination"
    $webclient.UploadFile($destination, $file.FullName)
} 

$webclient.Dispose()