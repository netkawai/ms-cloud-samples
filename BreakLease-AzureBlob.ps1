Param([string]$Uri = $(Read-Host -prompt "Please specify a blob URL"))

$ProgressPreference = 'SilentlyContinue'


echo "Processing $Uri"

echo "Reading storage account information..."
$acct = Get-AzureStorageAccount | ? { (new-object System.Uri($_.Endpoints[0])).Host -eq (new-object System.Uri($Uri)).Host }
if(-not $acct) {
	write-host "The supplied URL does not appear to correspond to a storage account associated with the current subscription." -foregroundcolor "red"
	break
}

$acctKey = Get-AzureStorageKey ($acct.StorageAccountName)
$creds = "DefaultEndpointsProtocol=http;AccountName=$($acctKey.StorageAccountName);AccountKey=$($acctKey.Primary)"
$acctobj = [Microsoft.WindowsAzure.CloudStorageAccount]::Parse($creds)
$uri = $acctobj.Credentials.TransformUri($uri)
echo "Confirmed - storage account '$($acct.StorageAccountName)'."


$blobclient = New-Object Microsoft.WindowsAzure.StorageClient.CloudBlobClient($acctobj.BlobEndpoint, $acctobj.Credentials)
$blobclient.Timeout = (New-TimeSpan -Minutes 1)
$blob = New-Object Microsoft.WindowsAzure.StorageClient.CloudPageBlob($uri, $blobclient)


echo "Checking whether the blob is currently registered as a disk or image..."
$disk = Get-AzureDisk | ? { (new-object System.Uri($_.MediaLink)) -eq $blob.Uri }
if($disk) {
	write-host "The blob is still registered as a disk with name '$($disk.DiskName)'.  Please delete the disk first." -foregroundcolor "red"
	break
}
$image = Get-AzureVMImage | ? { $_.MediaLink -eq $blob.Uri.AbsoluteUri }
if($image) {
	write-host "The blob is still registered as an OS image with name '$($image.ImageName)'.  Please delete the OS image first." -foregroundcolor "red"
	break
}
echo "Confirmed - the blob is not in use by the Windows Azure platform."




echo "Inspecting the blob's lease status..."
try {
	$blob.FetchAttributes()
} catch [System.Management.Automation.MethodInvocationException] {
	write-host $_.Exception.InnerException.Message -foregroundcolor "red"
	break
}

echo "Current lease status: $($blob.Properties.LeaseStatus)"

if($blob.Properties.LeaseStatus -ne [Microsoft.WindowsAzure.StorageClient.LeaseStatus]::Locked) {
	write-host "Success - the blob is unlocked." -foregroundcolor "green"
	break
}



echo "Unlocking the blob..."
$request = [Microsoft.WindowsAzure.StorageClient.Protocol.BlobRequest]::Lease($uri, 0, [Microsoft.WindowsAzure.StorageClient.Protocol.LeaseAction]::Break, $null)
$request.Timeout = $blobclient.Timeout.TotalMilliseconds
$acctobj.Credentials.SignRequest($request)
try {
	$response = $request.GetResponse()
	$response.Close()
}
catch {
	write-host "The blob could not be unlocked:" -foregroundcolor "red"
	write-host $_.Exception.InnerException.Message -foregroundcolor "red"
	break
}

$blob.FetchAttributes()
echo "Current lease status: $($blob.Properties.LeaseStatus)"

write-host "Success - the blob is unlocked." -foregroundcolor "green"
