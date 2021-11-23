Set-Culture fr-FR
pretzel bake site

if ($lastExitCode -ne 0)
{
    exit -1
}
else
{
    Write-Host "Starting deploy"
    $envConf = '""environment"": {{""default"": {{""connection"": ""ftp://{0}:{1}@laedit.net""}}}}' -f $env:ftp_user, $env:ftp_password
    creep -d "{""""tracker"""": """"hash"""", $envConf}" -b site/_site -y

    if ($lastExitCode -ne 0)
    {
        exit -1
    }
}
