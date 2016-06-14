using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace ControlApi
{
    enum Commands
    {
        MouseMove = 0,
        Zoom = 1,
        CtrMouse = 2,
        ShiftMosue = 3
    }
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en");
            var allDevices = UsbDevice.AllDevices;
            foreach (UsbRegistry usbRegistry in allDevices)
            {
                if (usbRegistry.DeviceProperties["Class"].ToString() != "AndroidUsbDeviceClass")
                    continue;

                var device = UsbDevice.OpenUsbDevice(n => usbRegistry.Pid == n.Pid);
                if (device == null)
                    throw new Exception("Device Not Found.");
                Console.WriteLine(device.Info.ProductString + " - Connected");
                var wholeUsbDevice = device as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    wholeUsbDevice.SetConfiguration(1);
                    wholeUsbDevice.ClaimInterface(0);
                }
                var reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
                var stream = new MemoryStream();
                new Task(() => ReadFromClient(stream)).Start();
                reader.DataReceived += (u, e) =>
                {
                    var p = stream.Position;
                    stream.Write(e.Buffer, 0, e.Count);
                    stream.Flush();
                    stream.Seek(p, SeekOrigin.Begin);
                };

            }

            foreach (var i in NetworkInterface.GetAllNetworkInterfaces())
                foreach (UnicastIPAddressInformation ua in i.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    try
                    {
                        var port = 4520;
                        TcpListener listener = new TcpListener(ua.Address, port);
                        listener.Start();
                        Console.WriteLine("start on:" + listener.LocalEndpoint);
                        new Task(() =>
                        {
                            while (true)
                            {

                                var client = listener.AcceptTcpClient();
                                new Task(() => AcceptConnection(client)).Start();
                            }
                        }).Start();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("fail for:" + ua.Address);
                    }
                }

            Console.WriteLine("Server listening on port 4520. Press enter to exit.");
            Console.ReadLine();
        }

        private static void AcceptConnection(TcpClient client)
        {
            Console.WriteLine("Clien connection");
            try
            {
                var clientStream = client.GetStream();
                new Task(() => ReadFromClient(clientStream)).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }

        private static void ReadFromClient(Stream clientStream)
        {
            var inputSimulator = new InputSimulator();
            var reader = new StreamReader(clientStream);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null)
                    break;
                Console.WriteLine(line);
                try
                {
                    var param = line.Split(',').Select(n => n.Trim()).ToArray();
                    var commandId = (Commands)int.Parse(param[0]);
                    switch (commandId)
                    {
                        case Commands.MouseMove:
                            MouseMove(param, inputSimulator);
                            break;
                        case Commands.Zoom:
                            inputSimulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                            inputSimulator.Mouse.VerticalScroll(int.Parse(param[1]));
                            inputSimulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                            break;
                        case Commands.CtrMouse:
                            MouseMoveModify(param, inputSimulator, VirtualKeyCode.CONTROL);
                            break;
                        case Commands.ShiftMosue:
                            MouseMoveModify(param, inputSimulator, VirtualKeyCode.SHIFT);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void MouseMoveModify(string[] param, InputSimulator inputSimulator, VirtualKeyCode key)
        {
            var deltaX = int.Parse(param[1]);
            var deltaY = int.Parse(param[2]);
            inputSimulator.Keyboard.KeyDown(key);
            inputSimulator.Mouse.MoveMouseBy(deltaX, deltaY);
            inputSimulator.Keyboard.KeyUp(key);
        }

        private static void MouseMove(string[] param, InputSimulator inputSimulator)
        {
            var deltaX = int.Parse(param[1]);
            var deltaY = int.Parse(param[2]);
            inputSimulator.Mouse.MoveMouseBy(deltaX, deltaY);
        }
    }
}
