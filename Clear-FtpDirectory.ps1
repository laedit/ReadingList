$server = "ftp://laedit.net/"
$user = "zlaeditn12713ne"
$pass = $env:ftp_password
$rootDirectory = "httpdocs/addbook/"

function Get-FtpResponse($ftpUrl, $ftpMethod)
{
	# create the FtpWebRequest and configure it
	$ftp = [System.Net.FtpWebRequest]::Create($ftpUrl)
	$ftp.Method = $ftpMethod
	$ftp.Credentials = New-Object System.Net.NetworkCredential($user, $pass)
	$ftp.UsePassive = $true

	$response = [System.Net.FtpWebResponse]$ftp.GetResponse()
	Return $response
}

function Delete-FtpFile ($fileToDelete)
{
	# Create the direct path to the file you want to delete
	$ftpPath = "$Server/$fileToDelete"
	$response = Get-FtpResponse $ftpPath ([System.Net.WebRequestMethods+Ftp]::DeleteFile)
	$response.Close()
}

function Delete-FtpFolder ($folderToDelete)
{
	# Create the direct path to the folder you want to delete
	$ftpPath = "$Server/$folderToDelete"
	$response = Get-FtpResponse $ftpPath ([System.Net.WebRequestMethods+Ftp]::RemoveDirectory)
	$response.Close()
}

Function List-FtpDirectory($Directory) {
    
    # Credentials
	$FTPResponse = Get-FtpResponse "$($Server)$($Directory)" ([System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails)
    $ResponseStream = $FTPResponse.GetResponseStream()

    # Create a nice Array of the detailed directory listing
    $StreamReader = New-Object System.IO.Streamreader $ResponseStream
    $DirListing = (($StreamReader.ReadToEnd()) -split [Environment]::NewLine)
    $StreamReader.Close()

    # Close the FTP connection so only one is open at a time
    $FTPResponse.Close()
    
    # This array will hold the final result
    $FileTree = @(,@())
	
    # Loop through the listings
    foreach ($CurLine in $DirListing) {

        # Split line into space separated array
        $LineTok = ($CurLine -split '\ +')

        # Get the filename (can even contain spaces)
        $CurFile = $LineTok[8..($LineTok.Length-1)]

        # Figure out if it's a directory. Super hax.
		if ($LineTok[2]) {
        	$DirBool = $LineTok[2].Contains("DIR")
		}
		
        # Determine what to do next (file or dir?)
		if($CurFile) {
			
			If ($DirBool) {
				# Recursively traverse sub-directories
				$FileTree += List-FtpDirectory "$($Directory)$($CurFile)/"
				
				$FileTree += ,("$($Directory)$($CurFile)", $True)
			} 
			Else {
				# Add the output to the file tree
				$FileTree += ,("$($Directory)$($CurFile)", $False)
			}
		}
    }
	
    Return $FileTree
}


foreach	($file in List-FtpDirectory $rootDirectory)
{
	If($file) {
		If ($file[1]) {
			Delete-FtpFolder $file[0]
		}
		Else {
			Delete-FtpFile $file[0]
		}
	}
}