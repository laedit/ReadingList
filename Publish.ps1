$server = "ftp://laedit.net"
$user = "zlaeditn12713ne"
$pass = $env:ftp_password
$rootDirectory = "/httpdocs/addbook/"

function CreateFtpFolder ($folderToCreate)
{
    # Create the direct path to the folder you want to create
    $ftpPath = "$Server$rootDirectory$folderToCreate"
    Write-Host "Create folder: $ftpPath"

    # create the FtpWebRequest and configure it
    $ftp = [System.Net.FtpWebRequest]::Create($ftpPath)
    $ftp.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
    $ftp.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    $ftp.UsePassive = $true

    $response = [System.Net.FtpWebResponse]$ftp.GetResponse()
    $response.Close()
}

function UploadFtpFile ($fileToUpload)
{
    $webclient = New-Object -TypeName System.Net.WebClient
    $webclient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
    Write-Host "Uploading $file"
    $destination = New-Object System.Uri($server + $rootDirectory + $file.FullName.replace($source, ""))
    Write-Host "Destination: $destination"
    $webclient.UploadFile($destination, $file.FullName)
    $webclient.Dispose()
}

Copy-Item "Publish" "Packaged" -Recurse
New-Item "Packaged/bin/app.config" -type file -value "AppVeyorApiKey:$env:config1`r`nSoleUser:$env:config2"

$source = "C:\projects\readinglist\Packaged\"
$files = Get-ChildItem $source -Recurse

foreach	($file in $files)
{
    If ($file -is [System.IO.DirectoryInfo]) {
        CreateFtpFolder $file
    }
    Else {
        UploadFtpFile $file
    }
}