using System;
using System.IO;
using DiscUtils;
using DiscUtils.Ntfs;
using DiscUtils.Setup;
using DiscUtils.Vmdk;
using System.Collections.Generic;
using System.Linq;
using DiscUtils.Ntfs.Internals;


namespace vmdk
{
    class Program
    {
        public static string GetArgument(IEnumerable<string> args, string option)
            => args.SkipWhile(i => i != option).Skip(1).Take(1).FirstOrDefault();

        static void Main(string[] args)
        {
            SetupHelper.RegisterAssembly(typeof(NtfsFileSystem).Assembly);
            SetupHelper.RegisterAssembly(typeof(Disk).Assembly);
            SetupHelper.RegisterAssembly(typeof(VirtualDiskManager).Assembly);
            if (args.Length != 0 && GetArgument(args, "--command").Length != 0)
            {
                string command = GetArgument(args, "--command");
                if (command.ToLower() == "dir" && GetArgument(args, "--source") != null)
                {
                    var diskimagepath = GetArgument(args, "--source");
                    var directorypath = GetArgument(args, "--directory");
                    try
                    {
                        GetDirListing(diskimagepath, directorypath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\r\n [!] An exception occured: {0}", ex);
                        throw;
                    }

                }
                else if (command.ToLower() == "cp" && GetArgument(args, "--source") != null &&
                         GetArgument(args, "--file2copy") != null && GetArgument(args, "--destination") != null)
                {
                    var diskimagepath = GetArgument(args, "--source");
                    var filepath = GetArgument(args, "--file2copy");
                    var destination = GetArgument(args, "--destination");

                    try
                    {
                        GetFile(diskimagepath, filepath, destination);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\r\n [!] An exception occured: {0}", ex);
                        throw;
                    }
                }
                else
                {
                    GetHelp();
                }
            }
            else
            {
                GetHelp();
            }
        }

        public static void GetHelp()
        {
            Console.WriteLine("\r\nvVvVvVvVmMmMmMmMdDdDdDdDkKkKkKkK");
            Console.WriteLine("K       VMDK mounter v0.1      K");
            Console.WriteLine("vVvVvVvVmMmMmMmMdDdDdDdDkKkKkKkK");
            Console.WriteLine("\r\n Usage:");
            Console.WriteLine("\r\n vmdkmounter.exe --command [command] [command arguments]:");
            Console.WriteLine("\r\n [?] Command: dir - Will output a dirlisting of the provided folder\n");
            Console.WriteLine(" --source: The source of the vmdk drive. It can also accept SMB paths");
            Console.WriteLine(
                " --directory: The directory you want to list from the vmdk disk. If not provided will default to root path");
            Console.WriteLine("\r\n [?] Command: cp - Will copy a file from the vmdk to the destination provided\n");
            Console.WriteLine("--source: The source of the vmdk drive. It can also accept SMB paths");
            Console.WriteLine("--file2copy: The file you want to copy from the vmdk disk ");
            Console.WriteLine("--destination: The destination where to save the file");
            Console.WriteLine("\r\n [?] Examples:\r\n");
            Console.WriteLine(
                "vmdk.exe --command dir --source \\\\backupserver\\dc01\\dc01.vmdk --directory \\Windows\\System32");
            Console.WriteLine(
                "vmdk.exe --command cp --source \\\\backupserver\\dc01\\dc01.vmdk --file2copy \\Windows\\System32\\notepad.exe --destination C:\\users\\user\\Desktop\\notepad.exe");

        }

        public static void GetDirListing(string VMDKpath, string directory)
        {
            if (File.Exists(VMDKpath))
            {
                try
                {
                    VolumeManager volMgr = new VolumeManager();
                    VirtualDisk vhdx = VirtualDisk.OpenDisk(VMDKpath, FileAccess.Read);
                    volMgr.AddDisk(vhdx);
                    VolumeInfo volInfo = null;
                    if (vhdx.Partitions.Count > 1)
                    {
                        Console.WriteLine("\r\n[*] Target has more than one partition\r\n");
                        foreach (var physVol in volMgr.GetPhysicalVolumes())
                        {
                            Console.WriteLine("      Identity: " + physVol.Identity);
                            Console.WriteLine("          Type: " + physVol.VolumeType);
                            Console.WriteLine("       Disk Id: " + physVol.DiskIdentity);
                            Console.WriteLine("      Disk Sig: " + physVol.DiskSignature.ToString("X8"));
                            Console.WriteLine("       Part Id: " + physVol.PartitionIdentity);
                            Console.WriteLine("        Length: " + physVol.Length + " bytes");
                            Console.WriteLine(" Disk Geometry: " + physVol.PhysicalGeometry);
                            Console.WriteLine("  First Sector: " + physVol.PhysicalStartSector);
                            Console.WriteLine();
                            if (!string.IsNullOrEmpty(physVol.Identity))
                            {
                                volInfo = volMgr.GetVolume(physVol.Identity);
                            }

                            using (NtfsFileSystem vhdbNtfs = new NtfsFileSystem(physVol.Partition.Open()))
                            {
                                if (vhdbNtfs.DirectoryExists("\\\\" + directory))
                                {
                                    string[] filelist = vhdbNtfs.GetFiles(vhdbNtfs.Root.FullName + directory);
                                    string[] dirlist = vhdbNtfs.GetDirectories(vhdbNtfs.Root.FullName + directory);

                                    foreach (var file in filelist)
                                    {
                                        Console.WriteLine("[F] {0}  {1}", file, vhdbNtfs.GetFileLength(file));
                                    }

                                    foreach (var dir in dirlist)
                                    {
                                        Console.WriteLine("[D] {0}", dir);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("\r\n[*] Directory does not exist in partition {0}\r\n",
                                        physVol.Identity);
                                }
                            }
                        }
                    }
                    else //No partitions
                    {
                        Console.WriteLine("\r\n[*] Found only one partition\r\n");
                        Console.WriteLine("LOGICAL VOLUMES");
                        foreach (var logVol in volMgr.GetLogicalVolumes())
                        {
                            Console.WriteLine("      Identity: " + logVol.Identity);
                            Console.WriteLine("        Length: " + logVol.Length + " bytes");
                            Console.WriteLine(" Disk Geometry: " + logVol.PhysicalGeometry);
                            Console.WriteLine("  First Sector: " + logVol.PhysicalStartSector);
                            Console.WriteLine();
                        }

                        using (NtfsFileSystem vhdbNtfs = new NtfsFileSystem(vhdx.Partitions[0].Open()))
                        {
                            if (vhdbNtfs.DirectoryExists("\\\\" + directory))
                            {
                                string[] filelist = vhdbNtfs.GetFiles(vhdbNtfs.Root.FullName + directory);
                                string[] dirlist = vhdbNtfs.GetDirectories(vhdbNtfs.Root.FullName + directory);


                                foreach (var file in filelist)
                                {
                                    Console.WriteLine("[F] {0}  {1}", file, vhdbNtfs.GetFileLength(file));
                                }

                                foreach (var dir in dirlist)
                                {
                                    Console.WriteLine("[D] {0}", dir);
                                }
                            }
                            else
                            {
                                Console.WriteLine("\r\n[*] Directory does not exist");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception {0}", ex);
                }
            }
        }

        public static void GetFile(string VMDKpath, string filepath, string destinationfile)
        {
            if (File.Exists(VMDKpath) && Directory.Exists(Path.GetDirectoryName(destinationfile)))
            {
                if (Path.GetFileName(destinationfile) == "")
                {
                    destinationfile += Path.GetFileName(filepath);
                }

                VolumeManager volMgr = new VolumeManager();
                VirtualDisk vhdx = VirtualDisk.OpenDisk(VMDKpath, FileAccess.Read);
                volMgr.AddDisk(vhdx);
                VolumeInfo volInfo = null;
                if (vhdx.Partitions.Count > 1)
                {
                    Console.WriteLine("\r\n[*] Target has more than one partition\r\n");
                    foreach (var physVol in volMgr.GetPhysicalVolumes())
                    {
                        Console.WriteLine("      Identity: " + physVol.Identity);
                        Console.WriteLine("          Type: " + physVol.VolumeType);
                        Console.WriteLine("       Disk Id: " + physVol.DiskIdentity);
                        Console.WriteLine("      Disk Sig: " + physVol.DiskSignature.ToString("X8"));
                        Console.WriteLine("       Part Id: " + physVol.PartitionIdentity);
                        Console.WriteLine("        Length: " + physVol.Length + " bytes");
                        Console.WriteLine(" Disk Geometry: " + physVol.PhysicalGeometry);
                        Console.WriteLine("  First Sector: " + physVol.PhysicalStartSector);
                        Console.WriteLine();
                        if (!string.IsNullOrEmpty(physVol.Identity))
                        {
                            volInfo = volMgr.GetVolume(physVol.Identity);
                        }

                        using (NtfsFileSystem vhdbNtfs = new NtfsFileSystem(physVol.Partition.Open()))
                        {
                            if (vhdbNtfs.FileExists("\\\\" + filepath))
                            {
                                long fileLength = vhdbNtfs.GetFileLength("\\\\" + filepath);
                                using (Stream bootStream = vhdbNtfs.OpenFile("\\\\" + filepath, FileMode.Open,
                                    FileAccess.Read))
                                {
                                    byte[] file = new byte[bootStream.Length];
                                    int totalRead = 0;
                                    while (totalRead < file.Length)
                                    {
                                        totalRead += bootStream.Read(file, totalRead, file.Length - totalRead);
                                        FileStream fileStream =
                                            File.Create(destinationfile, (int) bootStream.Length);
                                        bootStream.CopyTo(fileStream);
                                        fileStream.Write(file, 0, (int) bootStream.Length);
                                    }

                                    long destinationLength = new FileInfo(destinationfile).Length;
                                    if (fileLength != destinationLength)
                                    {
                                        Console.WriteLine(
                                            "[!] Something went wrong. Source file has size {0} and destination file has size {1}",
                                            fileLength, destinationLength);
                                    }
                                    else
                                    {
                                        Console.WriteLine("\r\n[*] File {0} was successfully copied to {1}",
                                            filepath, destinationfile);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("\r\n [!] File {0} can not be found", filepath);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var physVol in volMgr.GetPhysicalVolumes())
                    {
                        Console.WriteLine("      Identity: " + physVol.Identity);
                        Console.WriteLine("          Type: " + physVol.VolumeType);
                        Console.WriteLine("       Disk Id: " + physVol.DiskIdentity);
                        Console.WriteLine("      Disk Sig: " + physVol.DiskSignature.ToString("X8"));
                        Console.WriteLine("       Part Id: " + physVol.PartitionIdentity);
                        Console.WriteLine("        Length: " + physVol.Length + " bytes");
                        Console.WriteLine(" Disk Geometry: " + physVol.PhysicalGeometry);
                        Console.WriteLine("  First Sector: " + physVol.PhysicalStartSector);
                        Console.WriteLine();
                        NtfsFileSystem vhdbNtfs = new NtfsFileSystem(vhdx.Partitions[0].Open());
                        if (vhdbNtfs.FileExists("\\\\" + filepath))
                        {
                            long fileLength = vhdbNtfs.GetFileLength("\\\\" + filepath);
                            using (Stream bootStream =
                                vhdbNtfs.OpenFile("\\\\" + filepath, FileMode.Open, FileAccess.Read))
                            {
                                byte[] file = new byte[bootStream.Length];
                                int totalRead = 0;
                                while (totalRead < file.Length)
                                {
                                    totalRead += bootStream.Read(file, totalRead, file.Length - totalRead);
                                    FileStream fileStream = File.Create(destinationfile, (int) bootStream.Length);
                                    bootStream.CopyTo(fileStream);
                                    fileStream.Write(file, 0, (int) bootStream.Length);
                                }
                            }

                            long destinationLength = new FileInfo(destinationfile).Length;
                            if (fileLength != destinationLength)
                            {
                                Console.WriteLine(
                                    "[!] Something went wrong. Source file has size {0} and destination file has size {1}",
                                    fileLength, destinationLength);
                            }
                            else
                            {
                                Console.WriteLine("\r\n[*] File {0} was successfully copied to {1}", filepath,
                                    destinationfile);
                            }
                        }
                        else
                        {
                            Console.WriteLine("\r\n [!] File {0} can not be found", filepath);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine(
                    "\r\n [!] The provided VMDK image does not exist / can not be accessed or the destination folder does not exist");
            }
        }
    }
}


   