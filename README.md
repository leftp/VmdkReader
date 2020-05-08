# vmdkReader
.Net 4.0 Console App to read and extract files from vmdk images

Uses https://github.com/DiscUtils/DiscUtils lib to parse the vmdk images.

Useful in cases where the vmdk is on the network and you only want to copy a single file instead of GBs (e.g ntds.dit), since it does not transfer the whole disk over the network.

**Project uses:**
* Quamotion.DiscUtils.Core
* Quamotion.DiscUtils.Ntfs
* Quamotion.DiscUtils.Streams
* Quamotion.DiscUtils.Vmdk

and ILMerge 3.0.29 & ILMerge.MSBuild.Task to bundle the required dlls. Generated file < 1024kb

**Commands:**

dir 

--source: The source of the vmdk drive. It can also accept SMB paths

--directory: The directory you want to list from the vmdk disk. If not provided will default to root path

cp - Will copy a file from the vmdk to the destination provided

--source: The source of the vmdk drive. It can also accept SMB paths

--file2copy: The file you want to copy from the vmdk disk

--destination: The destination where to save the file


**Examples:**

vmdk.exe --command dir --source \\backupserver\dc01\dc01.vmdk --directory \Windows\System32")

vmdk.exe --command cp --source \\backupserver\dc01\dc01.vmdk --file2copy \Windows\System32\calc.exe --destination C:\users\user\Desktop\calc.exe

            
**WARNING - tested only with specific vmdk/vhd images and network latencies. Use at your own risk!**

**TODO**

Add support for filesystem detection

Add support for nfs / iSCSI
