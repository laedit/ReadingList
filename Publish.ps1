$server = "laedit.net"
$user = "zlaeditn12713ne"
$pass = $env:ftp_password
$rootDirectory = "/httpdocs/addbook"

Copy-Item "Publish" "Packaged"
New-Item "Packaged/bin" -type directory
New-Item "Packaged/bin/app.config" -type file -value "AppVeyorApiKey:$env:config1\r\nSoleUser:$env:config2"

## Get files
$dirSource = "C:\projects\readinglist\Packaged"
$files = Get-ChildItem -recurse $dirSource

## Get ftp object
$ftp_client = New-Object System.Net.WebClient
$ftp_address = "ftp://${user}:$pass@${server}:$rootdirectory"

## Make uploads
foreach($file in $files)
{
    Write-Host $file

    $directory = "";
    $source = $file.DirectoryName + "\" + $file;
    if ($file.DirectoryName.Length -gt 0)
    {
        $directory = $file.DirectoryName.Replace($dirSource,"")
    }
    $directory += "/";
    
    $ftp_command = $ftp_address + $directory + $file
    Write-Host $ftp_command
    
    $uri = New-Object System.Uri($ftp_command)
    $ftp_client.UploadFile($uri, $source)
}