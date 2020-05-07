using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.IO;

namespace CreateSpotVMinAzure
{
    class Program
    {
        public static string PREFIX = "1";
        public static IAzure azure;
        public static string ResourceGroupName = "myResourceGroup" + PREFIX;
        public static string PublicIpName = "myPublicIP" + PREFIX;
        public static string VirtualNetworkName = "myVN" + PREFIX;
        public static string NicName = "myNIC" + PREFIX;
        public static string VMName = "TestSpotVM" + PREFIX;
        public static string SubnetName = "mySubnet" + PREFIX;
        public static string RegionName = "westcentralus";
        public static VirtualMachineSizeTypes vmSize = VirtualMachineSizeTypes.StandardD4sV3;

        static void Main(string[] args)
        {
            try
            {
                var credentials = SdkContext.AzureCredentialsFactory
                    .FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));

                string sshKey =  File.ReadAllText(Environment.GetEnvironmentVariable("SSH_PUBLIC_KEY_PATH")).Replace("\n", "");

                azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                Console.WriteLine("Creating resource group...");
                var resourceGroup = azure.ResourceGroups.Define(ResourceGroupName)
                    .WithRegion(RegionName)
                    .Create();

                Console.WriteLine("Creating public IP address...");
                var publicIPAddress = azure.PublicIPAddresses.Define(PublicIpName)
                    .WithRegion(RegionName)
                    .WithExistingResourceGroup(ResourceGroupName)
                    .WithDynamicIP()
                    .Create();

                Console.WriteLine("Creating virtual network...");
                var network = azure.Networks.Define(VirtualNetworkName)
                    .WithRegion(RegionName)
                    .WithExistingResourceGroup(ResourceGroupName)
                    .WithAddressSpace("10.0.0.0/16")
                    .WithSubnet(SubnetName, "10.0.0.0/24")
                    .Create();

                Console.WriteLine("Creating network interface...");
                var networkInterface = azure.NetworkInterfaces.Define(NicName)
                    .WithRegion(RegionName)
                    .WithExistingResourceGroup(ResourceGroupName)
                    .WithExistingPrimaryNetwork(network)
                    .WithSubnet(SubnetName)
                    .WithPrimaryPrivateIPAddressDynamic()
                    .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                    .Create();

                Console.WriteLine("Creating virtual machine...");

                // Create Linux VM
                azure.VirtualMachines.Define(VMName)
                    .WithRegion(RegionName)
                    .WithExistingResourceGroup(ResourceGroupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithLatestLinuxImage("Canonical", "UbuntuServer", "16.04.0-LTS")
                    .WithRootUsername("azureuser")
                    .WithSsh(sshKey)
                    .WithComputerName(VMName)
                    .WithSize(vmSize)
                    .WithPriority(VirtualMachinePriorityTypes.Spot)
                    .WithMaxPrice(-1)
                    .Create();         

                // Create Windows VM
                /*
                azure.VirtualMachines.Define(VMName)
                    .WithRegion(RegionName)
                    .WithExistingResourceGroup(ResourceGroupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2012-R2-Datacenter")
                    .WithAdminUsername("azureuser")
                    .WithAdminPassword("Azure12345678")
                    .WithComputerName(VMName)
                    .WithSize(vmSize)
                    .WithPriority(VirtualMachinePriorityTypes.Spot)
                    .WithMaxPrice(-1)
                    .Create();
                */

                Console.WriteLine("VM Created Successfully.");

                Console.WriteLine("Press Enter to Get the Infomation about this VM...");
                Console.ReadLine();
                var vm = azure.VirtualMachines.GetByResourceGroup(ResourceGroupName, VMName);

                Console.WriteLine("Getting information about the virtual machine...");
                Console.WriteLine("hardwareProfile");
                Console.WriteLine("   vmSize: " + vm.Size);
                Console.WriteLine("   vmPriority: " + vm.Priority);
                Console.WriteLine("storageProfile");
                Console.WriteLine("  imageReference");
                Console.WriteLine("    publisher: " + vm.StorageProfile.ImageReference.Publisher);
                Console.WriteLine("    offer: " + vm.StorageProfile.ImageReference.Offer);
                Console.WriteLine("    sku: " + vm.StorageProfile.ImageReference.Sku);
                Console.WriteLine("    version: " + vm.StorageProfile.ImageReference.Version);
                Console.WriteLine("  osDisk");
                Console.WriteLine("    osType: " + vm.StorageProfile.OsDisk.OsType);
                Console.WriteLine("    name: " + vm.StorageProfile.OsDisk.Name);
                Console.WriteLine("    createOption: " + vm.StorageProfile.OsDisk.CreateOption);
                Console.WriteLine("    caching: " + vm.StorageProfile.OsDisk.Caching);
                Console.WriteLine("osProfile");
                Console.WriteLine("  computerName: " + vm.OSProfile.ComputerName);
                Console.WriteLine("  adminUsername: " + vm.OSProfile.AdminUsername);
                Console.WriteLine("  Public IP Address: " + vm.GetPrimaryPublicIPAddress().IPAddress);
                Console.WriteLine("networkProfile");
                foreach (string nicId in vm.NetworkInterfaceIds)
                {
                    Console.WriteLine("  networkInterface id: " + nicId);
                }
                Console.WriteLine("vmAgent");
                Console.WriteLine("  vmAgentVersion" + vm.InstanceView.VmAgent.VmAgentVersion);
                Console.WriteLine("    statuses");
                foreach (InstanceViewStatus stat in vm.InstanceView.VmAgent.Statuses)
                {
                    Console.WriteLine("    code: " + stat.Code);
                    Console.WriteLine("    level: " + stat.Level);
                    Console.WriteLine("    displayStatus: " + stat.DisplayStatus);
                    Console.WriteLine("    message: " + stat.Message);
                    Console.WriteLine("    time: " + stat.Time);
                }
                Console.WriteLine("disks");
                foreach (DiskInstanceView disk in vm.InstanceView.Disks)
                {
                    Console.WriteLine("  name: " + disk.Name);
                    Console.WriteLine("  statuses");
                    foreach (InstanceViewStatus stat in disk.Statuses)
                    {
                        Console.WriteLine("    code: " + stat.Code);
                        Console.WriteLine("    level: " + stat.Level);
                        Console.WriteLine("    displayStatus: " + stat.DisplayStatus);
                        Console.WriteLine("    time: " + stat.Time);
                    }
                }
                Console.WriteLine("VM general status");
                Console.WriteLine("  provisioningStatus: " + vm.ProvisioningState);
                Console.WriteLine("  id: " + vm.Id);
                Console.WriteLine("  name: " + vm.Name);
                Console.WriteLine("  type: " + vm.Type);
                Console.WriteLine("  location: " + vm.Region);
                Console.WriteLine("VM instance status");
                foreach (InstanceViewStatus stat in vm.InstanceView.Statuses)
                {
                    Console.WriteLine("  code: " + stat.Code);
                    Console.WriteLine("  level: " + stat.Level);
                    Console.WriteLine("  displayStatus: " + stat.DisplayStatus);
                }
                Console.WriteLine("Press enter to continue...");
                Console.ReadLine();
                Console.WriteLine("Press enter to Delete the Resource Group and Exit...");
                Console.ReadLine();
            }
            finally
            {
                azure.ResourceGroups.DeleteByName(ResourceGroupName);
            }

        }
    }
}
