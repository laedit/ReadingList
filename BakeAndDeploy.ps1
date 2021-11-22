Set-Culture fr-FR
pretzel bake site

if ($lastExitCode -ne 0)
{
    exit -1
}
else
{
    Write-Host "Starting deploy"
    '{{""default"": {{""connection"": ""ftp://{0}:{1}@laedit.net""}}}}' -f $env:ftp_user, $env:ftp_password | Out-File -FilePath .\.creep.env
    creep -d '{""source"": ""hash""}' -b site/_site -y
	Remove-Item .\.creep.env -Force
    if ($lastExitCode -ne 0)
    {
        exit -1
    }
}
