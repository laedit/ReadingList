$urlbase = "ftp://laedit.net/httpdocs/readinglist"
$user = "zlaeditn12713ne"
$pass = 'gl%$$91PN..-mj$#%' #$env:ftppassword

function Get-FtpCredential()
{
	Return New-Object System.Net.NetworkCredential($user, $pass)
}

function Delete-FtpFile ($fileToDelete)
{
	if($fileToDelete)
	{
		# Create the direct path to the file you want to delete
		$ftpPath = "$urlbase/$fileToDelete"
		Write-Host $ftpPath
		# create the FtpWebRequest and configure it
		$ftp = [System.Net.FtpWebRequest]::Create($ftpPath)
	
		$ftp.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
	
		$ftp.Credentials = Get-FtpCredential
	
		$ftp.UseBinary = $true
		$ftp.UsePassive = $true
	
		$response = [System.Net.FtpWebResponse]$ftp.GetResponse()
		$response.Close()
	}
}

Function List-FtpDirectory() {
    
    # Credentials
    $FTPRequest = [System.Net.FtpWebRequest]::Create($urlbase)
    $FTPRequest.Credentials = Get-FtpCredential
    $FTPRequest.Method = [System.Net.WebRequestMethods+FTP]::ListDirectoryDetails

    # Don't want Binary, Keep Alive unecessary.
    $FTPRequest.UseBinary = $False
    $FTPRequest.KeepAlive = $False

    $FTPResponse = $FTPRequest.GetResponse()
    $ResponseStream = $FTPResponse.GetResponseStream()

    # Create a nice Array of the detailed directory listing
    $StreamReader = New-Object System.IO.Streamreader $ResponseStream
    $DirListing = (($StreamReader.ReadToEnd()) -split [Environment]::NewLine)
    $StreamReader.Close()
Write-Host $DirListing
    # Remove first two elements ( . and .. ) and last element (\n)
    #$DirListing = $DirListing[2..($DirListing.Length-2)] 

    # Close the FTP connection so only one is open at a time
    $FTPResponse.Close()
    
    # This array will hold the final result
    $FileTree = @()

    # Loop through the listings
    foreach ($CurLine in $DirListing) {

        # Split line into space separated array
        $LineTok = ($CurLine -split '\ +')

        # Get the filename (can even contain spaces)
        $CurFile = $LineTok[8..($LineTok.Length-1)]
Write-Host $LineTok[2]
        # Figure out if it's a directory. Super hax.
        $DirBool = $LineTok[2].Contains("DIR")

        # Determine what to do next (file or dir?)
        If ($DirBool) {
            # Recursively traverse sub-directories
            $FileTree += ,(List-FtpDirectory "$($Directory)$($CurFile)/")
        } Else {
            # Add the output to the file tree
            $FileTree += ,"$($Directory)$($CurFile)"
        }
    }
    
    Return $FileTree
}


foreach	($file in List-FtpDirectory)
{
	Delete-FtpFile($file)
}